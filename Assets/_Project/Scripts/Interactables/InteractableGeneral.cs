using UnityEngine;

public class InteractableGeneral : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        ShowDialogue(task.inkJson, task.usesVariablesInInk);
        
        if (HasBeenCompleted) 
        {
            Debug.Log($"Player has already interacted with {name}. No need to interact again.");
            return;
        }
        Debug.Log($"Player interacted with {name}. Task completed!");
        TaskManager.Instance.CompleteTask(task.TaskName);
        HasBeenCompleted = true;
    }
}
