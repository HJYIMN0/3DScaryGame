using UnityEngine;

public class StaticAudioPlayer : GenericAudioPlayer
{
    public override void Play()
    {
        audioManager.PlaySfxFromAudioSource(audioSource, clip, isLooping);
    }
}