using UnityEngine;

public class StaticAudioPlayer : GenericAudioPlayer
{
    public override void Play()
    {
        audioManager.PlayAudioFromAudioSource(audioSource, clip, isLooping);
    }
}