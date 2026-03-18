using System;
using UnityEngine;
public abstract class AbstractInteractable : MonoBehaviour
{
    [SerializeField] protected TaskSO task;

    private bool isCanvaInstantiated = false;
    private GameObject canvaInstance;
    public TaskSO TaskSO => task;

    public bool HasBeenInteractedWith { get; protected set; } = false;

    protected virtual void Start()
    {
        if (task != null) 
        {
            TaskManager.Instance.AddTask(task);
            Debug.Log($"Added task '{task.TaskName}' to DayManager.");
        }
        else
        {
            Debug.LogWarning("No task assigned to this InteractableTask.");
        }
    }

    public void DeactivateCanvas()
    {
        if (isCanvaInstantiated && canvaInstance != null)
        {
            canvaInstance.SetActive(false);
            isCanvaInstantiated = false;
            Debug.Log("Player left, deactivating canva.");
        }
    }

    public void EvaluateCanvaStatus(PlayerInteractionController player, Vector3 pos, Quaternion rot, GameObject canvaObj)
    {
        Debug.Log("Player is here!");
        if (isCanvaInstantiated) return;

        player.SetInteractableTaskForPlayer(this);

        if (HasBeenInteractedWith) return;
        if (!isCanvaInstantiated && canvaInstance != null)
        {
            canvaInstance.transform.position = pos;
            canvaInstance.transform.rotation = rot;
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva wasn't null, but not instantiated. Instantiating now...");
        }
        else if (!isCanvaInstantiated && canvaInstance == null)
        {
            canvaInstance = Instantiate(canvaObj, pos, rot);
            canvaInstance.transform.SetParent(transform);
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva was null, instantiating now...");
        }
    }
    public abstract void InteractWithTask();

    public virtual void ShowDialogue(TextAsset dialogue, bool usesVariables)
    {
        if (!task.isInkTask) return;
        if (dialogue != null)
        {
            InkManager.Instance.StartDialogue(dialogue, usesVariables);
            Debug.Log($"Showing dialogue for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No dialogue assigned for task '{task.TaskName}'.");
        }
    }
}
