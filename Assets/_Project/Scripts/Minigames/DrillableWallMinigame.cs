using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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
    [Tooltip("CinemachineCamera dedicata al minigioco, posizionata in scena davanti al plane.")]
    [SerializeField] private CinemachineCamera _miniGameCamera;
    

    [Header("Controller")]
    [Tooltip("Velocità di spostamento del cursore virtuale con l'analogico destro.")]
    [SerializeField] private float _controllerCursorSpeed = 800f;

    // MODIFICATO: rimossa _mainCamera assegnata via Camera.main — ora si usa _raycastCamera serializzata.
    private Texture2D _wallTexture;
    private Renderer _renderer;

    private int _erasedPixelCount = 0;
    private int _totalPixelCount;

    private Vector2 _virtualCursorPosition;

    private Camera _raycastCamera;


    public override void Start()
    {
        base.Start();

        _renderer = GetComponent<Renderer>();

        _raycastCamera = Camera.main;

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

        // AGGIUNTO: textureCoord funziona SOLO con MeshCollider.
        // Il plane ha un BoxCollider (usato come trigger da AbstractInteractable),
        // quindi aggiungiamo un MeshCollider dedicato al raycast UV, senza rimuovere il BoxCollider.
        if (GetComponent<MeshCollider>() == null)
        {
            MeshCollider mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = GetComponent<MeshFilter>().sharedMesh;
        }

        // AGGIUNTO: la camera del minigioco deve essere disattiva all'avvio
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(false);
        else
            Debug.LogWarning("[DrillableWallMinigame] _miniGameCamera non assegnata.");
    }

    private void Update()
    {
        if (!IsMiniGameActive) return;

        UpdateVirtualCursorFromGamepad();

        // AGGIUNTO: aspettiamo che il CinemachineBrain abbia completato la transizione
        // verso la camera del minigioco prima di permettere qualsiasi interazione.
        // Se l'utente clicca durante il blend, la direzione del raycast è ancora quella
        // della camera precedente e colpisce punti sbagliati.
        CinemachineBrain brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null && brain.IsBlending) return;

        if (_playerInputController.InputActions.Player.Attack.WasPressedThisFrame())
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
        base.StartMiniGame();

        if (HasMiniGameBeenCompleted) return;

        TogglePlayerControl(false, true);

        // MODIFICATO: attiviamo la CinemachineCamera dedicata.
        // Cinemachine, vedendo una camera attiva con priorità più alta,
        // esegue automaticamente il blend verso di essa.
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(true);

        _virtualCursorPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();

        TogglePlayerControl(true, true);

        // MODIFICATO: disattiviamo la CinemachineCamera del minigioco.
        // Cinemachine torna automaticamente alla camera del player.
        if (_miniGameCamera != null)
            _miniGameCamera.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (HasCompletitionBeenReached())
        {
            taskManager.CompleteTask(interactable.TaskSO.TaskName);
            interactable.SetHasBeenCompleted(true);
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
        Vector2 screenPos = GetScreenPosition();

        Ray ray = _raycastCamera.ScreenPointToRay(screenPos);

        // MODIFICATO: QueryTriggerInteraction.Ignore esclude i BoxCollider trigger dal raycast
        // (usati da AbstractInteractable per OnTriggerEnter/Exit), così il raycast colpisce
        // solo il MeshCollider, necessario per ottenere textureCoord corrette.
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        // AGGIUNTO: log diagnostico — se il raycast colpisce un altro oggetto (es. una parete
        // adiacente), il check lo scarta senza output. Questo rivela cosa blocca il lato sinistro.
        if (hit.collider.gameObject != gameObject)
        {
            Debug.LogWarning($"[DrillableWallMinigame] Raycast ha colpito '{hit.collider.gameObject.name}' " +
                             $"(layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}) invece del plane.");
            return;
        }

        Vector2 uv = hit.textureCoord;
        Debug.Log("Hit UV: " + uv);

        int pixelX = Mathf.FloorToInt(uv.x * _wallTexture.width);
        int pixelY = Mathf.FloorToInt(uv.y * _wallTexture.height);

        EraseWithBrush(pixelX, pixelY);
        interactable.PLayTaskSfx();
    }

    // MODIFICATO: rinominato da EraseCircle a EraseWithBrush.
    // Se _brushTexture è assegnata, usa la sua alpha per definire la forma del pennello.
    // Ogni pixel del bounding box viene cancellato solo se il corrispondente pixel
    // della brush texture ha alpha > 0.5. Se _brushTexture è null, fallback circolare.
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
                    // Mappiamo l'offset del pixel alle UV della brush texture:
                    // il centro del brush corrisponde a UV (0.5, 0.5).
                    // GetPixelBilinear campiona con interpolazione e wrapping automatico.
                    float u = (dx / (brushSize * 2f)) + 0.5f;
                    float v = (dy / (brushSize * 2f)) + 0.5f;
                    shouldErase = _brushTexture.GetPixelBilinear(u, v).a > 0.5f;
                }
                else
                {
                    // Fallback: brush circolare originale
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
        Debug.Log("Applied changes to texture after erasing at (" + centerX + ", " + centerY + ")");
        if (HasCompletitionBeenReached())
        {
            Debug.Log("MiniGame completed after erasing at (" + centerX + ", " + centerY + ")");
            QuitMiniGame();
        }
    }

    //private void CheckCompletionThreshold()
    //{
    //    float exposedPercentage = (float)_erasedPixelCount / _totalPixelCount;
    //    if (exposedPercentage >= quitMiniGameAlphaThreshold)
    //    {
    //        Debug.Log($"[DrillableWallMinigame] Soglia raggiunta ({exposedPercentage:P0}). Chiusura minigioco.");
    //        QuitMiniGame();
    //    }
    //}

    public bool HasCompletitionBeenReached()
    {
        float exposedPercentage = (float)_erasedPixelCount / _totalPixelCount;
        return exposedPercentage >= quitMiniGameAlphaThreshold;
    }
}