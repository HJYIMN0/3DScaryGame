using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CreditsScroller : MonoBehaviour
{
    [SerializeField] private PlayerInputController playerInputController;

    [Header("Scroll")]
    [Tooltip("RectTransform del content (quello con il Content Size Fitter) da scrollare.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Velocità di scroll, in unità al secondo.")]
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private float fastScrollMultiplier = 3f;

    [Tooltip("Distanza (in unità) di cui il content deve scorrere prima di fermarsi.")]
    [SerializeField] private float stopDistance = 500f;

    // Posizione di partenza e target, calcolate a runtime in Start().
    private Vector2 startPosition;
    private Vector2 targetPosition;

    private float _currentScrollSpeed;

    [Header("Scroll Manuale (verso l'alto)")]
    [Tooltip("Azione di input che, tenuta premuta, scorre i crediti verso l'alto.")]
    [SerializeField] private InputActionReference scrollUpAction;

    [Tooltip("Velocità dello scroll manuale verso l'alto, in unità al secondo.")]
    [SerializeField] private float scrollUpSpeed = 100f;

    // Flag: il giocatore sta attualmente scrollando verso l'alto.
    private bool _isScrollingUp;

    [Header("Debug")]
    [Tooltip("Se true, in Start() salta fade-in e scroll e porta subito il content sul target.")]
    [SerializeField] private bool isDebugMode = false;

    [Header("Fade")]
    [Tooltip("CanvasGroup su cui viene eseguito il fade-in.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Velocità di fade, in unità di alpha al secondo.")]
    [SerializeField] private float FadeSpeed = 1f;

    private void Awake()
    {
        _currentScrollSpeed = scrollSpeed;
    }

    private void Start()
    {
        startPosition = content.anchoredPosition;
        targetPosition = startPosition + Vector2.down * stopDistance;

        if (isDebugMode)
        {
            canvasGroup.alpha = 1f;
            content.anchoredPosition = targetPosition;
            return;
        }

        StartCoroutine(PlayCreditsSequence());
    }

    private void Update()
    {
        if (playerInputController.InputActions.Player.Jump.IsPressed() ||
            playerInputController.InputActions.Player.Attack.IsPressed())
        {
            _currentScrollSpeed = scrollSpeed * fastScrollMultiplier;
        }
        else if (playerInputController.InputActions.Player.Jump.WasReleasedThisFrame() ||
                 playerInputController.InputActions.Player.Attack.WasReleasedThisFrame())
        {
            _currentScrollSpeed = scrollSpeed;
        }

        // Legge l'input di scroll manuale verso l'alto.
        _isScrollingUp = scrollUpAction != null && scrollUpAction.action.IsPressed();
    }

    private IEnumerator PlayCreditsSequence()
    {
        yield return StartCoroutine(FadeIn());
        yield return StartCoroutine(ScrollCredits());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;

        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += FadeSpeed * Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator ScrollCredits()
    {
        // MODIFICA: la coroutine non termina più quando il content raggiunge la fine.
        // Resta in ascolto: se il content viene spostato verso l'alto (scroll manuale),
        // riprende automaticamente a scorrere verso il target.
        while (true)
        {
            if (_isScrollingUp)
            {
                // Scroll manuale verso l'alto: allontana il content dal target.
                content.anchoredPosition += Vector2.up * scrollUpSpeed * Time.deltaTime;
            }
            else if (content.anchoredPosition != targetPosition)
            {
                // Scroll automatico verso la fine. Vale anche se il content è stato
                // riportato in alto dopo aver già raggiunto la fine.
                content.anchoredPosition = Vector2.MoveTowards(
                    content.anchoredPosition,
                    targetPosition,
                    _currentScrollSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }
}