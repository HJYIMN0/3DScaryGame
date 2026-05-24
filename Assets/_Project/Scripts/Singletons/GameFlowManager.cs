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

    private void Start()
    {
        Debug.Log($"GameFlowManager started. Current day: {currentDay}, Current scene: {CurrentScene}");
    }
    public void LoadScene(int day, float fadeDuration)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }
        
        StartCoroutine(FadeToLoad(day, gameScenes[day], fadeCanvaPrefab, fadeDuration));
    }
    public void LoadScene(int day)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }

        StartCoroutine(FadeToLoad(day, gameScenes[day], fadeCanvaPrefab, fadeDuration));
    }


    private IEnumerator FadeToLoad(int day, string sceneName, GameObject objToFade, float fadeDuration)
    {
        GameObject fadeInstance = Instantiate(objToFade, Vector3.zero, Quaternion.identity);
        fadeInstance.transform.SetParent(transform);
        Fader fader = fadeInstance.GetComponent<Fader>();
        fader.StartCoroutine(fader.FadeIn(fadeDuration));
        yield return new WaitUntil(() => fader.HasFadedIn);

        SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals(sceneName) && fader.HasFadedIn);

        Debug.Log($"Successfully loaded scene: {sceneName}");
        fader.StartCoroutine(fader.FadeOut(fadeDuration));
        currentDay = day;

        TaskManager.Instance.SetPhoneAnswered(false);
    }

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;
}