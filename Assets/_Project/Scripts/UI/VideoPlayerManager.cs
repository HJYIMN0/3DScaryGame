using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private PlayerMovementController playerMovementController;

    public bool IsVideoPlaying => videoPlayer != null && videoPlayer.isPlaying;

    private void OnEnable()
    {        
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
        Debug.Log($"[VideoPlayerManager] Video avviato. Loop attivo: {videoPlayer.isLooping}");
        SetPlayerMovement(false);
    }

    private void OnDisable()
    {
        SetPlayerMovement(false);
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnDestroy()
    {
        SetPlayerMovement(false);
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[VideoPlayerManager] OnVideoFinished chiamato.");
        Destroy(this.gameObject);
        SetPlayerMovement(true);
    }

    public void SetPlayerMovement(bool canMove)
    {
        {
            switch (canMove)
            {
                case true:

                    if (playerMovementController == null)
                    {
                        playerMovementController = GameObject.FindWithTag("Player").GetComponent<PlayerMovementController>();
                    }

                    if (playerMovementController == null)
                    {
                        Debug.LogWarning("Hole interaction: PlayerMovementController not found on player. Cannot stop movement.");
                        return;
                    }
                        

                    if (!playerMovementController.CanMove)
                        playerMovementController.StartMovement();

                    break;

                case false:

                    if (playerMovementController == null)
                    {
                        playerMovementController = GameObject.FindWithTag("Player").GetComponent<PlayerMovementController>();
                    }

                    if (playerMovementController == null)
                    {
                        Debug.LogWarning("Hole interaction: PlayerMovementController not found on player. Cannot stop movement.");
                        return;
                    }
                        

                    if (playerMovementController.CanMove)
                        playerMovementController.StopMovement();

                    break;

            }
        }

    }
}
