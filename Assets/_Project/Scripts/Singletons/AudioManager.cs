// AudioManager.cs
using System.Collections;
using UnityEngine;

public class AudioManager : GenericSingleton<AudioManager>
{
    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    // ==================== MUSICA PERSISTENTE ====================
    private AudioSource _persistentMusicSource;
    private AudioClip _persistentMusicClip;
    private bool _persistentMusicLoop;
    private AudioSource _persistentFadeSource;   // solo per crossfade
    private Coroutine _persistentRoutine;

    // ==================== SFX ====================
    private Transform _sfxRoot;
    private AudioSource _sfxSource2D;

    // ==================== LEGACY ====================
    private AudioSource _playerAudioSource;

    public override void Awake()
    {
        base.Awake();

        // --- Sorgente musica persistente: figlio dedicato ---
        var musicGO = new GameObject("~PersistentMusicSource");
        musicGO.transform.SetParent(transform, false);
        _persistentMusicSource = musicGO.AddComponent<AudioSource>();
        _persistentMusicSource.playOnAwake = false;
        _persistentMusicSource.spatialBlend = 0f;   // 2D
        _persistentMusicSource.volume = 1f;
        _persistentMusicSource.loop = true;

        // --- Radice per i SFX ---
        var sfxRootGO = new GameObject("~SfxRoot");
        sfxRootGO.transform.SetParent(transform, false);
        _sfxRoot = sfxRootGO.transform;

        var sfxGO = new GameObject("~Sfx2DSource");
        sfxGO.transform.SetParent(_sfxRoot, false);
        _sfxSource2D = sfxGO.AddComponent<AudioSource>();
        _sfxSource2D.playOnAwake = false;
        _sfxSource2D.spatialBlend = 0f;
        _sfxSource2D.volume = 1f;
        _sfxSource2D.loop = false;
    }

    // =====================================================================
    //  API MUSICA PERSISTENTE  (unico accesso possibile alla musica)
    // =====================================================================

    public bool IsPersistentPlaying()
        => _persistentMusicSource != null && _persistentMusicSource.isPlaying;

    public AudioClip GetPersistentClip() => _persistentMusicClip;

    /// <summary>
    /// Avvia o SOSTITUISCE la musica persistente.
    ///  - Stessa clip gia' in riproduzione -> idempotente.
    ///  - Nessuna musica in play             -> avvio diretto.
    ///  - Altra clip in play, fade<=0        -> taglio netto.
    ///  - Altra clip in play, fade>0         -> crossfade (vecchia su _persistentFadeSource).
    /// </summary>
    public void PlayPersistentMusic(AudioClip clip, bool loop = true, float fadeTime = 0f)
    {
        if (clip == null) { Debug.LogWarning("[AudioManager] PlayPersistentMusic: clip null."); return; }
        if (_persistentMusicSource == null) return;

        if (_persistentMusicClip == clip && _persistentMusicSource.isPlaying) return;

        StopPersistentRoutine();
        _persistentMusicClip = clip;
        _persistentMusicLoop = loop;

        if (!_persistentMusicSource.isPlaying || fadeTime <= 0f)
        {
            _persistentMusicSource.volume = 1f;
            _persistentMusicSource.clip = clip;
            _persistentMusicSource.loop = loop;
            _persistentMusicSource.Play();
            return;
        }

        _persistentRoutine = StartCoroutine(CrossfadePersistentRoutine(clip, loop, fadeTime));
    }

    public void StopPersistentMusic(float fadeTime = 0f)
    {
        StopPersistentRoutine();
        _persistentMusicClip = null;

        if (_persistentMusicSource == null) return;

        if (!_persistentMusicSource.isPlaying || fadeTime <= 0f)
        {
            _persistentMusicSource.Stop();
            _persistentMusicSource.clip = null;
            _persistentMusicSource.volume = 1f;
            return;
        }

        _persistentRoutine = StartCoroutine(FadeOutPersistentRoutine(fadeTime));
    }

    public void ReleasePersistentSource() => StopPersistentMusic(0f);

    /// <summary>
    /// Cede il controllo dalla musica persistente ad una sorgente LOCALE.
    /// Usato dalle scene che NON vogliono persistere ma ereditano una traccia.
    /// </summary>
    public void CrossfadeFromPersistentTo(AudioSource target, AudioClip newClip, float fadeTime, bool loop)
    {
        if (target == null || newClip == null) return;
        StopPersistentRoutine();
        _persistentMusicClip = null;
        _persistentRoutine = StartCoroutine(CrossfadePersistentToLocalRoutine(target, newClip, fadeTime, loop));
    }

    // =====================================================================
    //  Routine interne musica
    // =====================================================================

    private void StopPersistentRoutine()
    {
        if (_persistentRoutine == null) return;
        StopCoroutine(_persistentRoutine);
        _persistentRoutine = null;
    }

    private AudioSource GetOrCreateFadeSource()
    {
        if (_persistentFadeSource == null)
        {
            var go = new GameObject("~PersistentFadeSource");
            go.transform.SetParent(transform, false);
            _persistentFadeSource = go.AddComponent<AudioSource>();
            _persistentFadeSource.playOnAwake = false;
            _persistentFadeSource.spatialBlend = 0f;
            _persistentFadeSource.volume = 1f;
            _persistentFadeSource.loop = false;
        }
        return _persistentFadeSource;
    }

    private IEnumerator CrossfadePersistentRoutine(AudioClip newClip, bool loop, float fadeTime)
    {
        var fade = GetOrCreateFadeSource();

        fade.clip = _persistentMusicSource.clip;
        fade.loop = _persistentMusicSource.loop;
        fade.time = _persistentMusicSource.time;
        fade.volume = _persistentMusicSource.volume;
        fade.Play();

        _persistentMusicSource.clip = newClip;
        _persistentMusicSource.loop = loop;
        _persistentMusicSource.volume = 0f;
        _persistentMusicSource.Play();

        float startVol = fade.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            if (fade == null || _persistentMusicSource == null) { _persistentRoutine = null; yield break; }
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            fade.volume = Mathf.Lerp(startVol, 0f, k);
            _persistentMusicSource.volume = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }

        if (fade != null) { fade.Stop(); fade.clip = null; fade.volume = 1f; }
        if (_persistentMusicSource != null) _persistentMusicSource.volume = 1f;
        _persistentRoutine = null;
    }

    private IEnumerator FadeOutPersistentRoutine(float fadeTime)
    {
        float startVol = _persistentMusicSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            if (_persistentMusicSource == null) { _persistentRoutine = null; yield break; }
            t += Time.unscaledDeltaTime;
            _persistentMusicSource.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(t / fadeTime));
            yield return null;
        }
        if (_persistentMusicSource != null)
        {
            _persistentMusicSource.Stop();
            _persistentMusicSource.clip = null;
            _persistentMusicSource.volume = 1f;
        }
        _persistentRoutine = null;
    }

    private IEnumerator CrossfadePersistentToLocalRoutine(AudioSource target, AudioClip newClip, float fadeTime, bool loop)
    {
        target.volume = 0f;
        target.clip = newClip;
        target.loop = loop;
        target.Play();

        float startVol = _persistentMusicSource.volume;
        float t = 0f;
        while (t < fadeTime)
        {
            if (target == null || _persistentMusicSource == null) { _persistentRoutine = null; yield break; }
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            _persistentMusicSource.volume = Mathf.Lerp(startVol, 0f, k);
            target.volume = Mathf.Lerp(0f, 1f, k);
            yield return null;
        }

        if (target != null) target.volume = 1f;
        if (_persistentMusicSource != null)
        {
            _persistentMusicSource.Stop();
            _persistentMusicSource.clip = null;
            _persistentMusicSource.volume = 0f;   // <-- MODIFICATO: prima era 1f
        }
        _persistentRoutine = null;
    }

    // =====================================================================
    //  API SFX
    // =====================================================================

    /// <summary>SFX 2D globale: PlayOneShot su sorgente dedicata. Non tocca nulla di esistente.</summary>
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        _sfxSource2D.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// SFX posizionale: crea un GameObject temporaneo sotto ~SfxRoot, lo suona e
    /// lo distrugge. Sorgente indipendente da qualsiasi cosa esistente.
    /// </summary>
    public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        var go = new GameObject("~SfxOneShot");
        go.transform.SetParent(_sfxRoot, false);
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 1f;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    /// <summary>SFX su una sorgente specifica, senza sostituirne il clip (PlayOneShot).</summary>
    public void PlaySfxOneShot(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, volume);
    }

    // =====================================================================
    //  LEGACY
    // =====================================================================

    public void PlayGameMusic(AudioClip clip, bool loop)
    {
        if (_playerAudioSource == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerAudioSource = player.GetComponent<AudioSource>();
            if (_playerAudioSource == null) { Debug.LogError("Player audio source not found!"); return; }
        }
        PlayAudioFromAudioSource(_playerAudioSource, clip, loop);
    }

    /// <summary>DEPRECATO: usa PlaySfxAtPosition. Internamente non tocca la sorgente passata.</summary>
    public void PlaySfxFromPointAndDestroy(AudioSource source, AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = source != null ? source.transform.position : Vector3.zero;
        PlaySfxAtPosition(clip, pos);
    }

    /// <summary>ATTENZIONE: sostituisce il clip della sorgente. Mai su sorgenti di musica.</summary>
    public void PlayAudioFromAudioSource(AudioSource source, AudioClip clip, bool loop)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }

    public void PlaySfxMoving(AudioSource source, AudioClip clip, bool loop)
    {
        if (source == null || clip == null) return;
        source.clip = clip;
        source.loop = loop;
        source.spatialBlend = 1f;
        source.Play();
    }
}