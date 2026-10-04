using UnityEngine;

public class InteractableHole : AbstractInteractable
{
    [SerializeField] private GameObject UiVideoCanva;

    protected override void Start() 
    {
        base.Start();
        TaskManager.Instance.ClearTask(task);
    }
    public override void ExecuteInteraction()
    {
        if (!HasBeenCompleted) 
        {
            StartMiniGame();
        }
        else
        {
            ShowDialogue(task.inkJson, true);
        }
    }

    public void ShowVideoSequence()
    {
        GameObject uiInstance = Instantiate(UiVideoCanva);
        uiInstance.GetComponent<VideoPlayerManager>().OnVideoEnd += () =>
        {
            DeactivateCanvas();
            ShowDialogue(task.inkJson, true);
            taskManager.MarkAllTasksAsComplete();
        };
    }

}
