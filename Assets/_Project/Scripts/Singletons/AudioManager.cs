using UnityEngine;

public class AudioManager : GenericSingleton<AudioManager>
{
    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    public void PlaySfxFromPointAndDestroy(AudioSource source, AudioClip clip)
    {
        AudioSource.PlayClipAtPoint(clip, source.transform.position);
    }

    public void PlaySfxFromAudioSource(AudioSource source, AudioClip clip, bool loop)
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

    public void UodateSfxPosition(AudioSource source, Vector3 newPosition)
    {
        if (source != null)
        {
            source.transform.position = newPosition;
        }
    }
}
