using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : GenericSingleton<GameFlowManager >
{
    [SerializeField] private Scene[] gameScenes;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private GameObject fadeCanvaPrefab;
    public Scene[] GameScenes => gameScenes;
    public int CurrentDay { get; private set; } = 0;
    public Scene CurrentScene => gameScenes[CurrentDay];

    public void LoadNextScene(int day)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }
        CurrentDay = day;
        StartCoroutine(FadeToLoad(gameScenes[CurrentDay], fadeCanvaPrefab, fadeDuration));
    }

    private IEnumerator FadeToLoad(Scene scene, GameObject objToFade, float fadeDuration)
    {
        StopAllCoroutines(); // Stop any ongoing fade coroutines
        Instantiate(objToFade, Vector3.zero, Quaternion.identity);
        CanvasGroup canvasGroup = objToFade.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 1f; // Ensure it's fully faded

            SceneManager.LoadSceneAsync(scene.name);
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals(scene.name)); //Wait until the new scene is fully loaded

            if (!SceneManager.GetActiveScene().name.Equals(gameScenes[CurrentDay]))
            {
                Debug.LogError($"Failed to load scene: {scene.name}. Current active scene is: {SceneManager.GetActiveScene().name}");
            }
            else
            {
                Debug.Log($"Successfully loaded scene: {scene.name}");
            }

            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            canvasGroup.alpha = 0f; // Ensure it's fully visible
            Destroy(objToFade); // Clean up the fade canvas after transition
            
        }
        else 
        {
            Debug.LogError($"{this.gameObject.name} couldn't find the canvasGroup component on {objToFade.name}");
        }
    }

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;
}
