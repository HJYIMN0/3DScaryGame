using UnityEngine;

public class AudioManager : GenericSingleton<AudioManager>
{
    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    public void PlaySfxSoundFromSource(AudioSource source, AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, source.transform.position);
    }
}
