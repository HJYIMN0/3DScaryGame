using Unity.VisualScripting;
using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private bool isThisLevelPhoneLevel = false;

    public AbstractInteractable interactableTask { get; private set; }
    public InputSystem_Actions Actions { get; private set; }

    public PlayerInputController Input { get; private set; }
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
        //N.B Per ora questa roba non fa niente.
        //Dovremmo fare un refactor di come funzionano TaskManager e AbstractTask per correggere
        //E forse conviene tenerlo qui. 
        //Per ora funziona e perciò non lo tocco.
        //In futuro possiamo valutare di spostare questa roba in un altro script, magari un PhoneManager
        if (isThisLevelPhoneLevel)
        {
            Debug.Log("This level is a phone level. Initializing phone-related properties.");
            HasAnsweredPhone = false;
        }
        else
        {
            Debug.Log("This level is not a phone level. Phone-related properties will not be initialized.");
        }
        Input = GetComponent<PlayerInputController>();
    }

    private void Start()
    {
        Input.OnInteractAction += HandleInteraction;
    }

    //private void Update()
    //{
    //    if (interactableTask != null)
    //    {
    //        Debug.Log("Player is near interactable task: " + interactableTask.name);
    //    }
    //}

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
