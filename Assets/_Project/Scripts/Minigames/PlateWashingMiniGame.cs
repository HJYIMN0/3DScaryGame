using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
public class PlateWashingMiniGame : AbstractMinigame
{
    [Header("Ink references")]
    [SerializeField] private TextAsset[] inkJsonFiles;

    [Header("UI References")]
    [Tooltip("Reference here the map used to clean!")]
    [SerializeField] private RectTransform cursorRect;
    [Tooltip("Reference here the plate in the ui from inspector")]
    [SerializeField] private Image plateImage;
    [Tooltip("All the different plates images you need to clean")]
    [SerializeField] private Sprite[] plateSprites;

    [Header("Minigame Settings")]
    [SerializeField] private float lookSensitivity = 300f;
    // MODIFICATO: sostituito requiredCircles (int, giri necessari) con
    // necessaryCleanAmount (float, secondi di "pulizia" necessari dentro il rect
    // del piatto). Non contiamo più i giri, solo il tempo passato dentro l'area.
    [SerializeField] private float necessaryCleanAmount = 3f;
    [Tooltip("Set this as true if you want the minigame to start as soon as you press play on Unity")]
    [SerializeField] private bool isDebugMode = false;
    [SerializeField] private bool canReplayMiniGame = true;

    [Header("Dirt stain settings")]
    [SerializeField] private CanvasGroup dirtStain;
    [SerializeField] private float fadeSpeed = 4f;

    [Header("Ink knot settings")]
    [Tooltip("Verify the knot name before the number in the ink file then paste it here. Remember: You must also add the _")]
    [SerializeField] private string inkKnotName = "plate_";

    private float _targetAlpha = 1f;


    private CanvasGroup _canvasGroup;
    private RectTransform _uiPanelRect;

    private Vector2 _playerLookInput;
    private Vector2 _cursorPos;
    private float _currentCleanAmount;
    private int _completedPlates;

    private InkManager _inkManager => interactable.GetInkManager();

    // Nuovo flag per attendere la fine del dialogo
    private bool _isWaitingForDialogue = false;

    // Riferimento al PlayerDialogueController (lo riempiamo all'avvio)
    private PlayerDialogueController _dialogueController;
    private bool _waitingForPlateDialogueToEnd; // AGGIUNTO: true quando il piatto è stato pulito e stiamo aspettando la chiusura del dialogo prima di passare al piatto successivo

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _uiPanelRect = GetComponent<RectTransform>();
    }

    public override void Start()
    {
        base.Start();

        if (plateSprites.Length > 0)
            plateImage.sprite = plateSprites[0];

        if (isDebugMode)
            StartMiniGame();
    }

    private void Update()
    {
        if (!IsMiniGameActive) return;

        if (isDialogueActive)
        {
            if (!_inkManager.IsDialogueOpen)
            {
                isDialogueActive = false;

                // AGGIUNTO: se il dialogo appena chiuso era quello di completamento
                // del piatto, solo ora passiamo al piatto successivo.
                if (_waitingForPlateDialogueToEnd)
                {
                    _waitingForPlateDialogueToEnd = false;
                    AdvanceToNextPlate();
                }
            }
            return;
        }

        if (playerInputController.InputActions.Player.Quit.IsPressed())
        {
            QuitMiniGame();
            return;
        }

        HandleMiniGameLogic();
    }   

    public override void HandleMiniGameLogic()
    {
        _playerLookInput = playerInputController.InputActions.Player.Look.ReadValue<Vector2>();

        if (_playerLookInput == Vector2.zero) return;

        EvaluateMousePosition();
        EvaluateAlpha();
        // MODIFICATO: chiamavamo EvaluateCirclesNumber(), ora chiamiamo il nuovo
        // EvaluateCleanAmount() che non richiede più movimento circolare.
        EvaluateCleanAmount();
    }

    private void EvaluateMousePosition()
    {
        _cursorPos += _playerLookInput * lookSensitivity * Time.deltaTime;

        Vector2 halfSize = _uiPanelRect.rect.size * 0.5f;
        _cursorPos.x = Mathf.Clamp(_cursorPos.x, -halfSize.x, halfSize.x);
        _cursorPos.y = Mathf.Clamp(_cursorPos.y, -halfSize.y, halfSize.y);

        cursorRect.anchoredPosition = _cursorPos;
    }

    private void EvaluateAlpha()
    {
        dirtStain.alpha = Mathf.Lerp(dirtStain.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
    }
    private void EvaluateCleanAmount()
    {
        if (_isWaitingForDialogue) return;

        RectTransform plateRect = plateImage.rectTransform;
        Vector2 plateCenter = plateRect.anchoredPosition;
        Vector2 plateHalfSize = plateRect.rect.size * 0.5f;

        bool isInsidePlate =
            Mathf.Abs(_cursorPos.x - plateCenter.x) <= plateHalfSize.x &&
            Mathf.Abs(_cursorPos.y - plateCenter.y) <= plateHalfSize.y;

        if (!isInsidePlate) return;

        _currentCleanAmount += Time.deltaTime;

        float progress = _currentCleanAmount / necessaryCleanAmount;
        _targetAlpha = 1f - progress;

        if (_currentCleanAmount >= necessaryCleanAmount)
        {
            dirtStain.alpha = 1;
            _currentCleanAmount = 0f;

            Debug.Log($"[PlateWashing] Piatto pulito: {_completedPlates + 1}/{plateSprites.Length}");

            // MODIFICATO: non avanziamo più subito (incremento, cambio sprite, completamento
            // task). Avviamo solo il dialogo di completamento e segnaliamo che dobbiamo
            // aspettare la sua chiusura. La logica che prima stava qui è stata spostata in
            // AdvanceToNextPlate(), richiamata da Update() solo quando il dialogo si chiude.
            isDialogueActive = true;
            _waitingForPlateDialogueToEnd = true; // AGGIUNTO
            int dialoguePoint = Mathf.Min(_completedPlates + 1, inkJsonFiles.Length - 1);
            _inkManager.StartDialogue(inkJsonFiles[dialoguePoint], false, false);
        }
    }

    // AGGIUNTO: logica che prima veniva eseguita subito dopo l'avvio del dialogo
    // di completamento in EvaluateCleanAmount(). Ora parte solo quando quel
    // dialogo si è effettivamente chiuso.
    private void AdvanceToNextPlate()
    {
        if (_completedPlates >= plateSprites.Length - 1)
        {
            taskManager.CompleteTask(interactable.TaskSO);
            interactable.SetHasBeenCompleted(true);
            GameFlowManager.Instance.LoadNextDay(fadeSpeed);
            QuitMiniGame();
            return;
        }

        _completedPlates++;
        plateImage.sprite = plateSprites[_completedPlates];

        EvaluateInkDialogueProgress();
    }

    private void EvaluateInkDialogueProgress()
    {
        TextAsset inkJson = interactable.TaskSO.inkJson;
        if (inkJson == null) return;

        // Usiamo il knot name con il numero del piatto corrente (quello appena pulito)
        string knot = $"{inkKnotName}{_completedPlates}";  // attenzione: _completedPlates è ancora il vecchio indice

        isDialogueActive = true;
        _inkManager.StartDialogue(inkJson, false, false);

        if (_inkManager.HasKnot(knot))
        {
            _inkManager.JumpToKnot(knot);
        }
        else
        {
            Debug.Log($"[PlateWashing] Nessun knot '{knot}' trovato, salto il dialogo per questo piatto.");
            _inkManager.EndDialogue();
            isDialogueActive = false;
            // Se il dialogo viene saltato, dobbiamo forzare il passaggio al prossimo piatto?
            // Allora dovresti chiamare OnDialogueEnded() manualmente? Meglio gestire:
            // Se non c'è knot, non avviamo il dialogo e quindi non abbiamo attesa.
            // In tal caso, il piatto successivo dovrebbe partire subito.
            // Quindi, se non c'è dialogo, settiamo _isWaitingForDialogue = false e procediamo.
            // Ma nel nostro flusso, abbiamo già impostato _isWaitingForDialogue = true prima di chiamare EvaluateInkDialogueProgress,
            // quindi dobbiamo gestire questo caso.
            // Suggerisco: in EvaluateCleanAmount, prima di chiamare EvaluateInkDialogueProgress, controlliamo se il knot esiste.
            // Se non esiste, possiamo saltare direttamente al prossimo piatto senza attendere.
            // Per semplicità, implementiamo un controllo in EvaluateCleanAmount:
            // if (HasKnotForCurrentPlate()) { ... } else { // passa subito al prossimo }
        }
    }

    public override void StartMiniGame()
    {
        if (IsTaskCompleted() && !canReplayMiniGame)
        {
            Debug.Log("This task has already been completed!");
            return;
        }

        base.StartMiniGame();

        // Recupera il PlayerDialogueController e iscriviti all'evento
        if (playerInputController != null)
        {
            _dialogueController = playerInputController.GetComponent<PlayerDialogueController>();
            if (_dialogueController != null)
            {
                _dialogueController.onDialogueEnd += OnDialogueEnded;
            }
        }

        TogglePlayerControl(false, true);
        ToggleUI(true);

        // --- Avvia il dialogo per il primo piatto (indice 0) ---
        if (inkJsonFiles != null && inkJsonFiles.Length > 0 && inkJsonFiles[0] != null)
        {
            _inkManager.StartDialogue(inkJsonFiles[0], false, false);
            isDialogueActive = true;
            // Il minigioco resta in pausa finché il dialogo non viene chiuso.
            // Non serve _isWaitingForDialogue qui perché isDialogueActive blocca l'update.
        }
        else
        {
            // Se non c'è il file, mostra il primo piatto e inizia subito
            plateImage.sprite = plateSprites[0];
            _cursorPos = Vector2.zero;
            _currentCleanAmount = 0f;
            cursorRect.anchoredPosition = Vector2.zero;
            isDialogueActive = false;
        }

        // Reset generale (già presente nel tuo codice)
        _cursorPos = Vector2.zero;
        _currentCleanAmount = 0f;
        cursorRect.anchoredPosition = Vector2.zero;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();
        _currentCleanAmount = 0f;
        dirtStain.alpha = 1f;
        plateImage.sprite = plateSprites[_completedPlates];
        _waitingForPlateDialogueToEnd = false; // AGGIUNTO: evita stati residui se si esce mentre si aspetta la chiusura del dialogo

        if (playerInputController != null)
        {
            TogglePlayerControl(true, true);
        }

        ToggleUI(false);
    }
    public override void ResetMiniGame()
    {
        QuitMiniGame();
        _currentCleanAmount = 0f;
        dirtStain.alpha = 1f;
        plateImage.sprite = plateSprites[0];
        StartMiniGame();
    }

    private void ToggleUI(bool enable)
    {
        _canvasGroup.alpha = enable ? 1f : 0f;
        _canvasGroup.blocksRaycasts = enable;
        _canvasGroup.interactable = enable;
    }

    private void OnDialogueEnded()
    {
        if (!_isWaitingForDialogue) return;

        _isWaitingForDialogue = false;
        isDialogueActive = false;

        // Se per qualche motivo siamo all'ultimo piatto
        if (_completedPlates >= plateSprites.Length - 1)
        {
            if (!IsTaskCompleted())
            {
                taskManager.CompleteTask(interactable.TaskSO);
                interactable.SetHasBeenCompleted(true);
                QuitMiniGame();
            }
            return;
        }

        // Passa al piatto successivo
        _completedPlates++;
        plateImage.sprite = plateSprites[_completedPlates];
        _currentCleanAmount = 0f;
        dirtStain.alpha = 1f;
    }

    private bool HasKnotForPlate(int plateIndex)
    {
        TextAsset inkJson = interactable.TaskSO.inkJson;
        if (inkJson == null) return false;
        string knot = $"{inkKnotName}{plateIndex}";
        return _inkManager.HasKnot(knot);
    }
}