using System;
using UnityEngine;

public class InteractableCoffeeMachine : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        if (!HasBeenCompleted)
        {
            MarkTaskAsComplete();
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
        }
        else
        {
            ShowDialogue(task.alreadyCompletedTaskJson, false);
        }
        
    }

}
