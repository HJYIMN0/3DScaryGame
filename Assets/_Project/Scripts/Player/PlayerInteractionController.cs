using UnityEngine;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 3f;

    // [MODIFICA] Default 5f -> 0.5f. Un raggio di 5m per lo SphereCast è enorme: inoltre Unity NON rileva
    // i collider già sovrapposti alla sfera di partenza, quindi con raggio 5 qualsiasi interactable
    // entro 5m dalla camera veniva ignorato (era questa la causa della "distanza troppo specifica").
    // ATTENZIONE: il default cambia solo per i nuovi componenti, nell'Inspector va aggiornato a mano il valore serializzato.
    [SerializeField] private float interactionRadius = 0.5f;

    [SerializeField] private LayerMask interactableLayerMask;

    // [NUOVO] Sfera di prossimità attorno al transform del player: copre la zona "morta" in cui lo SphereCast
    // parte già dentro al collider e non lo vedrebbe. Serve per interagire anche da molto vicino.
    [Header("Proximity (sfera attorno al player)")]
    [SerializeField] private float proximityRadius = 2f;
    // [NUOVO] Offset del centro della sfera (utile se il pivot del player è ai piedi: es. (0, 1, 0)).
    [SerializeField] private Vector3 proximityCenterOffset = Vector3.zero;
    // [NUOVO] Semi-angolo massimo (rispetto allo sguardo) entro cui un oggetto in prossimità è interagibile.
    // 180 = anche dietro di te, 90 = solo davanti a te.
    [SerializeField, Range(0f, 180f)] private float proximityMaxAngle = 90f;

    public AbstractInteractable interactableTask { get; private set; }
    public AbstractMinigame activeMinigame { get; private set; }

    private PlayerInputController _input;
    private PlayerDialogueController _dialogueController;
    private InkManagerUI _inkManagerUI;

    // [NUOVO] Buffer riutilizzabili: Update gira ogni frame, così evitiamo allocazioni (GC) ad ogni query.
    private const int MaxQueryResults = 32;
    private readonly Collider[] _overlapBuffer = new Collider[MaxQueryResults];
    private readonly RaycastHit[] _castBuffer = new RaycastHit[MaxQueryResults];

    // [NUOVO] Interactable su cui abbiamo chiamato OnPlayerEnter per ultimo (quello "agganciato" dal player).
    // Serve a sapere SEMPRE a chi chiamare OnPlayerExit quando il candidato cambia (A -> B) o sparisce.
    // Non usiamo interactableTask perché EvaluateCanvaStatus lo imposta solo in certi casi
    // (es. non lo imposta se il canva è già istanziato o se il task è completato).
    private AbstractInteractable _hoveredInteractable;

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

        // Durante dialoghi e minigiochi non cerchiamo nuovi interactable.
        if (isBusyWithDialogueOrMinigame)
            return;

        // [MODIFICA] Prima: un singolo Physics.SphereCast che restituiva solo il PRIMO hit (se era un obstacle,
        // l'interactable dietro/accanto veniva perso). Ora TryFindBestInteractable controlla TUTTI i collider
        // colpiti sia dalla sfera di prossimità sia dallo SphereCast e restituisce il migliore.
        // Il tag e la presenza di AbstractInteractable sono già verificati dentro l'helper.
        bool isLookingAtInteractable = TryFindBestInteractable(out Collider bestCollider);

        AbstractInteractable interactable = null;
        AbstractMinigame minigame = null;

        if (isLookingAtInteractable)
        {
            // [MODIFICA] hit.collider -> bestCollider (resto della logica invariato)
            interactable = bestCollider.GetComponent<AbstractInteractable>();

            if (interactable != null)
            {
                // [NUOVO] FIX BUG canva bloccato: se il candidato è cambiato (A -> B senza passare da
                // "nessun interactable"), prima chiudiamo esplicitamente A. Prima OnPlayerExit(A) non veniva
                // mai chiamato: il canva di A restava acceso e poi, uscendo dal range, veniva "ripulito"
                // solo B (interactableTask), lasciando il canva di A orfano a schermo.
                if (_hoveredInteractable != null && _hoveredInteractable != interactable)
                    _hoveredInteractable.OnPlayerExit(this);

                _hoveredInteractable = interactable;

                // Mostra il popup e imposta interactableTask.
                interactable.OnPlayerEnter(this);

                // Per oggetti come la DrilableWall.
                // [MODIFICA] hit.collider -> bestCollider
                minigame = bestCollider.GetComponent<AbstractMinigame>();

                if (minigame != null)
                    SetActiveMiniGameForPlayer(minigame);
                else if (activeMinigame != null)
                    ClearActiveMiniGameForPlayer();
            }
        }

        // Non stiamo guardando un vero interactable: pulizia UI e riferimenti.
        if (interactable == null)
        {
            // [NUOVO] Chiudiamo l'interactable agganciato (potrebbe essere diverso da interactableTask).
            if (_hoveredInteractable != null)
            {
                _hoveredInteractable.OnPlayerExit(this);
                _hoveredInteractable = null;
            }

            // Codice originale: resta come rete di sicurezza. Se è lo stesso oggetto di sopra,
            // OnPlayerExit ha già azzerato interactableTask e questo ramo non viene eseguito.
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

        // Gestione della pressione di E.
        if (_input != null &&
            _input.InputActions.Player.Interact.WasPressedThisFrame())
        {
            if (activeMinigame != null && !activeMinigame.IsMiniGameActive)
            {
                // DrilableWall e altri minigiochi.
                HandleMiniGameInteraction();
            }
            else if (interactableTask != null)
            {
                // Telefono e interactable basati su dialogo.
                interactableTask.InteractWithTask();
            }
        }
    }

    // [NUOVO] Doppio rilevamento:
    //  1) SphereCast dalla camera a interactionDistance: vede tutto ciò che si trova lungo lo sguardo.
    //  2) Sfera di prossimità attorno al player (Physics.OverlapSphere): vede tutto ciò che è vicino,
    //     anche se lo SphereCast parte già dentro al collider.
    // In entrambi i casi si scorrono TUTTI i collider (non solo il primo hit), quindi un obstacle
    // colpito prima non nasconde più l'interactable.
    // [MODIFICA] Priorità: se lo SphereCast direzionale trova qualcosa, vince quello (il più "al centro dello sguardo");
    // la sfera di prossimità è solo un fallback quando non stiamo guardando nessun interactable.
    // Metodo senza side-effect: usato sia da Update sia da OnDrawGizmos.
    private bool TryFindBestInteractable(out Collider bestCollider)
    {
        bestCollider = null;
        float bestAngle = float.MaxValue;

        Transform cam = playerCamera.transform;
        Vector3 camPos = cam.position;
        Vector3 camForward = cam.forward;

        // --- 1) SphereCast dalla camera (priorità: è ciò che il player sta guardando) ---
        // [MODIFICA] Spostato prima della sfera di prossimità.
        int hitCount = Physics.SphereCastNonAlloc(
            camPos,
            interactionRadius,
            camForward,
            _castBuffer,
            interactionDistance,
            interactableLayerMask
        );

        // for invece di foreach: con le versioni NonAlloc il numero di risultati validi è hitCount, non la lunghezza del buffer.
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _castBuffer[i];
            Collider col = hit.collider;
            if (!IsValidInteractable(col)) continue;

            // Se hit.distance == 0 la sfera partiva già dentro al collider e hit.point non è affidabile.
            Vector3 point = hit.distance > 0f ? hit.point : col.bounds.ClosestPoint(camPos);
            float angle = GetAngleToPoint(camPos, camForward, point);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestCollider = col;
            }
        }

        // [NUOVO] Priorità allo sguardo: se il cast direzionale ha trovato un interactable, vince lui
        // e la sfera di prossimità non viene nemmeno considerata (niente doppi candidati contemporanei).
        if (bestCollider != null)
            return true;

        // --- 2) Sfera di prossimità (fallback) ---
        // [MODIFICA] Ora viene valutata solo se lo SphereCast direzionale non ha trovato nulla.
        int overlapCount = Physics.OverlapSphereNonAlloc(
            transform.position + proximityCenterOffset,
            proximityRadius,
            _overlapBuffer,
            interactableLayerMask
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = _overlapBuffer[i];
            if (!IsValidInteractable(col)) continue;

            float angle = GetAngleToPoint(camPos, camForward, col.bounds.ClosestPoint(camPos));

            // In prossimità accettiamo solo ciò che è (grossomodo) davanti a noi.
            if (angle > proximityMaxAngle) continue;

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestCollider = col;
            }
        }

        return bestCollider != null;
    }

    // [NUOVO] Stesso criterio dell'originale (tag "Interactable" + componente AbstractInteractable sul collider).
    private static bool IsValidInteractable(Collider col)
    {
        return col != null
            && col.CompareTag("Interactable")
            && col.TryGetComponent<AbstractInteractable>(out _);
    }

    // [NUOVO] Angolo (in gradi) tra lo sguardo e la direzione verso un punto.
    private static float GetAngleToPoint(Vector3 origin, Vector3 forward, Vector3 point)
    {
        Vector3 dir = point - origin;

        // Punto coincidente con la camera (siamo dentro al collider): lo consideriamo perfettamente a fuoco.
        if (dir.sqrMagnitude < 0.0001f)
            return 0f;

        return Vector3.Angle(forward, dir);
    }

    public void SetInteractableTaskForPlayer(AbstractInteractable taskToInteractWith)
    {
        Debug.Log("Setting interactable task for player: " + taskToInteractWith.name);
        interactableTask = taskToInteractWith;

        if (_dialogueController != null)
            _dialogueController.onDialogueEnd -= OnCurrentTaskDialogueEnded;

        interactableTask = taskToInteractWith;

        // 🔽 Poi ci iscriviamo al nuovo task
        if (interactableTask != null && _dialogueController != null)
            _dialogueController.onDialogueEnd += OnCurrentTaskDialogueEnded;
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

        // [MODIFICA] Prima: Physics.SphereCast "a parte" (senza LayerMask) solo per il gizmo.
        // Ora usiamo lo stesso helper dell'Update, così il gizmo mostra ESATTAMENTE ciò che il gioco rileva.
        // Conseguenza: il colore giallo ("colpito qualcos'altro") non ha più senso e non c'è più, restano verde/rosso.
        bool hitInteractable = TryFindBestInteractable(out Collider best);

        // Verde se trova un Interactable, rosso se non trova nulla
        Gizmos.color = hitInteractable ? Color.green : Color.red;

        Vector3 endPoint = origin + direction * interactionDistance;

        // Linea centrale del cast
        Gizmos.DrawLine(origin, endPoint);

        // Sfere ai due estremi per visualizzare il raggio della SphereCast
        Gizmos.DrawWireSphere(origin, interactionRadius);
        Gizmos.DrawWireSphere(endPoint, interactionRadius);

        // [NUOVO] Sfera di prossimità attorno al player
        Gizmos.color = hitInteractable ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + proximityCenterOffset, proximityRadius);

        // [MODIFICA] Prima evidenziava hit.point; ora evidenzia il punto più vicino dell'interactable scelto.
        if (hitInteractable)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(best.bounds.ClosestPoint(origin), 0.1f);
        }
    }

    private void OnCurrentTaskDialogueEnded()
    {
        // Il dialogo è finito: ora deve partire il "prossimo piatto"!
        if (interactableTask != null)
        {
            // Opzione 1: Se AbstractInteractable ha un metodo dedicato (es. OnDialogueComplete)
            // interactableTask.OnDialogueComplete(); 

            // Opzione 2: Se vuoi che parta la prossima interazione automaticamente
            // (es. se il task è una catena di dialoghi, lo richiami)
            interactableTask.InteractWithTask();

            // Opzione 3: Se il "prossimo piatto" è gestito da un TaskSO specifico,
            // puoi fare un cast e chiamare un metodo personalizzato.
            // Esempio: if (interactableTask is PhoneTask phone) phone.NextStep();
        }
    }
}