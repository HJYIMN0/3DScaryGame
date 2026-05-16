using System;
using Unity.VisualScripting;
using UnityEngine;

public class InteractableBathroom : AbstractInteractable
{
    [Header("Materials Attributes")]
    [SerializeField] private Material cleanMTL;

    [Header("Bathroom GameObjs")]
    [SerializeField] private Renderer[] bathroomObjs;

    [Header("Fader")]
    [SerializeField] private GameObject faderPrefab;
    [SerializeField] private float fadeDuration = 1f;

    public override void ExecuteInteraction()
    {
        if (HasBeenCompleted)
        {
            Debug.Log("Bathroom has already been interacted with. No further action taken.");
            return;
        }
        GameObject faderObj = Instantiate(faderPrefab);
        Fader fader = faderObj.GetComponent<Fader>();
        if (fader != null)
        {
            fader.StartCoroutine(fader.FadeIn(fadeDuration));
            fader.onFadeComplete += (bool fadedIn) =>
            {
                if (fadedIn)
                {
                    ChangeBathroomState(cleanMTL);
                    fader.StartCoroutine(fader.FadeOut(fadeDuration));
                }
            };
        }
        HasBeenCompleted = true;
    }

    private void ChangeBathroomState(Material mat)
    {
        foreach (Renderer renderer in bathroomObjs)
        {
            if (renderer != null)
            {
                renderer.material = mat;
                Debug.Log($"Changed material of {renderer.gameObject.name} to {mat.name}.");
            }
        }
    }
}
