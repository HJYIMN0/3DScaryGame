using System;
using UnityEngine;

public class InteractableCoffeeMachine : AbstractInteractable
{
    [SerializeField] private int differentDialogueIndex = 100;
    public override void InteractWithTask()
    {
        if (!HasBeenInteractedWith)
        {
            MarkTaskAsComplete();
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
        }
        else
        {
            ShowDialogue(task.inkJson, task.usesVariablesInInk, differentDialogueIndex);
        }
        
    }

}
