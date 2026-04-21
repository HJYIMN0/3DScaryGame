using UnityEngine;

public class Interactable_Hole : AbstractInteractable
{
    [SerializeField] private GameObject UiVideoCanva;

    protected override void Start() 
    {
        TaskManager.Instance.ClearTask(task);
    }
    public override void InteractWithTask()
    {
        if (!HasBeenInteractedWith) 
        {
            GameObject uiInstance = Instantiate(UiVideoCanva);
            uiInstance.GetComponent<VideoPlayerManager>().OnVideoEnd += () =>
            {
                DeactivateCanvas();
                Debug.Log("Video ended, showing dialogue...");
                ShowDialogue(task.inkJson, true);
            };
            TaskManager.Instance.CompleteTask(task.TaskName);
            HasBeenInteractedWith = true;
        }
        else
        {
            Debug.Log("Already interacted with the hole.");
            ShowDialogue(task.inkJson, true);
        }
    }

}
