using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private bool isThisLevelPhoneLevel = false;

    public AbstractInteractable interactableTask { get; private set; }

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
            HandleInteraction();
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

    private void HandleInteraction()
    {
        if (_dialogueController != null && _dialogueController.IsDialogueActive)
            return;

        if (interactableTask != null)
        {
            Debug.Log("Player interacted with task: " + interactableTask.name);
            interactableTask.InteractWithTask();
        }
    }
}