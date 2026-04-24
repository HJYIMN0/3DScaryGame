using UnityEngine;

public class InteractableBed : AbstractInteractable
{

    [SerializeField] private int notAllTasksCompletedDialogueIndex = 100;

    private void OnEnable()
    {
        InkManager.Instance.onDialogueEnd += HandleDialogueEnd;
    }
    public override void InteractWithTask()
    {
        if (TaskManager.Instance.AreAllTasksCompleted())
        {
            Debug.Log("Player interacted with the bed. Task completed!");
            MarkTaskAsComplete();
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
            GameFlowManager.Instance.LoadNextScene(GameFlowManager.Instance.CurrentDay + 1);
        }
        else
        {
            Debug.Log("Player interacted with the bed, but not all tasks are completed yet.");
             ShowDialogue(task.inkJson, task.usesVariablesInInk, notAllTasksCompletedDialogueIndex);
        }
    }

    public void HandleDialogueEnd(TextAsset ts)
    {
        if (ts == task.inkJson && TaskManager.Instance.AreAllTasksCompleted())
        {
            Debug.Log("Dialogue ended for task: " + task.TaskName);
            GameFlowManager.Instance.LoadNextScene(GameFlowManager.Instance.CurrentDay + 1);
        }
    }
}
