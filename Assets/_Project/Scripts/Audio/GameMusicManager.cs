// GameMusicManager.cs
using UnityEngine;

/// <summary>
/// Ogni livello ha la sua istanza.
/// Con persistAcrossScenes = true NON tocca nessuna AudioSource della musica:
/// delega tutto all'AudioManager, che possiede lo stato e la sorgente persistente.
/// </summary>
public class GameMusicManager : MonoBehaviour
{
    [Header("Sorgenti LOCALI (usate SOLO se persistAcrossScenes = false, " +
            "oppure esplicitamente da PlayLocal)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource transitionAudioSource;

    [Header("Brani del livello")]
    [SerializeField] private AudioClip[] levelMusicClips;
    [SerializeField] private bool shouldStartPlayingOnAwake = true;
    [SerializeField] private bool isLooping = true;

    [Header("Persistenza")]
    [Tooltip("Se true, la musica vive sulla sorgente persistente dell'AudioManager e sopravvive al cambio scena.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Tooltip("Durata del fade applicato quando questa scena avvia la propria musica " +
             "mentre e' ancora in riproduzione la musica persistente ereditata dalla scena " +
             "precedente (persistAcrossScenes = false). A fine fade la sorgente persistente " +
             "viene lasciata vuota: clip = null e volume = 0.")]
    [SerializeField] private float fadeMusicDuration = 1f;

    private int musicIndex = 0;
    private AudioClip currentClip;

    private void Start()
    {
        if (levelMusicClips != null && levelMusicClips.Length > 0)
            currentClip = levelMusicClips[musicIndex];

        if (shouldStartPlayingOnAwake)
            Play();
    }

    // ==================== API MUSICA PERSISTENTE / LOCALE DI SCENA ====================

    public void Play()
    {
        if (levelMusicClips == null || levelMusicClips.Length == 0)
        {
            Debug.LogError($"GameMusicManager: levelMusicClips vuoto su {gameObject.name}");
            return;
        }
        currentClip = levelMusicClips[musicIndex];
        PlayInternal(currentClip, isLooping);
    }

    public void Play(AudioClip newClip, bool loop)
    {
        if (newClip == null) { Debug.LogError("GameMusicManager: clip null."); return; }
        currentClip = newClip;
        PlayInternal(newClip, loop);
    }

    /// <summary>
    /// Logica:
    ///  1. persistAcrossScenes = true  -> delega al singleton: PlayPersistentMusic
    ///     sostituisce la traccia persistente con crossfade (durata = fadeMusicDuration).
    ///  2. persistAcrossScenes = false && persistente in riproduzione (musica ereditata):
    ///     crossfade persistente -> sorgente LOCALE con durata = fadeMusicDuration.
    ///     A fine fade AudioManager lascia la persistente con clip = null e volume = 0.
    ///  3. persistAcrossScenes = false && nessuna persistente in riproduzione:
    ///     suona la clip su una sorgente locale.
    /// </summary>
    private void PlayInternal(AudioClip newClip, bool loop)
    {
        var am = AudioManager.Instance;

        // 1) Persistente.
        if (persistAcrossScenes)
        {
            am.PlayPersistentMusic(newClip, loop, fadeMusicDuration);
            return;
        }

        // 2) Eredito la musica dalla scena precedente e passo a sorgente locale.
        if (am.IsPersistentPlaying())
        {
            var target = GetFreeLocalSource();
            if (target == null) return;
            am.CrossfadeFromPersistentTo(target, newClip, fadeMusicDuration, loop);
            return;
        }

        // 3) Locale pura.
        var local = GetFreeLocalSource();
        if (local == null) return;
        local.volume = 1f;
        local.clip = newClip;
        local.loop = loop;
        local.Play();
    }

    // ==================== API "SUONA SOLO IN LOCALE" ====================

    /// <summary>
    /// Riproduce una clip su una sorgente LOCALE di questa scena, indipendentemente
    /// dalla modalita' persistAcrossScenes. NON tocca MAI la musica persistente del
    /// singleton: la traccia persistente continua a suonare e questa clip si sovrappone.
    /// </summary>
    public void PlayLocal(AudioClip clip, bool loop)
    {
        if (clip == null) { Debug.LogError("GameMusicManager.PlayLocal: clip null."); return; }

        AudioSource local = GetFreeLocalSource();

        if (local == null)
        {
            Debug.LogWarning("GameMusicManager.PlayLocal: sorgenti locali non disponibili, " +
                             "uso AudioManager.PlaySfx come fallback.");
            AudioManager.Instance.PlaySfx(clip);
            return;
        }

        local.volume = 1f;
        local.clip = clip;
        local.loop = loop;
        local.Play();
    }

    /// <summary>Ferma SOLO l'audio locale di questa scena. Non tocca la musica persistente.</summary>
    public void StopLocal()
    {
        if (audioSource != null) audioSource.Stop();
        if (transitionAudioSource != null) transitionAudioSource.Stop();
    }

    // ==================== STOP "GENERALE" ====================

    /// <summary>
    /// Ferma la musica gestita da questo GameMusicManager.
    ///  - In modalita' persistente: ferma la musica persistente del singleton.
    ///  - In modalita' locale: ferma le sorgenti locali di scena.
    /// Per fermare SOLO la parte locale usa StopLocal().
    /// </summary>
    public void Stop()
    {
        if (persistAcrossScenes)
        {
            AudioManager.Instance.StopPersistentMusic(0f);
            return;
        }
        StopLocal();
    }

    // ==================== HELPER ====================

    private AudioSource GetFreeLocalSource()
    {
        if (audioSource == null || transitionAudioSource == null)
        {
            return null;
        }
        if (audioSource.isPlaying && transitionAudioSource.isPlaying)
        {
            Debug.LogWarning("GameMusicManager: entrambe le sorgenti locali occupate; " +
                             "la nuova clip sostituira' quella piu' vecchia su 'audioSource'.");
            return audioSource;
        }
        return transitionAudioSource.isPlaying ? audioSource : transitionAudioSource;
    }
}