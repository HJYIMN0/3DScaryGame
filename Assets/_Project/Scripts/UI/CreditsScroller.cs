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

    [Tooltip("Punto (in anchoredPosition) in cui lo scroll deve fermarsi. Posiziona qui un RectTransform vuoto nella UI.")]
    [SerializeField] private RectTransform stopPoint;

    // MODIFICA: flag di debug per saltare fade+scroll e verificare subito lo stopPoint.
    [Header("Debug")]
    [Tooltip("Se true, in Start() salta fade-in e scroll e porta subito il content sullo stopPoint.")]
    [SerializeField] private bool isDebugMode = false;

    [Header("Fade")]
    [Tooltip("CanvasGroup su cui viene eseguito il fade-in.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Tooltip("Velocità di fade, in unità di alpha al secondo.")]
    [SerializeField] private float FadeSpeed = 1f;

    private void Start()
    {
        // MODIFICA: se isDebugMode è true, salta fade-in e scroll e porta
        // subito il content sullo stopPoint, per verificarlo rapidamente.
        if (isDebugMode)
        {
            canvasGroup.alpha = 1f;
            content.anchoredPosition = stopPoint.anchoredPosition;
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
        // MODIFICA: lo scroll ora si ferma esattamente su stopPoint invece di
        // essere infinito. Uso Vector2.MoveTowards così la direzione è dedotta
        // automaticamente dalla posizione di stopPoint (non serve più Vector2.down/up
        // a mano) e non si verifica overshoot oltre il target.
        while (content.anchoredPosition != stopPoint.anchoredPosition)
        {
            content.anchoredPosition = Vector2.MoveTowards(
                content.anchoredPosition,
                stopPoint.anchoredPosition,
                scrollSpeed * Time.deltaTime);
            yield return null;
        }
    }
}