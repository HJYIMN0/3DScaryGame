using UnityEngine;

public class InteractableBed : AbstractInteractable
{
    public override void InteractWithTask()
    {
        if (TaskManager.Instance.AreAllTasksCompleted()) 
        {
            Debug.Log("Player interacted with the bed. Task completed!");
            TaskManager.Instance.CompleteTask(task.TaskName);
            GameFlowManager.Instance.LoadNextScene(GameFlowManager.Instance.CurrentDay + 1);
        }
        else Debug.Log("Player interacted with the bed, but not all tasks are completed yet.");
    }
}
