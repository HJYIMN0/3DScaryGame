using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float interactionRadius = 5f;
    [SerializeField] private LayerMask interactableLayerMask;

    public AbstractInteractable interactableTask { get; private set; }
    public AbstractMinigame activeMinigame { get; private set; }

    private PlayerInputController _input;
    private PlayerDialogueController _dialogueController;
    private InkManagerUI _inkManagerUI;

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
        _inkManagerUI = GetComponent<InkManagerUI>();
    }

    private void Update()
    {
        bool isBusyWithDialogueOrMinigame =
            (_dialogueController != null && _dialogueController.IsDialogueActive) ||
            (activeMinigame != null && activeMinigame.IsMiniGameActive);

        // Durante dialogo o minigioco non cerchiamo altri oggetti.
        if (isBusyWithDialogueOrMinigame)
            return;

        bool didHit = Physics.SphereCast(
            playerCamera.transform.position,
            interactionRadius,
            playerCamera.transform.forward,
            out RaycastHit hit,
            interactionDistance,
            interactableLayerMask
        );

        bool isLookingAtInteractable =
            didHit && hit.collider.CompareTag("Interactable");

        if (!isLookingAtInteractable)
        {
            if (interactableTask != null)
                interactableTask.OnPlayerExit(this);

            if (activeMinigame != null)
                ClearActiveMiniGameForPlayer();

            if (_inkManagerUI != null &&
                _inkManagerUI.IsDialogueOpen &&
                interactableTask == null &&
                activeMinigame == null)
            {
                _inkManagerUI.CloseCanva();
            }

            return;
        }

        AbstractInteractable interactable =
            hit.collider.GetComponent<AbstractInteractable>();

        if (interactable == null)
            return;

        // Fa comparire la UI e imposta interactableTask nel Player.
        interactable.OnPlayerEnter(this);

        // Salva il minigioco associato all'oggetto, se presente.
        AbstractMinigame minigame =
            hit.collider.GetComponent<AbstractMinigame>();

        if (minigame != null)
        {
            SetActiveMiniGameForPlayer(minigame);
        }
        else if (activeMinigame != null)
        {
            // Stai guardando un normale interactable, non più il minigioco precedente.
            ClearActiveMiniGameForPlayer();
        }

        // E avvia il minigioco dell'oggetto che stai guardando.
        if (_input != null &&
            _input.InputActions.Player.Interact.WasPressedThisFrame() &&
            activeMinigame != null &&
            !activeMinigame.IsMiniGameActive)
        {
            HandleMiniGameInteraction();
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

    public void SetActiveMiniGameForPlayer(AbstractMinigame minigame)
    {
        if (minigame != null && minigame != activeMinigame)
        {
            activeMinigame = minigame;
            activeMinigame.SetPlayerInputController(this.gameObject.GetComponent<PlayerInputController>());

            Debug.Log($"Setting active Minigame for player as {minigame}");
        }
    }

    public void ClearActiveMiniGameForPlayer()
    {
        if (activeMinigame == null) return;

        activeMinigame.SetPlayerInputController(null);
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

    private void OnDrawGizmos()
    {
        if (playerCamera == null) return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        bool didHit = Physics.SphereCast(origin, interactionRadius, direction, out RaycastHit hit, interactionDistance);
        bool hitInteractable = didHit && hit.collider.CompareTag("Interactable");

        // Verde se colpisce un Interactable, giallo se colpisce qualcos'altro, rosso se non colpisce nulla
        Gizmos.color = hitInteractable ? Color.green : (didHit ? Color.yellow : Color.red);

        Vector3 endPoint = origin + direction * interactionDistance;

        // Linea centrale del cast
        Gizmos.DrawLine(origin, endPoint);

        // Sfere ai due estremi per visualizzare il raggio della SphereCast
        Gizmos.DrawWireSphere(origin, interactionRadius);
        Gizmos.DrawWireSphere(endPoint, interactionRadius);

        // Se c'è un hit, evidenzia il punto di contatto effettivo
        if (didHit)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hit.point, 0.1f);
        }
    }
}