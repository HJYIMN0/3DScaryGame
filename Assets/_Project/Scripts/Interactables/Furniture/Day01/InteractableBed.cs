using UnityEngine;

public class InteractableBed : AbstractInteractable
{

    [SerializeField] private string notAllTasksCompletedDialogueKey = "I can't go to bed yet.";
    [SerializeField] private float fadeDuration = 2f;
    public override void ExecuteInteraction()
    {
        if (taskManager.AreAllTasksCompleted())
        {
            MarkTaskAsComplete();
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
        }
        else
        {
             ShowDialogue(notAllTasksCompletedDialogueKey);
        }
    }

    public override void OnDialogueEnd(TextAsset dialogue)
    {
        base.OnDialogueEnd(dialogue);
        if (dialogue == task.inkJson && TaskManager.Instance.AreAllTasksCompleted())
        {
            GameFlowManager.Instance.LoadScene(GameFlowManager.Instance.CurrentDay + 1, fadeDuration);
        }
    }
}
