using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// this class is destroyed on load. So you will need to create a new one for each scene.
/// Each Scenes will have a different set of tasks, so this class is responsible for keeping track of the tasks of the day.
public class TaskManager : GenericSingleton<TaskManager>
{
    [SerializeField] private List<TaskSO> tasksOfTheDay;
    public List<TaskSO> CompletedTasks { get; private set; } = new List<TaskSO>();

    public Action<TaskSO> OnTaskComplete;
    public Action<TaskSO> OnTaskAdded;

    public List<TaskSO> GetTasksOfTheDay() => tasksOfTheDay;

    public bool IsPhoneInScene {  get; private set; }
    public bool HasAnsweredThePhone { get; private set; }

    public void SetPhoneAnswered(bool value)
    {
        HasAnsweredThePhone = value;
    }

    private void Start()
    {
        //SceneManager.sceneLoaded += (scene, mode) => CompletedTasks.Clear(); // Clear completed tasks when a new scene is loaded
        CompletedTasks.Clear();
    }

    public void AddTask(TaskSO task)
    {
        if (!tasksOfTheDay.Contains(task))
        {
            tasksOfTheDay.Add(task);
            Debug.Log($"Task '{task.TaskName}' added to today's tasks.");
            OnTaskAdded?.Invoke(task);
        }
        else
        {
            Debug.LogWarning($"Task '{task.TaskName}' is already in today's tasks.");
        }
    }

    public void ClearTask(TaskSO task)
    {
        if (tasksOfTheDay.Contains(task))
        {
            tasksOfTheDay.Remove(task);
            Debug.Log($"Task '{task.TaskName}' removed from today's tasks.");
        }
        else
        {
            Debug.LogWarning($"Task '{task.TaskName}' not found in today's tasks.");
        }
    }
    public void CompleteTask(string TaskId)
    {
        TaskSO task = tasksOfTheDay.Find(t =>
            t.TaskName.Equals(TaskId, System.StringComparison.OrdinalIgnoreCase));
        if (task != null && !CompletedTasks.Contains(task))
        {
            CompletedTasks.Add(task);
            Debug.Log($"Task '{task.TaskName}' completed.");

            OnTaskComplete?.Invoke(task);
        }
        else if (task == null)
        {
            Debug.LogWarning($"Task with ID '{TaskId}' not found in today's tasks.");
        }
        else
        {
            Debug.LogWarning($"Task '{task.TaskName}' has already been completed.");
        }
    }

    public bool AreAllTasksCompleted()
    {
        List<TaskSO> mandatoryTasks = tasksOfTheDay.FindAll(t => t.isMandatory);
        foreach (TaskSO task in mandatoryTasks)
        {
            if (!CompletedTasks.Contains(task))
            {
                return false; // Found a mandatory task that hasn't been completed
            }
        }
        return true; // All mandatory tasks are completed
    }

    public void MarkAllTasksAsComplete()
    {
        foreach (TaskSO task in tasksOfTheDay)
        {
            if (!CompletedTasks.Contains(task))
            {
                CompletedTasks.Add(task);
                Debug.Log($"Task '{task.TaskName}' marked as completed.");
                OnTaskComplete?.Invoke(task);
            }
        }
    }

    public override bool IsDestroyedOnLoad() => true;
    public override bool ShouldDetatchFromParent() => true;
}
