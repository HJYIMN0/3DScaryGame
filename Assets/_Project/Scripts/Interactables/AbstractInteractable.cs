using System;
using UnityEngine;
public abstract class AbstractInteractable : MonoBehaviour
{
    [SerializeField] protected TaskSO task;
    [SerializeField] protected GameObject canvaPrefab;

    private bool isCanvaInstantiated = false;
    private GameObject canvaInstance;
    public TaskSO TaskSO => task;

    public bool HasBeenInteractedWith { get; protected set; } = false;
    private PlayerInteractionController _playerInteractionController;

    protected virtual void Start()
    {
        if (task != null && !task.isTaskSecret) 
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

    public void EvaluateCanvaStatus(PlayerInteractionController player, GameObject canvaObj)
    {
        Debug.Log("Player is here!");
        if (isCanvaInstantiated) return;

        player.SetInteractableTaskForPlayer(this);

        if (HasBeenInteractedWith) return;
        if (!isCanvaInstantiated && canvaInstance != null)
        {
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva wasn't null, but not instantiated. Instantiating now...");
        }
        else if (!isCanvaInstantiated && canvaInstance == null)
        {
            canvaInstance = Instantiate(canvaObj, Vector3.zero, Quaternion.identity);
            canvaInstance.transform.SetParent(transform);
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva was null, instantiating now...");
        }
    }
    public abstract void InteractWithTask();

    /// <summary>
    /// Remember, you need to set the TaskSo to bool IsInkTask = true if you need to show Dialogue
    /// </summary>
    /// <param name="dialogue"></param>
    /// <param name="usesVariables"></param>
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

    public void PLayTaskSfx()
    {
        AudioSource audioSource = this.GetComponent<AudioSource>();
        if (task.TaskSfx != null && audioSource != null)
        {
            AudioManager.Instance.PlaySfxSoundFromSource(audioSource, task.TaskSfx);
            Debug.Log($"Playing SFX for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No SFX assigned for task '{task.TaskName}'. or No AudioSource Component");
            
        }
    }

    public virtual void MarkTaskAsComplete()
    {
        TaskManager.Instance.CompleteTask(task.TaskName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            EvaluateCanvaStatus(other.gameObject.GetComponent<PlayerInteractionController>(), canvaPrefab);

            if (_playerInteractionController == null)
                _playerInteractionController = other.gameObject.GetComponent<PlayerInteractionController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            DeactivateCanvas();
            if (_playerInteractionController != null && _playerInteractionController.interactableTask == this)
            {
                _playerInteractionController.ClearInteractableTaskForPlayer();
                InkManager.Instance.ClearStoryAndTextAsset();
                Debug.Log("Player left, clearing interactable task for player.");
            }
        }
    }
}
