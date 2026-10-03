using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerPauseManager : MonoBehaviour
{

    [SerializeField] private PlayerInputController playerInputController;
    [SerializeField] private CanvasGroup pauseMenuCanvasGroup;
    [Tooltip("Velocità di fade in/out del menu di pausa. Valore più alto = fade più veloce.")]
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private TextMeshProUGUI resumeText;
    [SerializeField] private PlayerMovementController playerMovementController;
    [SerializeField] private PlayerCameraController playerCameraController;

    private AudioManager _audioManager;
    private bool _isPaused = false;
    private bool _isCoroutineRunning = false;
    private string _resumeText;

    private void Start()
    {
        pauseMenuCanvasGroup.alpha = 0;
        pauseMenuCanvasGroup.interactable = false;
        pauseMenuCanvasGroup.blocksRaycasts = false;

        _audioManager = AudioManager.Instance;
    }
    void Update()
    {
        if (playerInputController.InputActions.Player.Quit.WasPressedThisFrame() && !_isCoroutineRunning)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        Debug.Log("Called toggle pause! IsPaused: " + _isPaused);
        _resumeText = $"Press {playerInputController.InputActions.Player.Quit.name} to Resume";
        if (_isPaused)
        {
            StartCoroutine(ResumeGame(fadeSpeed));
        }
        else
        {
            StartCoroutine(PauseGame(fadeSpeed));
        }
    }

    private IEnumerator PauseGame(float fadeSpeed)
    {
        Debug.Log("Pausing game...");
        _isCoroutineRunning = true;
        _isPaused = true;

        _audioManager.PauseMusic();
        playerMovementController.StopMovement();
        playerCameraController.StopLook();
        while (pauseMenuCanvasGroup.alpha < 1)
        {
            pauseMenuCanvasGroup.alpha = Mathf.MoveTowards(pauseMenuCanvasGroup.alpha, 1, fadeSpeed * Time.deltaTime);
            Debug.Log("Pause menu alpha: " + pauseMenuCanvasGroup.alpha);
            yield return null;
        }
        pauseMenuCanvasGroup.alpha = 1;
        pauseMenuCanvasGroup.interactable = true;
        _isCoroutineRunning = false;
        Debug.Log("Game paused! IsPaused: " + _isPaused);
    }

    private IEnumerator ResumeGame(float fadeSpeed)
    {
        Debug.Log("Resuming game...");
        _isCoroutineRunning = true;
        _isPaused = false;

        _audioManager.ResumeMusic();
        playerMovementController.StartMovement();
        playerCameraController.StartLook();
        while (pauseMenuCanvasGroup.alpha > 0)
        {
            pauseMenuCanvasGroup.alpha = Mathf.MoveTowards(pauseMenuCanvasGroup.alpha, 0, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        pauseMenuCanvasGroup.alpha = 0;
        pauseMenuCanvasGroup.interactable = false;
        pauseMenuCanvasGroup.blocksRaycasts = false;
        _isCoroutineRunning = false;
        Debug.Log("Game resumed!");
    }
}
