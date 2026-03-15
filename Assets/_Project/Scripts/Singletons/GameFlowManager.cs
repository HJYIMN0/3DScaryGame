using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : GenericSingleton<GameFlowManager>
{
    [SerializeField] private string[] gameScenes;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private GameObject fadeCanvaPrefab;
    [SerializeField] private int currentDay = 0;
    public string[] GameScenes => gameScenes;
    public int CurrentDay => currentDay;
    public string CurrentScene => gameScenes[CurrentDay];

    public void LoadNextScene(int day)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }
        
        StartCoroutine(FadeToLoad(gameScenes[day], fadeCanvaPrefab, fadeDuration));
    }

    private IEnumerator FadeToLoad(string sceneName, GameObject objToFade, float fadeDuration)
    {
        // MODIFICATO: StopAllCoroutines() era dentro la coroutine e la fermava
        // immediatamente. Ora non si ferma più da sola.
        // Se vuoi evitare coroutine sovrapposte, usa un flag booleano esterno.

        // MODIFICATO: salviamo il riferimento al clone istanziato. Prima si chiamava
        // GetComponent e Destroy sull'originale (il prefab), non sul clone.
        GameObject fadeInstance = Instantiate(objToFade, Vector3.zero, Quaternion.identity);
        fadeInstance.transform.SetParent(transform);
        CanvasGroup canvasGroup = fadeInstance.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            Debug.LogError($"{gameObject.name} couldn't find CanvasGroup on {objToFade.name}");
            yield break;
        }

        canvasGroup.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals(sceneName));

        Debug.Log($"Successfully loaded scene: {sceneName}");

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // MODIFICATO: Destroy sul clone, non sul prefab originale
        Destroy(fadeInstance);
    }

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;
}