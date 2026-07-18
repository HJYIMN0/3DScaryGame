using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private VideoPlayerManager videoPlayer;

    private void Start()
    {
        videoPlayer.OnVideoEnd += GoToNextScene;
    }

    private void OnDestroy()
    {
        videoPlayer.OnVideoEnd -= GoToNextScene;
    }
    public void GoToNextScene() => GameFlowManager.Instance.LoadScene(GameFlowManager.Instance.CurrentDay + 1);
}
