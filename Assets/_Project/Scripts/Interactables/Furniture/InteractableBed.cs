using UnityEngine;

public class InteractableBed : AbstractInteractable
{

    [SerializeField] private string notAllTasksCompletedDialogueKey = "I can't go to bed yet.";

    private void OnEnable()
    {
        InkManager.Instance.onDialogueEnd += HandleDialogueEnd;
    }
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

    public void HandleDialogueEnd(TextAsset ts)
    {
        if (ts == task.inkJson && TaskManager.Instance.AreAllTasksCompleted())
        {
            Debug.Log("Dialogue ended for task: " + task.TaskName);
            int nextDay = GameFlowManager.Instance.CurrentDay + 1;
            if (nextDay > GameFlowManager.Instance.GameScenes.Length)
            {
                Debug.Log("No more scenes to load. Game might be completed.");
                Debug.Log("Reloading current scene or showing end game screen...");
                GameFlowManager.Instance.LoadScene(GameFlowManager.Instance.CurrentDay); // Reload current scene or implement end game logic
                return;
            }
            GameFlowManager.Instance.LoadScene(nextDay);
        }
    }
}
