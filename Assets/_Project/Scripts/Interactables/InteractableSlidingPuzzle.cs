using UnityEngine;

public class InteractableSlidingPuzzle : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        if (HasBeenCompleted)
        {
            Debug.Log($"Player has already completed the puzzle in {name}. No need to interact again.");
            return;
        }

        ShowDialogue(task.inkJson, task.usesVariablesInInk);

        Debug.Log($"Player interacted with {name}. Starting sliding puzzle minigame.");
        StartMiniGame();
    }
}