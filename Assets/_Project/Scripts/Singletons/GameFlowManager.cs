using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : GenericSingleton<GameFlowManager>
{
    [SerializeField] private string[] gameScenes;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private GameObject fadeCanvaPrefab;

    // MODIFICATO: rimosso il campo serializzato "currentDay" (int).
    // Come richiesto, il giorno non è più tracciato manualmente: viene dedotto
    // confrontando SceneManager.GetActiveScene().name con l'array gameScenes.
    public string[] GameScenes => gameScenes;

    // MODIFICATO: CurrentDay è ora una proprietà calcolata al volo invece di un campo.
    // Cerca il nome della scena attiva dentro gameScenes con Array.IndexOf: la posizione
    // trovata è il numero del giorno. Se la scena attiva non è nell'array, ritorna -1
    // e logga un warning (caso che prima, con il campo manuale, non veniva rilevato).
    public int CurrentDay
    {
        get
        {
            string activeSceneName = SceneManager.GetActiveScene().name;
            int index = Array.IndexOf(gameScenes, activeSceneName);

            if (index < 0)
            {
                Debug.LogWarning($"La scena attiva '{activeSceneName}' non è presente in gameScenes. Impossibile determinare il giorno corrente.");
            }

            return index;
        }
    }

    // MODIFICATO: CurrentScene ora ritorna direttamente SceneManager.GetActiveScene().name
    // invece di gameScenes[CurrentDay]. Con CurrentDay dedotto dalla scena attiva,
    // reindicizzare l'array sarebbe ridondante e andrebbe in IndexOutOfRange se la scena
    // attiva non fosse presente in gameScenes (es. CurrentDay == -1).
    public string CurrentScene => SceneManager.GetActiveScene().name;

    private bool isLoadingScene = false;

    private void Start()
    {
        Debug.Log($"GameFlowManager started. Current day: {CurrentDay}, Current scene: {CurrentScene}");
    }

    public void LoadNextDay(float fadeDuration)
    {
        Debug.Log($"Attempting to load next day. Current day: {CurrentDay}, Total days: {gameScenes.Length}");

        if (CurrentDay < gameScenes.Length - 1)
        {
            LoadScene(CurrentDay + 1, fadeDuration);
        }
        else
        {
            Debug.Log("Already at the last day. No next day to load.");
            // Qui puoi aggiungere logica per fine gioco, crediti, ecc.
        }
    }
    public void LoadScene(int day, float fadeDuration)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }

        // MODIFICATO: FadeToLoad non riceve più "day" come parametro, perché non serve
        // più aggiornare manualmente currentDay al termine del caricamento (vedi sotto).
        StartCoroutine(FadeToLoad(gameScenes[day], fadeCanvaPrefab, fadeDuration));
    }
    public void LoadScene(int day)
    {
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeToLoad(gameScenes[day], fadeCanvaPrefab, fadeDuration));
    }


    // MODIFICATO: firma cambiata da FadeToLoad(int day, string sceneName, ...) a
    // FadeToLoad(string sceneName, ...): il parametro "day" serviva solo per fare
    // "currentDay = day;" a fine coroutine, operazione ora superflua perché CurrentDay
    // si aggiorna da solo (in automatico) non appena la scena attiva cambia.
    private IEnumerator FadeToLoad(string sceneName, GameObject objToFade, float fadeDuration)
    {
        if (isLoadingScene)
        {
            Debug.LogWarning("A scene is already loading. Please wait until the current load is complete.");
            yield break;
        }
        isLoadingScene = true;
        GameObject fadeInstance = Instantiate(objToFade, Vector3.zero, Quaternion.identity);
        fadeInstance.transform.SetParent(transform);
        Fader fader = fadeInstance.GetComponent<Fader>();
        fader.StartCoroutine(fader.FadeIn(fadeDuration));
        yield return new WaitUntil(() => fader.HasFadedIn);

        SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals(sceneName) && fader.HasFadedIn);

        Debug.Log($"Successfully loaded scene: {sceneName}");
        fader.StartCoroutine(fader.FadeOut(fadeDuration));

        // MODIFICATO: rimossa "currentDay = day;" — non esiste più il campo currentDay.
        // A questo punto la scena attiva è già quella nuova, quindi CurrentDay la riflette
        // automaticamente al prossimo accesso.

        TaskManager.Instance.SetPhoneAnswered(false);
        isLoadingScene = false;
    }

    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;
}