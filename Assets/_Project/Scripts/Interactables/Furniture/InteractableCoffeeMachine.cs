using System;
using UnityEngine;

public class InteractableCoffeeMachine : AbstractInteractable
{
    [SerializeField] private int differentDialogueIndex = 100;
    public override void InteractWithTask()
    {
        if (!taskManager.HasAnsweredThePhone)
        {
            Debug.Log("Player interacted with the clothes, but hasn't answered the phone yet.");
            ShowDialogue(task.answerThePhoneText, false);
            return;
        }

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
