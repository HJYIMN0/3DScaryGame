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
            GameObject uiInstance = Instantiate(UiVideoCanva);
            uiInstance.GetComponent<VideoPlayerManager>().OnVideoEnd += () =>
            {
                DeactivateCanvas();
                Debug.Log("Video ended, showing dialogue...");
                ShowDialogue(task.inkJson, true);
                taskManager.MarkAllTasksAsComplete();
            };
            TaskManager.Instance.CompleteTask(task);
            HasBeenCompleted = true;
        }
        else
        {
            Debug.Log("Already interacted with the hole.");
            ShowDialogue(task.inkJson, true);
        }
    }

}
