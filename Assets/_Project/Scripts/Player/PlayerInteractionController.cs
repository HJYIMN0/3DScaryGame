using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private bool isThisLevelPhoneLevel = false;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float interactionRadius = 5f;

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
        // MODIFICATO: il return era stato commentato e sostituito con un semplice
        // Debug.Log ("Ecco qui!"), quindi questo controllo non aveva più alcun effetto:
        // il blocco raycast girava sempre, anche con dialogo/minigioco attivi.
        // Ripristinato l'if che avvolge il blocco raycast, così mentre il player è
        // impegnato in un dialogo o in un minigioco non viene ri-assegnato/ripulito
        // l'interactable sotto al naso dell'interazione in corso.
        bool isBusyWithDialogueOrMinigame = (_dialogueController != null && _dialogueController.IsDialogueActive)
            || (activeMinigame != null && activeMinigame.IsMiniGameActive);

        if (!isBusyWithDialogueOrMinigame)
        {
            bool didHit = Physics.SphereCast(playerCamera.transform.position,
                interactionRadius, playerCamera.transform.forward, out RaycastHit hit, interactionDistance);
            bool isLookingAtInteractable = didHit && hit.collider.CompareTag("Interactable");

            if (isLookingAtInteractable)
            {
                AbstractInteractable interactable = hit.collider.gameObject.GetComponent<AbstractInteractable>();
                if (interactable != null)
                {
                    interactable.OnPlayerEnter(this);

                    AbstractMinigame minigame = hit.collider.gameObject.GetComponent<AbstractMinigame>();
                    if (minigame != null)
                    {
                        SetActiveMiniGameForPlayer(minigame);
                    }
                }
            }
            else
            {
                if (interactableTask != null)
                {
                    interactableTask.OnPlayerExit(this);
                }
                if (activeMinigame != null)
                {
                    ClearActiveMiniGameForPlayer();
                }
            }
        }

        // AGGIUNTO: questo blocco mancava del tutto nel file incollato — era l'unico punto
        // che chiamava HandleDialogueInteraction()/HandleMiniGameInteraction(). Senza di
        // esso premere Interact non faceva letteralmente nulla: è la causa reale di entrambi
        // i problemi segnalati (interazione che non parte, canva che non si disattiva mai
        // perché il minigioco/dialogo non veniva mai avviato).
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