
public class InteractableSlidingPuzzle : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        if (HasBeenCompleted)
        {
            return;
        }

        ShowDialogue(task.inkJson, task.usesVariablesInInk);
        StartMiniGame();
    }
}