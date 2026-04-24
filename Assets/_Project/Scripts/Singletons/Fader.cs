using System;
using System.Collections;
using UnityEngine;

public class Fader : GenericSingleton<Fader>
{
    [SerializeField] private CanvasGroup canvasGroup;

    private GameObject objToFade;
    private CanvasGroup canvaToFade;

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    public float CanvasGroupAlpha => canvaToFade.alpha;

    /// <summary>
    /// true when screen is black. False when screen is transparent. Useful for knowing when the fade is complete.
    /// </summary>
    public Action<bool> onFadeComplete;

    private void Start()
    {
    }
    public IEnumerator FadeIn(float duration)
    {
        Debug.Log("Fader callde for FadeIn!");
        if (objToFade == null)
        {
            objToFade = Instantiate(canvasGroup.gameObject, Vector3.zero, Quaternion.identity); // Ensure the canvas is active in the scene
            canvaToFade = objToFade.GetComponent<CanvasGroup>();
        }
        canvaToFade.alpha = 0f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvaToFade.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvaToFade.alpha = 1f;
        onFadeComplete?.Invoke(false); // Notify that fade-in is complete (screen is transparent)
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