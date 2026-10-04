using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuManager : MonoBehaviour
{
    [Header("Player references")]
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private PlayerInteractionController playerInteractionController;

    [Header("Ui attributes")]
    [SerializeField] private TextMeshProUGUI pressAnyKeyAction;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Drillable wall references")]
    [SerializeField] private InteractableDrillableWall interactableDrillableWall;
    [SerializeField] private DrillableWallMinigame drillableWallMinigame;

    [Header("Audio settings")]
    [SerializeField] private GameMusicManager gameMusicManager;
    [SerializeField] private AudioClip distrurbingAudioClip;

    [Header("Light settings")]
    [SerializeField] private Light wallLight;
    [SerializeField] private float wallLightMinIntensity = 1f;
    [SerializeField] private float wallLightMaxIntensity = 100f;

    [Header("Camera settings")]
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraMoveSpeed = 2f;

    [Header("Material settings")]
    [SerializeField] private GameObject wallObject;
    [SerializeField] private Color destinationColor = Color.white;
    [SerializeField] private float colorChangeSpeed = 5f;

    private Renderer _wallRenderer;
    private MaterialPropertyBlock _wallMaterialProperties;
    private Color _wallStartColor;
    private int _colorPropertyId;

    private Material _wallMaterialInstance;

    private bool _isGameStarted = false;
    private string playerActionInput;

    private void Awake()
    {
        if (wallObject == null)
        {
            Debug.LogError("[MainMenuManager] wallObject non assegnato.");
            return;
        }

        _wallRenderer = wallObject.GetComponent<Renderer>();

        if (_wallRenderer == null)
        {
            Debug.LogError("[MainMenuManager] wallObject non ha un Renderer. Se il Renderer è su un figlio, assegna direttamente quel GameObject.");
            return;
        }

        Material wallMaterial = _wallRenderer.sharedMaterial;

        if (wallMaterial == null)
        {
            Debug.LogError("[MainMenuManager] Il Renderer non ha un Material assegnato.");
            return;
        }

        // [MODIFICA] Usa MaterialPropertyBlock: aggiorna il colore visibile in scena senza creare una copia del Material.
        if (wallMaterial.HasProperty("_BaseColor"))
        {
            _colorPropertyId = Shader.PropertyToID("_BaseColor");
        }
        else if (wallMaterial.HasProperty("_Color"))
        {
            _colorPropertyId = Shader.PropertyToID("_Color");
        }
        else
        {
            Debug.LogError($"[MainMenuManager] Il Material '{wallMaterial.name}' non espone né _BaseColor né _Color. Verifica la Reference della proprietà nel Shader Graph.");
            return;
        }

        _wallStartColor = wallMaterial.GetColor(_colorPropertyId);
        _wallMaterialProperties = new MaterialPropertyBlock();
    }

    private void Start()
    {
        playerInputController.InputActions.Player.Interact.performed += ctx => Debug.Log("Starting the game...");
        playerInputController.InputActions.Player.Interact.performed += ctx => StartGame();

        playerActionInput = playerInputController.InputActions.Player.Interact.GetBindingDisplayString(0);
        pressAnyKeyAction.text = $"{playerActionInput} to Start";
    }

    private void OnDestroy()
    {
        // Evita di lasciare in giro l'istanza del materiale creata a runtime.
        if (_wallMaterialInstance != null)
        {
            Destroy(_wallMaterialInstance);
        }
    }

    private void StartGame()
    {
        if (_isGameStarted) return;
        _isGameStarted = true;

        drillableWallMinigame.OnMiniGameCompleted += HandleMiniGameCompleted;
        drillableWallMinigame.OnProgressChanged += HandleProgressChanged;

        playerInteractionController.SetInteractableTaskForPlayer(interactableDrillableWall);
        playerInteractionController.SetActiveMiniGameForPlayer(drillableWallMinigame);

        drillableWallMinigame.StartMiniGame();
        StartCoroutine(HandleMiniGameStarted());
    }

    private IEnumerator HandleMiniGameStarted()
    {
        mainMenuCanvasGroup.alpha = 1f;
        gameMusicManager.Play();

        if (_wallRenderer == null || _wallMaterialProperties == null)
        {
            Debug.LogError("[MainMenuManager] Renderer o MaterialPropertyBlock non inizializzati: cambio colore annullato.");
            yield break;
        }

        // [MODIFICA] colorChangeSpeed è usato come durata in secondi del solo cambio colore.
        float colorChangeDuration = Mathf.Max(0.01f, colorChangeSpeed);
        float elapsedTime = 0f;

        float colorProgress = 0f;
        float fadeProgress = 0f;

        // [MODIFICA] Attende che finiscano sia il colore sia il fade, ognuno con il proprio valore serializzato.
        while (colorProgress < 1f || fadeProgress < 1f)
        {
            elapsedTime += Time.deltaTime;

            colorProgress = Mathf.Clamp01(elapsedTime / colorChangeDuration);
            fadeProgress = Mathf.Clamp01(elapsedTime * fadeSpeed);

            mainMenuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeProgress);

            _wallRenderer.GetPropertyBlock(_wallMaterialProperties);
            _wallMaterialProperties.SetColor(
                _colorPropertyId,
                Color.Lerp(_wallStartColor, destinationColor, colorProgress)
            );
            _wallRenderer.SetPropertyBlock(_wallMaterialProperties);

            yield return null;
        }

        mainMenuCanvasGroup.alpha = 0f;

        _wallRenderer.GetPropertyBlock(_wallMaterialProperties);
        _wallMaterialProperties.SetColor(_colorPropertyId, destinationColor);
        _wallRenderer.SetPropertyBlock(_wallMaterialProperties);
    }

    private void HandleMiniGameCompleted()
    {
        StopAllCoroutines();

        // PRIMA:  gameMusicManager.Play(distrurbingAudioClip, true);
        // Sostituiva la musica persistente (con persistAcrossScenes = true).
        // ORA:
        gameMusicManager.PlayLocal(distrurbingAudioClip, true);

        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        Vector3 newCameraPos = Camera.main.transform.position
                               + Camera.main.transform.forward * cameraDistance;

        while (Vector3.Distance(Camera.main.transform.position, newCameraPos) >= 0.1f)
        {
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                newCameraPos,
                Time.deltaTime * cameraMoveSpeed
            );
            yield return null;
        }

        Debug.Log("Loading the game...");
        GameFlowManager.Instance.LoadNextDay(GameFlowManager.Instance.FadeDuration);
    }

    private void HandleProgressChanged(float percentage)
    {
        wallLight.intensity = Mathf.Lerp(wallLightMinIntensity, wallLightMaxIntensity, percentage);
    }
}