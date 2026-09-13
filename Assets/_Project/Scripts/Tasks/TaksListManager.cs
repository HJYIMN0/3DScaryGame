using System;
using TMPro;
using UnityEngine;

public class TaksListManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI taskListText;

    private void Start()
    {
        SetupTaskList();
    }

    private TaskManager _taskManager;

    private void OnEnable()
    {
        _taskManager = TaskManager.Instance;
        if (_taskManager == null) return;
        _taskManager.OnTaskComplete += HandleTaskComplete;
        _taskManager.OnTaskAdded += UpdateList;
    }

    private void OnDisable()
    {
        if (_taskManager == null) return;
        _taskManager.OnTaskComplete -= HandleTaskComplete;
        _taskManager.OnTaskAdded -= UpdateList;   // mancava anche questo!
        _taskManager = null;
    }

    private void UpdateList(TaskSO newTask)
    {
        Debug.Log($"New task '{newTask.TaskName}' added. Updating the task list UI.");
        SetupTaskList();
    }

    private void HandleTaskComplete(TaskSO completedTask)
    {
        Debug.Log($"Task '{completedTask.TaskName}' has been completed. Update the task list UI accordingly.");

        // MODIFICA: al completamento di una task, ricostruisce la lista
        // per applicare il tag <s> (strikethrough TMP) alla task appena completata
        RefreshTaskList();
    }

    public void SetupTaskList()
    {
        // MODIFICA: delegato a RefreshTaskList() per evitare duplicazione di logica
        RefreshTaskList();
    }

    // MODIFICA: nuovo metodo privato che ricostruisce il testo della lista da zero.
    // Segna con <s>...</s> (strikethrough TMP rich text) le task già completate.
    private void RefreshTaskList()
    {
        taskListText.text = string.Empty;

        TaskManager.Instance.GetTasksOfTheDay().ForEach(task =>
        {
            bool isCompleted = TaskManager.Instance.CompletedTasks.Contains(task);
            taskListText.text += isCompleted
                ? $"- <s>{task.TaskDescription}</s>\n"
                : $"- {task.TaskDescription}\n";
        });
    }
}