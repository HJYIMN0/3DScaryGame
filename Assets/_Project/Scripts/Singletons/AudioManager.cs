// AudioManager.cs
using System.Collections;
using UnityEngine;

public class AudioManager : GenericSingleton<AudioManager>
{
    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;
    private AudioSource _playerAudioSource;

    public void PlayGameMusic(AudioClip clip, bool loop)
    {
        if (_playerAudioSource == null)
        {
            _playerAudioSource = GameObject.FindGameObjectWithTag("Player").GetComponent<AudioSource>();
            if (_playerAudioSource == null)
            {
                Debug.LogError("Player audio source not found!");
            }
        }
        PlayAudioFromAudioSource(_playerAudioSource, clip, loop);
    }

    public void PlaySfxFromPointAndDestroy(AudioSource source, AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, source.transform.position);
    }

    public void PlayAudioFromAudioSource(AudioSource source, AudioClip clip, bool loop)
    {
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }

    public void PlaySfxMoving(AudioSource source, AudioClip clip, bool loop)
    {
        if (source != null && clip != null)
        {
            source.clip = clip;
            source.loop = loop;

            source.spatialBlend = 1f; // Set spatial blend to 3D
            source.Play();
        }
    }
    public Coroutine FadeBetweenAudioSources(AudioSource currentlyPlayingSource, AudioSource newAudioSource, AudioClip newClip, float transitionTime, bool loop)
    {
        return StartCoroutine(FadeRoutine(currentlyPlayingSource, newAudioSource, newClip, transitionTime, loop));
    }

    private IEnumerator FadeRoutine(AudioSource currentlyPlayingSource, AudioSource newAudioSource, AudioClip newClip, float transitionTime, bool loop)
    {
        newAudioSource.volume = 0f;
        PlayAudioFromAudioSource(newAudioSource, newClip, loop);

        float startVolume = currentlyPlayingSource.volume;

        float timer = 0f;
        while (timer < transitionTime)
        {
            timer += Time.deltaTime;
            currentlyPlayingSource.volume = Mathf.Lerp(startVolume, 0f, timer / transitionTime);
            newAudioSource.volume = Mathf.Lerp(0f, 1f, timer / transitionTime);
            yield return null;
        }
        newAudioSource.volume = 1f;

        currentlyPlayingSource.volume = 0f;
        currentlyPlayingSource.Stop();
    }
}