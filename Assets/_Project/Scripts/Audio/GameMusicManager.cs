// GameMusicManager.cs
using System.Collections;
using UnityEngine;
/// <summary>
/// This is not a singleton.
/// Every level has its own instance of this class, which is responsible for playing the music of that level.
/// Every level has its different array of songs. 
/// And the array must be set in the inspector for each level.
/// </summary>
public class GameMusicManager : GenericAudioPlayer
{
    [Tooltip("This is the array of music clips for the current level.")]
    [SerializeField] private AudioClip[] levelMusicClips;

    [Tooltip("This is the audio source for the transition effect. You must assign two audiosource from the same GameObject that provides the music. So I can transition smoothly between the songs.")]
    [SerializeField] private AudioSource transitionAudioSource;
    private int musicIndex = 0;
    public new bool IsPlaying => audioSource.isPlaying || transitionAudioSource.isPlaying;

    private void Awake()
    {
        if (clip == null && levelMusicClips.Length > 0)
        {
            clip = levelMusicClips[musicIndex];
        }
    }
    public override void Play()
    {
        if (audioSource.isPlaying && transitionAudioSource.isPlaying)
        {
            Debug.LogError("Both audio sources are playing. This should not happen.");
            return;
        }

        AudioSource newPlayingAudioSource = GetNotPlayingAudioSource();
        clip = levelMusicClips[musicIndex];
        audioManager.PlayAudioFromAudioSource(newPlayingAudioSource, clip, isLooping);
    }
    public IEnumerator ChangeSongWithEase(AudioClip newClip, float transitionTime)
    {
        if (audioSource == null && transitionAudioSource == null)
        {
            Debug.LogError("AudioSource or TransitionAudioSource is not assigned in the inspector.");
            yield break;
        }

        AudioSource newAudioSource = GetNotPlayingAudioSource();
        AudioSource currentlyPlayingSource = (newAudioSource == audioSource) ? transitionAudioSource : audioSource;

        audioManager.FadeBetweenAudioSources(currentlyPlayingSource, newAudioSource, newClip, transitionTime, isLooping);

        clip = newClip;
    }

    private AudioSource GetNotPlayingAudioSource()
    {
        if (audioSource.isPlaying && transitionAudioSource.isPlaying)
        {
            Debug.LogError("Both audio sources are playing. This should not happen.");
            return null;
        }

        // MODIFICATO: la logica a 4 branch è stata ridotta a una singola espressione ternaria,
        // preservando la convenzione originale (new = transitionAudioSource quando nessuna
        // delle due sta suonando).
        return transitionAudioSource.isPlaying ? audioSource : transitionAudioSource;
    }

}