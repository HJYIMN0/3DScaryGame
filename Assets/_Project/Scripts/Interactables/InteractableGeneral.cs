public class InteractableGeneral : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        ShowDialogue(task.inkJson, task.usesVariablesInInk);
        
        if (HasBeenCompleted) 
        {
            return;
        }
        TaskManager.Instance.CompleteTask(task);
        HasBeenCompleted = true;
    }
}
