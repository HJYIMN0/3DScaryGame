using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private bool isThisLevelPhoneLevel = false;

    public AbstractInteractable interactableTask { get; private set; }
    public AbstractMinigame activeMinigame { get; private set; }

    private PlayerInputController _input;
    private PlayerDialogueController _dialogueController;

    public bool HasAnsweredPhone { get; private set; }

    public void SetHasAnsweredPhone(TaskSO phoneTask, bool hasAnswered)
    {
        if (phoneTask != null && phoneTask.isThisPhoneTask)
        {
            HasAnsweredPhone = hasAnswered;
            Debug.Log("Player has answered the phone task: " + phoneTask.TaskName);
        }
    }

    private void Awake()
    {
        _input = GetComponent<PlayerInputController>();
        _dialogueController = GetComponent<PlayerDialogueController>();

        if (isThisLevelPhoneLevel)
            Debug.Log("This level is a phone level.");
        else
            Debug.Log("This level is not a phone level.");
    }

    private void Update()
    {
        // Polling diretto — un solo frame di input per pressione
        if (_input.InputActions.Player.Interact.WasPressedThisFrame())
        {
            if (interactableTask != null)
            {
                HandleDialogueInteraction();
            }
            else if (activeMinigame != null)
            {
                HandleMiniGameInteraction();
            }
        }
    }

    // AGGIUNTO: rilevamento centralizzato del trigger. Prima questa logica era duplicata
    // separatamente in AbstractInteractable.OnTriggerEnter e AbstractMinigame.OnTriggerEnter
    // (ognuno controllava il tag "Player" per conto suo). Ora è solo questa classe a rilevare
    // l'overlap (essendo sul Player non serve nemmeno controllare il tag) e a smistarlo verso
    // i metodi pubblici già esistenti sulle due classi.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Interactable")) return;

        Debug.Log("I found an interactable!");
        AbstractInteractable interactable = other.gameObject.GetComponent<AbstractInteractable>();
        if (interactable != null)
        {
            interactable.OnPlayerEnter(this);
        }

        AbstractMinigame minigame = other.gameObject.GetComponent<AbstractMinigame>();
        if (minigame != null)
        {
            SetActiveMiniGameForPlayer(minigame);
        }
    }

    // AGGIUNTO: speculare a OnTriggerEnter sopra, stesso motivo.
    private void OnTriggerExit(Collider other)
    {
        AbstractInteractable interactable = other.gameObject.GetComponent<AbstractInteractable>();
        if (interactable != null)
        {
            interactable.OnPlayerExit(this);
        }

        AbstractMinigame minigame = other.gameObject.GetComponent<AbstractMinigame>();
        if (minigame != null)
        {
            ClearActiveMiniGameForPlayer();
        }
    }

    public void SetInteractableTaskForPlayer(AbstractInteractable taskToInteractWith)
    {
        Debug.Log("Setting interactable task for player: " + taskToInteractWith.name);
        interactableTask = taskToInteractWith;
    }

    public void ClearInteractableTaskForPlayer()
    {
        Debug.Log("Clearing interactable task for player.");
        interactableTask = null;
    }

    private void HandleDialogueInteraction()
    {
        if (_dialogueController != null && _dialogueController.IsDialogueActive)
            return;

        if (interactableTask != null)
        {
            Debug.Log("Player interacted with task: " + interactableTask.name);
            interactableTask.InteractWithTask();
        }
    }

    public void SetActiveMiniGameForPlayer(AbstractMinigame minigame)
    {
        if (minigame != null && minigame != activeMinigame)
        {
            activeMinigame = minigame;
            Debug.Log($"Setting active Minigame for player as {minigame}");
        }
    }

    public void ClearActiveMiniGameForPlayer()
    {
        if (activeMinigame == null) return;

        activeMinigame = null;
        Debug.Log("active minigame is now null");
    }

    private void HandleMiniGameInteraction()
    {
        if (activeMinigame != null)
        {
            Debug.Log("Starting minigame " + activeMinigame.name);
            activeMinigame.StartMiniGame();
        }
    }
}