using System;
using UnityEngine;

public class InteractableBed : AbstractInteractable
{

    [SerializeField] private string notAllTasksCompletedDialogueKey = "I can't go to bed yet.";
    [SerializeField] private float fadeDuration = 2f;
    public override void ExecuteInteraction()
    {

        Debug.Log("Interacted with bed! Checking if all tasks are completed...");
        if (taskManager.AreAllTasksCompleted())
        {
            Debug.Log("All tasks are completed. Proceeding with bed interaction.");
            MarkTaskAsComplete();
            ShowDialogue(task.inkJson, task.usesVariablesInInk);
        }
        else
        {
            Debug.Log("Player interacted with the bed, but not all tasks are completed yet.");
             ShowDialogue(notAllTasksCompletedDialogueKey);
        }
    }

    public override void OnDialogueEnd(TextAsset dialogue)
    {
        base.OnDialogueEnd(dialogue);
        if (dialogue == task.inkJson && TaskManager.Instance.AreAllTasksCompleted())
        {
            Debug.Log("Dialogue ended for task: " + task.TaskName);
            GameFlowManager.Instance.LoadScene(GameFlowManager.Instance.CurrentDay + 1, fadeDuration);
        }
    }
}
