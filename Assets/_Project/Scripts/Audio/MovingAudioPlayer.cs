using UnityEngine;

public class MovingAudioPlayer : GenericAudioPlayer
{
    [Header("Attributes")]
    [SerializeField] private GameObject player;


    [Header("Moving settings")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float distanceFromPlayer = 1f;

    private float _angle;



    public override void Start()
    {
        base.Start();
        _angle = 0;
    }
    private void Update()
    {
        if (!IsPlaying) return;

        _angle += moveSpeed * Time.deltaTime;
        audioSource.gameObject.transform.position = player.transform.position + new Vector3(Mathf.Cos(_angle), 0, Mathf.Sin(_angle)) * distanceFromPlayer;
    }

    public override void Play()
    {
        audioManager.PlaySfxMoving(audioSource, clip, isLooping);
    }

}
