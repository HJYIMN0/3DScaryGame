using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    public AbstractInteractable interactableTask { get; private set; }
    public InputSystem_Actions actions { get; private set; }

    private PlayerInputController _input;
    private PlayerDialogueController _dialogueController;

    private void Awake()
    {
        _input = GetComponent<PlayerInputController>();
    }

    private void Start()
    {
        _input.OnInteractAction += HandleInteraction;
    }

    private void Update()
    {
        if (interactableTask != null)
        {
            Debug.Log("Player is near interactable task: " + interactableTask.name);
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

    private void HandleInteraction()
    {
        if (_dialogueController == null)
        {
            _dialogueController = GetComponent<PlayerDialogueController>();
        }

        if (_dialogueController.IsDialogueActive)
        {
         return;   
        }

        if (interactableTask != null)
        {
            Debug.Log("Player interacted with task: " + interactableTask.name);
            interactableTask.InteractWithTask();
        }
    }
}
