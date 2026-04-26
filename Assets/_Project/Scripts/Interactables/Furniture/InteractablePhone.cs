using UnityEngine;

public class InteractablePhone : AbstractInteractable
{
    protected override void Start()
    {
        base.Start();
        if (taskManager.HasAnsweredThePhone) 
        {
            taskManager.SetPhoneAnswered(false);
        }
    }
    public override void ExecuteInteraction()
    {
        if (!HasBeenInteractedWith)
        {
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
            MarkTaskAsComplete();
            taskManager.SetPhoneAnswered(true);
        }
        else
        {
            ShowDialogue(task.alreadyCompletedTaskJson, false);
        }

    }

}
