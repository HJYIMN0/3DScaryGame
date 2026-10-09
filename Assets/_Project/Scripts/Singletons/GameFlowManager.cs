using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : GenericSingleton<GameFlowManager>
{
    [SerializeField] private string[] gameScenes;
    [SerializeField] private float fadeDuration = 2f;
    public float FadeDuration => fadeDuration;
    [SerializeField] private GameObject fadeCanvaPrefab;

    [Header("Pre-roll audio")]
    [Tooltip("Clip da far partire prima del caricamento della scena. L'indice corrisponde a quello di gameScenes. Elemento vuoto = nessun pre-roll per quella scena.")]
    [SerializeField] private AudioClip[] sceneStartClips;
    [Tooltip("Secondi (in tempo reale) tra l'avvio dell'audio e l'inizio del caricamento della scena.")]
    [SerializeField] private float audioLeadTime = 0.5f;
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

    public void LoadNextDay(float fadeDuration)
    {

        if (CurrentDay < gameScenes.Length - 1)
        {
            LoadScene(CurrentDay + 1, fadeDuration);
        }
    }
    public void LoadScene(int day, float fadeDuration)
    {
        if (isLoadingScene) return;   // silenzioso, o Debug.Log una volta sola
        if (day < 0 || day >= gameScenes.Length)
        {
            Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
            return;
        }

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

        // [MODIFICA] Pre-roll audio: a schermo già nero, prima di LoadSceneAsync, avvio la musica della scena di destinazione.
        // L'indice si ricava da sceneName con Array.IndexOf, così non cambio la firma del metodo.
        int sceneIndex = Array.IndexOf(gameScenes, sceneName);
        if (sceneStartClips != null && sceneIndex >= 0 && sceneIndex < sceneStartClips.Length && sceneStartClips[sceneIndex] != null)
        {
            // [MODIFICA] Uso la sorgente persistente dell'AudioManager (DontDestroyOnLoad): la scena nuova non esiste ancora.
            // fadeTime = 0 perché lo schermo è nero e un taglio netto non si nota.
            AudioManager.Instance.PlayPersistentMusic(sceneStartClips[sceneIndex], true, 0f);
            Debug.Log($"[GameFlowManager] Pre-roll audio '{sceneStartClips[sceneIndex].name}' avviato prima di caricare '{sceneName}'");

            // [MODIFICA] Realtime perché il tempo di gioco potrebbe essere fermo (timeScale = 0) durante la transizione.
            yield return new WaitForSecondsRealtime(audioLeadTime);
        }

        SceneManager.LoadSceneAsync(sceneName);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name.Equals(sceneName) && fader.HasFadedIn);

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