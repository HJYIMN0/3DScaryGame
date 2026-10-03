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

    private Material _wallMaterialInstance;
    private int _colorPropertyId;

    private bool _isGameStarted = false;
    private string playerActionInput;

    private void Awake()
    {
        if (wallObject == null)
        {
            Debug.LogError("[MainMenuManager] wallObject non assegnato.");
            return;
        }

        Renderer rend = wallObject.GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogError("[MainMenuManager] wallObject non ha un Renderer.");
            return;
        }

        // .material crea/restituisce l'istanza del materiale solo per questo renderer.
        // NON usare .sharedMaterial, altrimenti modifichi l'asset condiviso.
        _wallMaterialInstance = rend.material;

        // URP / HDRP usano "_BaseColor", Built-in Standard usa "_Color".
        _colorPropertyId = _wallMaterialInstance.HasProperty("_BaseColor")
            ? Shader.PropertyToID("_BaseColor")
            : Shader.PropertyToID("_Color");
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

        if (_wallMaterialInstance == null)
            yield break;

        Color startColor = _wallMaterialInstance.GetColor(_colorPropertyId);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            float clamped = Mathf.Clamp01(t);

            mainMenuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, clamped);
            _wallMaterialInstance.SetColor(
                _colorPropertyId,
                Color.Lerp(startColor, destinationColor, clamped)
            );

            yield return null;
        }

        // Forza i valori finali per evitare residui floating-point.
        mainMenuCanvasGroup.alpha = 0f;
        _wallMaterialInstance.SetColor(_colorPropertyId, destinationColor);
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