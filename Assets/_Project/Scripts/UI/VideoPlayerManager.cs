using System;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private bool isPlayerInScene = true;


    private PlayerMovementController playerMovementController;

    public bool IsVideoPlaying => videoPlayer != null && videoPlayer.isPlaying;

    public Action OnVideoEnd;
    private void OnEnable()
    {        
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
        Debug.Log($"[VideoPlayerManager] Video avviato. Loop attivo: {videoPlayer.isLooping}");
        SetPlayerMovement(false, isPlayerInScene);
    }

    private void OnDisable()
    {
        SetPlayerMovement(false, isPlayerInScene);
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnDestroy()
    {
        SetPlayerMovement(false, isPlayerInScene);
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[VideoPlayerManager] OnVideoFinished chiamato.");
        Destroy(this.gameObject);
        SetPlayerMovement(true, isPlayerInScene);
        OnVideoEnd?.Invoke();
    }

    public void SetPlayerMovement(bool canMove, bool isPlayerInScene)
    {
        if (!isPlayerInScene) return;

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
