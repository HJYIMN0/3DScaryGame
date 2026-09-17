using System.Collections;
using TMPro;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private PlayerInteractionController playerInteractionController;
    [SerializeField] private TextMeshProUGUI pressAnyKeyAction;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;
    [SerializeField] private float fadeSpeed = 2f;
    [SerializeField] private InteractableDrillableWall interactableDrillableWall;
    [SerializeField] private DrillableWallMinigame drillableWallMinigame;

    [SerializeField] private Light wallLight;
    [SerializeField] private float wallLightIntensity = 1.1f;
    [SerializeField] private float wallLightMinIntensity = 1f;
    [SerializeField] private float wallLightMaxIntensity = 100f;
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraMoveSpeed = 2f;

    private bool _isGameStarted = false;

    private string playerActionInput;

    private void Start()
    {
        playerInputController.InputActions.Player.Interact.performed += ctx => Debug.Log("Starting the game...");
        playerInputController.InputActions.Player.Interact.performed += ctx => StartGame();

        playerActionInput = playerInputController.InputActions.Player.Interact.name;

        pressAnyKeyAction.text = $"Press [{playerActionInput}] to Start";
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

        mainMenuCanvasGroup.alpha = 1;
        while (mainMenuCanvasGroup.alpha > 0)
        {
            mainMenuCanvasGroup.alpha = Mathf.Lerp(mainMenuCanvasGroup.alpha, 0, Time.deltaTime * fadeSpeed);
            yield return null;
        }    
    }

    private void HandleMiniGameCompleted()
    {
        StopAllCoroutines();
        StartCoroutine(LoadGame());
    }

    private IEnumerator LoadGame()
    {
        Vector3 newCameraPos = Camera.main.transform.position + Camera.main.transform.forward * cameraDistance;
        while (Vector3.Distance(Camera.main.transform.position, newCameraPos) >= 0.1f)
        {
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, newCameraPos, Time.deltaTime * cameraMoveSpeed);
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
