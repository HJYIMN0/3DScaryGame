using System.Collections;
using UnityEngine;

/// <summary>
/// Gestisce la sequenza dei crediti: prima esegue il fade-in del CanvasGroup,
/// poi avvia lo scroll del content (RectTransform con Content Size Fitter).
/// </summary>
public class CreditsScroller : MonoBehaviour
{
    [Header("Scroll")]
    [Tooltip("RectTransform del content (quello con il Content Size Fitter) da scrollare.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Velocità di scroll, in unità al secondo.")]
    [SerializeField] private float scrollSpeed = 50f;

    // MODIFICA: sostituito il RectTransform stopPoint con una distanza (float).
    // Con il Content Size Fitter tutto è ancorato in un unico punto, quindi
    // confrontare anchoredPosition con quella di un RectTransform separato
    // dava errori; una distanza relativa alla posizione di partenza del
    // content evita il problema.
    [Tooltip("Distanza (in unità) di cui il content deve scorrere prima di fermarsi.")]
    [SerializeField] private float stopDistance = 500f;

    // Posizione di partenza e target, calcolate a runtime in Start().
    private Vector2 startPosition;
    private Vector2 targetPosition;

    [Header("Debug")]
    [Tooltip("Se true, in Start() salta fade-in e scroll e porta subito il content sul target.")]
    [SerializeField] private bool isDebugMode = false;

    [Header("Fade")]
    [Tooltip("CanvasGroup su cui viene eseguito il fade-in.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Velocità di fade, in unità di alpha al secondo.")]
    [SerializeField] private float FadeSpeed = 1f;

    private void Start()
    {
        // MODIFICA: target calcolato come startPosition + distanza verso il basso,
        // invece di leggere anchoredPosition da un RectTransform esterno.
        startPosition = content.anchoredPosition;
        targetPosition = startPosition + Vector2.down * stopDistance;

        // Se isDebugMode è true, salta fade-in e scroll e porta subito il
        // content sul target, per verificarlo rapidamente.
        if (isDebugMode)
        {
            canvasGroup.alpha = 1f;
            content.anchoredPosition = targetPosition;
            return;
        }

        StartCoroutine(PlayCreditsSequence());
    }

    // Coroutine "master" chiamata in Start: orchestra fade-in -> scroll.
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

        canvasGroup.alpha = 1f; // clamp, evita overshoot oltre 1
    }

    private IEnumerator ScrollCredits()
    {
        // MODIFICA: il target ora è targetPosition (startPosition + stopDistance),
        // calcolato in Start(), non più la anchoredPosition di un RectTransform esterno.
        while (content.anchoredPosition != targetPosition)
        {
            content.anchoredPosition = Vector2.MoveTowards(
                content.anchoredPosition,
                targetPosition,
                scrollSpeed * Time.deltaTime);
            yield return null;
        }
    }
}