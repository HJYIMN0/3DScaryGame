using UnityEngine;

public class InteractableGeneral : AbstractInteractable
{
    public override void InteractWithTask()
    {
        if (HasBeenInteractedWith) 
        {
            Debug.Log($"Player has already interacted with {name}. No need to interact again.");
            return;
        }
        Debug.Log($"Player interacted with {name}. Task completed!");
        TaskManager.Instance.CompleteTask(task.TaskName);
        HasBeenInteractedWith = true;
    }
}
