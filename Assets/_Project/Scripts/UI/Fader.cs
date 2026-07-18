using System;
using System.Collections;
using UnityEngine;

public class Fader : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvaToFade;

    public float CanvasGroupAlpha => canvaToFade.alpha;
    public bool IsFading => canvaToFade.alpha > 0f && canvaToFade.alpha < 1f;
    public bool HasFadedIn => canvaToFade.alpha >= 1f;
    public bool HasFadedOut => canvaToFade.alpha <= 0f;

    /// <summary>
    /// true when screen is black. False when screen is transparent. Useful for knowing when the fade is complete.
    /// </summary>
    public Action<bool> onFadeComplete;
    public IEnumerator FadeIn(float duration)
    {
        canvaToFade.alpha = 0f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvaToFade.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvaToFade.alpha = 1f;
        onFadeComplete?.Invoke(true); 
    }

    public IEnumerator FadeOut(float duration)
    {
        canvaToFade.alpha = 1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // FIX: era Lerp(0f, 1f, ...) — direzione invertita, causava doppio fade visivo
            canvaToFade.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvaToFade.alpha = 0f;
        canvaToFade.interactable = false;
        canvaToFade.blocksRaycasts = false;
        // FIX: era Invoke(true) — ma "true = schermo nero", qui lo schermo è trasparente
        onFadeComplete?.Invoke(false);
        Destroy(this.gameObject); // Clean up the fade object after fading out
    }

    public IEnumerator FadeOut(float waitTimer, float duration)
    {
        canvaToFade.alpha = 1f;
        yield return new WaitForSeconds(waitTimer);
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            // FIX: stesso errore di direzione del lerp
            canvaToFade.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvaToFade.alpha = 0f;
        canvaToFade.interactable = false;
        canvaToFade.blocksRaycasts = false;
        // FIX: stesso errore sul booleano
        onFadeComplete?.Invoke(false);
    }


}