using System;
using UnityEngine;
public abstract class AbstractInteractable : MonoBehaviour
{
    [SerializeField] protected TaskSO task;
    [SerializeField] protected GameObject canvaPrefab;
    [SerializeField] protected AbstractMinigame MiniGame;

    private bool isCanvaInstantiated = false;
    private GameObject canvaInstance;
    public TaskSO TaskSO => task;
    protected TaskManager taskManager;

    public bool HasBeenCompleted { get; protected set; } = false;
    public void SetHasBeenCompleted(bool value) => HasBeenCompleted = value;

    protected PlayerInteractionController _playerInteractionController;
    protected InkManager _inkManager;
    public InkManager GetInkManager() 
    {
        if (_inkManager == null)
        {
            Debug.LogWarning("You tried to access to PlayerInkManager but it is null");
            return null;
        }
        return _inkManager;
    }

    protected virtual void Start()
    {
        taskManager = TaskManager.Instance;

        if (task != null && !task.isTaskSecret)
        {
            taskManager.AddTask(task);
            Debug.Log($"Added task '{task.TaskName}' to DayManager.");
        }
        else
        {
            Debug.LogWarning("No task assigned to this InteractableTask.");
        }
    }

    private void Update()
    {
        if (canvaInstance != null && canvaInstance.activeSelf)
        {
            if (MiniGame != null && MiniGame.IsMiniGameActive || HasBeenCompleted)
            {
                DeactivateCanvas();
            }
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

    // MODIFICATO: rimosso il parametro "canvaObj". Era ridondante: l'unico punto da cui
    // veniva chiamato questo metodo (il vecchio OnTriggerEnter) passava sempre "canvaPrefab",
    // che è già un campo di questa classe. Se da qualche altra parte nel progetto chiami
    // EvaluateCanvaStatus passando un prefab diverso, segnalamelo: con questa modifica
    // quel comportamento andrebbe perso.
    public void EvaluateCanvaStatus(PlayerInteractionController player)
    {
        Debug.Log("Player is here!");
        if (isCanvaInstantiated) return;
        if (HasBeenCompleted) return;

        player.SetInteractableTaskForPlayer(this);

        //if (HasBeenInteractedWith) return;
        if (!isCanvaInstantiated && canvaInstance != null)
        {
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva wasn't null, but not instantiated. Instantiating now...");
        }
        else if (!isCanvaInstantiated && canvaInstance == null)
        {
            canvaInstance = Instantiate(canvaPrefab, Vector3.zero, Quaternion.identity);
            canvaInstance.transform.SetParent(transform);
            canvaInstance.GetComponent<InteractionCanvaManager>().Initialize(this);
            canvaInstance.SetActive(true);
            isCanvaInstantiated = true;
            Debug.Log("Canva was null, instantiating now...");
        }
    }

    public void InteractWithTask()
    {
        if (taskManager.IsPhoneInScene && !taskManager.HasAnsweredThePhone && !task.isThisPhoneTask)
        {
            Debug.Log("Player hasn't completed the phone task yet.");
            ShowDialogue(task.answerThePhoneText);
            return;
        }
        ExecuteInteraction();
        PLayTaskSfx();
    }

    public abstract void ExecuteInteraction();

    /// <summary>
    /// Remember, you need to set the TaskSo to bool IsInkTask = true if you need to show Dialogue
    /// </summary>
    /// <param name="dialogue"></param>
    /// <param name="usesVariables"></param>
    public virtual void ShowDialogue(TextAsset dialogue, bool usesVariables)
    {
        if (!task.IsInkTask) return;
        if (dialogue != null)
        {
            // MODIFICATO: da InkManager.Instance a _inkManager
            _inkManager?.StartDialogue(dialogue, usesVariables);
            Debug.Log($"Showing dialogue for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No dialogue assigned for task '{task.TaskName}'.");
        }
    }

    public virtual void ShowDialogue(TextAsset dialogue, bool usesVariables, int differentDay)
    {
        if (!task.IsInkTask) return;
        if (dialogue != null)
        {
            // MODIFICATO: da InkManager.Instance a _inkManager
            _inkManager?.StartDialogue(dialogue, usesVariables, differentDay);
            Debug.Log($"Showing dialogue for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No dialogue assigned for task '{task.TaskName}'.");
        }
    }

    public virtual void ShowDialogue(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        // MODIFICATO: da InkManager.Instance a _inkManager
        _inkManager?.StartDialogue(text);
        Debug.Log($"Showing dialogue: '{text}'.");
    }

    public void PLayTaskSfx()
    {
        AudioSource audioSource = this.GetComponent<AudioSource>();
        if (task.TaskSfx != null && audioSource != null)
        {
            AudioManager.Instance.PlaySfxFromPointAndDestroy(audioSource, task.TaskSfx);
            Debug.Log($"Playing SFX for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No SFX assigned for task '{task.TaskName}'. or No AudioSource Component");

        }
    }

    public virtual void MarkTaskAsComplete()
    {
        taskManager.CompleteTask(task);
        HasBeenCompleted = true;
    }
    public void StartMiniGame()
    {
        if (MiniGame != null)
        {
            if (_playerInteractionController != null)
                MiniGame.SetPlayerInputController(_playerInteractionController.GetComponent<PlayerInputController>());

            MiniGame.StartMiniGame();
            Debug.Log($"Starting mini-game for task '{task.TaskName}'.");
        }
        else
        {
            Debug.LogWarning($"No mini-game assigned for task '{task.TaskName}'.");
        }
    }

    public virtual void OnDialogueEnd(TextAsset dialogue)
    {
        // Questo metodo può essere sovrascritto dalle classi figlie per gestire la fine del dialogo specifico del task
        Debug.Log($"Dialogue ended for task '{task.TaskName}'.");
    }

    // MODIFICATO: OnTriggerEnter è stato rimosso da qui. Il rilevamento del trigger ora
    // avviene solo in PlayerInteractionController (come richiesto), che chiama questo
    // metodo pubblico passando se stesso. La logica interna è identica a prima: cambia solo
    // chi la innesca (prima questa classe faceva GetComponent sul Player per procurarsi
    // il riferimento, ora lo riceve già pronto).
    public void OnPlayerEnter(PlayerInteractionController player)
    {
        EvaluateCanvaStatus(player);

        if (_playerInteractionController == null)
            _playerInteractionController = player;

        // AGGIUNTO: recupera InkManager dal player (ora è un componente su di esso)
        if (_inkManager == null)
            _inkManager = player.GetComponent<InkManager>();

        _inkManager.onDialogueEnd += OnDialogueEnd; // Sottoscrivi all'evento onDialogueEnd dell'InkManager
    }

    // MODIFICATO: OnTriggerExit rimosso da qui per lo stesso motivo di OnPlayerEnter sopra.
    public void OnPlayerExit(PlayerInteractionController player)
    {
        DeactivateCanvas();
        if (_playerInteractionController != null && _playerInteractionController.interactableTask == this)
        {
            _playerInteractionController.ClearInteractableTaskForPlayer();
            // MODIFICATO: da InkManager.Instance a _inkManager
            _inkManager?.ClearStoryAndTextAsset();
            Debug.Log("Player left, clearing interactable task for player.");
        }
        if (_inkManager != null)
        {
            _inkManager.onDialogueEnd -= OnDialogueEnd; // Annulla la sottoscrizione all'evento onDialogueEnd
            Debug.Log("Player left, unsubscribing from onDialogueEnd event.");
        }
    }
}