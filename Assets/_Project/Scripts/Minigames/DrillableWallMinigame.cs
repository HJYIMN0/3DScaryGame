using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;

public class DrillableWallMinigame : AbstractMinigame
{
    [Header("Pennello")]
    [SerializeField] private int brushSize = 1;
    [Tooltip("Texture che definisce la forma del pennello. L'alpha determina quali pixel vengono cancellati. " +
             "Se null, usa il brush circolare di default. Richiede Read/Write abilitato nelle import settings.")]
    [SerializeField] private Texture2D _brushTexture;

    [Header("Completamento")]
    [Tooltip("Percentuale di pixel esposti (0-1) oltre la quale il minigioco termina automaticamente.")]
    [SerializeField] private float quitMiniGameAlphaThreshold = 0.5f;

    [Header("Camera")]
    [Tooltip("CinemachineCamera dedicata al minigioco. Se non assegnata, il minigioco usa Camera.main senza effettuare blend.")]
    [SerializeField] private CinemachineCamera _miniGameCamera;

    [Tooltip("Camera usata per il raycast del minigioco. Se non assegnata, viene usata Camera.main come fallback.")]
    [SerializeField] private Camera _raycastCamera;

    [Header("Controller")]
    [Tooltip("Velocità di spostamento del cursore virtuale con l'analogico destro.")]
    [SerializeField] private float _controllerCursorSpeed = 800f;

    private Texture2D _wallTexture;
    private Renderer _renderer;

    private int _erasedPixelCount = 0;
    private int _totalPixelCount;

    public float CompletionPercentage => (float)_erasedPixelCount / _totalPixelCount;

    private Vector2 _virtualCursorPosition;

    public Action OnMiniGameCompleted;
    public event Action<float> OnProgressChanged;

    public override void Start()
    {
        base.Start();

        _renderer = GetComponent<Renderer>();

        // MODIFICATO: se la camera per il raycast non è assegnata nell'Inspector,
        // si procede usando Camera.main come fallback.
        if (_raycastCamera == null)
        {
            _raycastCamera = Camera.main;
            if (_raycastCamera == null)
            {
                Debug.LogError("[DrillableWallMinigame] Nessuna _raycastCamera assegnata e Camera.main non trovata. " +
                               "Impossibile procedere con il raycast.");
                return;
            }
            Debug.Log("[DrillableWallMinigame] _raycastCamera non assegnata: uso Camera.main come fallback.");
        }

        Texture2D originalTexture = _renderer.material.mainTexture as Texture2D;
        if (originalTexture == null)
        {
            Debug.LogError("The material's main texture is not a Texture2D.");
            return;
        }

        _wallTexture = new Texture2D(originalTexture.width,
                                     originalTexture.height,
                                     TextureFormat.RGBA32,
                                     mipChain: false);

        _wallTexture.SetPixels(originalTexture.GetPixels());
        _wallTexture.Apply();

        _renderer.material = new Material(_renderer.material);
        _renderer.material.mainTexture = _wallTexture;

        _totalPixelCount = _wallTexture.width * _wallTexture.height;

        // textureCoord funziona SOLO con MeshCollider.
        if (GetComponent<MeshCollider>() == null)
        {
            MeshCollider mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        }

        // MODIFICATO: se _miniGameCamera non è assegnata, avvisiamo soltanto.
        // Il minigioco procederà comunque usando Camera.main (raycast e rendering).
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(false);
        else
            Debug.LogWarning("[DrillableWallMinigame] _miniGameCamera non assegnata: verrà usata Camera.main.");
    }

    private void Update()
    {
        if (!IsMiniGameActive) return;

        UpdateVirtualCursorFromGamepad();

        // Aspettiamo che il CinemachineBrain abbia completato la transizione
        // verso la camera del minigioco. Se non c'è blending (es. nessuna _miniGameCamera),
        // IsBlending sarà false e si procede normalmente.
        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null && brain.IsBlending) return;

        if (playerInputController.InputActions.Player.Attack.WasPressedThisFrame())
        {
            HandleMiniGameLogic();
        }
    }

    private void UpdateVirtualCursorFromGamepad()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        Vector2 stick = gamepad.rightStick.ReadValue();
        if (stick.magnitude <= 0.1f) return;

        _virtualCursorPosition += stick * _controllerCursorSpeed * Time.deltaTime;
        _virtualCursorPosition.x = Mathf.Clamp(_virtualCursorPosition.x, 0f, Screen.width);
        _virtualCursorPosition.y = Mathf.Clamp(_virtualCursorPosition.y, 0f, Screen.height);
    }

    private Vector2 GetScreenPosition()
    {
        var mouse = Mouse.current;
        if (mouse != null)
            _virtualCursorPosition = mouse.position.ReadValue();

        return _virtualCursorPosition;
    }

    public override void StartMiniGame()
    {
        if (HasMiniGameBeenCompleted) return;

        base.StartMiniGame();

        TogglePlayerControl(false, true);

        // MODIFICATO: attiviamo la CinemachineCamera dedicata solo se assegnata.
        // Se non lo è, il minigioco rimane su Camera.main.
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(true);
        else
            Debug.Log("[DrillableWallMinigame] Nessuna _miniGameCamera: uso Camera.main per il minigioco.");

        _virtualCursorPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();

        TogglePlayerControl(true, true);

        // Disattiviamo la CinemachineCamera del minigioco (se assegnata).
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (HasCompletitionBeenReached())
        {
            Debug.Log("MiniGame completato! Segnalo al TaskManager che il task è stato completato.");
            taskManager.CompleteTask(interactable.TaskSO);
            interactable.SetHasBeenCompleted(true);
            OnMiniGameCompleted?.Invoke();
        }
    }

    public override void ResetMiniGame()
    {
        Debug.Log("Resetting DrillableWallMinigame...");
    }

    public override void HandleMiniGameLogic()
    {
        TryEraseAtPosition();
    }

    private void TryEraseAtPosition()
    {
        // Sicurezza: se per qualche motivo la camera è nulla, proviamo a recuperarla.
        if (_raycastCamera == null)
        {
            _raycastCamera = Camera.main;
            if (_raycastCamera == null) return;
        }

        Vector2 screenPos = GetScreenPosition();

        Ray ray = _raycastCamera.ScreenPointToRay(screenPos);

        // QueryTriggerInteraction.Ignore esclude i BoxCollider trigger dal raycast,
        // così il raycast colpisce solo il MeshCollider (necessario per textureCoord corrette).
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider.gameObject != gameObject)
        {
            Debug.LogWarning($"[DrillableWallMinigame] Raycast ha colpito '{hit.collider.gameObject.name}' " +
                             $"(layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) invece del plane.");
            return;
        }

        Vector2 uv = hit.textureCoord;

        int pixelX = Mathf.FloorToInt(uv.x * _wallTexture.width);
        int pixelY = Mathf.FloorToInt(uv.y * _wallTexture.height);

        EraseWithBrush(pixelX, pixelY);
        interactable.PLayTaskSfx();
    }

    private void EraseWithBrush(int centerX, int centerY)
    {
        int xMin = Mathf.Max(0, centerX - brushSize);
        int xMax = Mathf.Min(_wallTexture.width - 1, centerX + brushSize);
        int yMin = Mathf.Max(0, centerY - brushSize);
        int yMax = Mathf.Min(_wallTexture.height - 1, centerY + brushSize);

        float radiusSq = brushSize * brushSize;

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                float dx = x - centerX;
                float dy = y - centerY;

                bool shouldErase;
                if (_brushTexture != null)
                {
                    float u = (dx / (brushSize * 2f)) + 0.5f;
                    float v = (dy / (brushSize * 2f)) + 0.5f;
                    shouldErase = _brushTexture.GetPixelBilinear(u, v).a > 0.5f;
                }
                else
                {
                    shouldErase = (dx * dx + dy * dy) <= radiusSq;
                }

                if (!shouldErase) continue;

                Color pixel = _wallTexture.GetPixel(x, y);
                if (pixel.a > 0f)
                {
                    pixel.a = 0f;
                    _wallTexture.SetPixel(x, y, pixel);
                    _erasedPixelCount++;
                }
            }
        }

        _wallTexture.Apply();

        _wallTexture.Apply();

        OnProgressChanged?.Invoke(CompletionPercentage);   // <-- qui

        if (HasCompletitionBeenReached())
        {
            OnMiniGameCompleted?.Invoke();
            QuitMiniGame();
        }
        if (HasCompletitionBeenReached())
        {
            OnMiniGameCompleted?.Invoke();
            QuitMiniGame();
        }
    }

    public bool HasCompletitionBeenReached()
    {
        float exposedPercentage = (float)_erasedPixelCount / _totalPixelCount;
        return exposedPercentage >= quitMiniGameAlphaThreshold;
    }
}