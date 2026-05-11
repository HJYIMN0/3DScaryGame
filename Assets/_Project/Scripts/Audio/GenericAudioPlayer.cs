using UnityEngine;
public abstract class GenericAudioPlayer: MonoBehaviour
{
    [SerializeField] protected AudioSource audioSource;
    [SerializeField] protected AudioClip clip;
    [SerializeField] protected bool isLooping;
    [SerializeField] private protected bool playOnStart = true;

    protected AudioManager audioManager;
    public bool IsPlaying => audioSource.isPlaying;

    public virtual void Start()
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing on " + gameObject.name);
            return;
        }
        if (clip == null)
        {
            Debug.LogError("AudioClip is not assigned on " + gameObject.name);
            return;
        }

        audioManager = AudioManager.Instance;

        if (playOnStart)
        {
            Play();
        }

    }

    public abstract void Play();

    public virtual void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
