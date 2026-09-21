using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class EndGameManager : MonoBehaviour
{
    [SerializeField] private PlayerInputController playerInputController;

    [Header("Ui attributes")]
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private CanvasGroup GameIsOverCanvasGroup;
    [SerializeField] private float fadeTime = 2f;

    private bool isCreditShowing => creditsCanvasGroup != null && creditsCanvasGroup.alpha > 0f;
    private bool _isCoroutineRunning;

    private void Awake()
    {
        if (playerInputController == null)
        {
            Debug.LogWarning("You forgot to assign the PlayerInputController");
            playerInputController = GetComponent<PlayerInputController>();
        }

        if (creditsCanvasGroup == null || GameIsOverCanvasGroup == null)
        {
            Debug.LogError("CanvasGroup non assegnati.");
            enabled = false;
            return;
        }

        // Stato iniziale: crediti nascosti, game over visibile
        creditsCanvasGroup.alpha = 0f;
        GameIsOverCanvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
            Debug.Log("Application.Quit() called. If running in the editor, this won't close the editor.");
        }

        if (playerInputController == null || playerInputController.InputActions == null)
            return;

        if (playerInputController.InputActions.Player.Interact.WasPressedThisFrame())
        {
            EvaluateCanvaStatus();
        }
    }

    private void EvaluateCanvaStatus()
    {
        if (_isCoroutineRunning) return;

        _isCoroutineRunning = true;

        if (isCreditShowing)
            StartCoroutine(FadeBetweenCanvas(fadeTime, creditsCanvasGroup, GameIsOverCanvasGroup));
        else
            StartCoroutine(FadeBetweenCanvas(fadeTime, GameIsOverCanvasGroup, creditsCanvasGroup));
    }

    private IEnumerator FadeBetweenCanvas(float timeToFade, CanvasGroup canvaToFade, CanvasGroup canvaToShow)
    {
        float startFadeAlpha = canvaToFade.alpha;
        float startShowAlpha = canvaToShow.alpha;
        float elapsed = 0f;

        while (elapsed < timeToFade)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / timeToFade);

            canvaToFade.alpha = Mathf.Lerp(startFadeAlpha, 0f, t);
            canvaToShow.alpha = Mathf.Lerp(startShowAlpha, 1f, t);

            yield return null;
        }

        canvaToFade.alpha = 0f;
        canvaToShow.alpha = 1f;
        _isCoroutineRunning = false;
    }
}