using System;
using System.Collections;
using UnityEngine;

public class Fader : GenericSingleton<Fader>
{
    [SerializeField] private CanvasGroup canvasGroup;

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    public float CanvasGroupAlpha => canvasGroup.alpha;

    /// <summary>
    /// true when screen is black. False when screen is transparent. Useful for knowing when the fade is complete.
    /// </summary>
    public Action<bool> onFadeComplete;

    private void Start()
    {
        canvasGroup.alpha = 0f; // Ensure the canvas starts fully transparent
        canvasGroup.interactable = false; // Disable interaction during fade
        canvasGroup.blocksRaycasts = false;
    }
    public IEnumerator FadeIn(float duration)
    {
        Instantiate(canvasGroup.gameObject, Vector3.zero, Quaternion.identity); // Ensure the canvas is active in the scene
        canvasGroup.alpha = 0f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;
        onFadeComplete?.Invoke(false); // Notify that fade-in is complete (screen is transparent)
    }

    public IEnumerator FadeOut(float duration)
    {
        canvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
        onFadeComplete?.Invoke(true); // Notify that fade-out is complete (screen is black)
    }
}