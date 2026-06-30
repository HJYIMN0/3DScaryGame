using UnityEngine;
using UnityEngine.UI;
public class PlateWashingMiniGame : AbstractMinigame
{
    [Header("UI References")]
    [Tooltip("Reference here the map used to clean!")]
    [SerializeField] private RectTransform cursorRect;
    [Tooltip("Reference here the plate in the ui from inspector")]
    [SerializeField] private Image plateImage;
    [Tooltip("All the different plates images you need to clean")]
    [SerializeField] private Sprite[] plateSprites;

    [Header("Minigame Settings")]
    [SerializeField] private float lookSensitivity = 300f;
    [SerializeField] private int requiredCircles = 3;
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
    private Vector2 _prevCursorPos;
    private float _accumulatedAngle;
    private int _completedCircles;
    private int _completedPlates;

    private const float MinCircleRadius = 20f;
    private InkManager _inkManager => interactable.GetInkManager();

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
                isDialogueActive = false;
            return;
        }

        if (_playerInputController.InputActions.Player.Quit.IsPressed())
        {
            QuitMiniGame();
            return;
        }

        HandleMiniGameLogic();
    }

    // MODIFICATO: HandleMiniGameLogic ora è solo un orchestratore che chiama i metodi
    // dedicati in ordine. La logica interna di ognuno è identica a prima, solo spostata.
    public override void HandleMiniGameLogic()
    {
        _playerLookInput = _playerInputController.InputActions.Player.Look.ReadValue<Vector2>();

        if (_playerLookInput == Vector2.zero) return;

        EvaluateMousePosition();
        EvaluateAlpha();
        EvaluateCirclesNumber();
    }

    // AGGIUNTO: estratto da HandleMiniGameLogic. Aggiorna la posizione del cursore
    // in base all'input Look, applicando il clamp ai limiti del pannello UI.
    private void EvaluateMousePosition()
    {
        _cursorPos += _playerLookInput * lookSensitivity * Time.deltaTime;

        Vector2 halfSize = _uiPanelRect.rect.size * 0.5f;
        _cursorPos.x = Mathf.Clamp(_cursorPos.x, -halfSize.x, halfSize.x);
        _cursorPos.y = Mathf.Clamp(_cursorPos.y, -halfSize.y, halfSize.y);

        cursorRect.anchoredPosition = _cursorPos;
    }

    // AGGIUNTO: estratto da HandleMiniGameLogic. Gestisce solo il fade dello sporco,
    // logica identica a prima (Lerp verso _targetAlpha).
    private void EvaluateAlpha()
    {
        dirtStain.alpha = Mathf.Lerp(dirtStain.alpha, _targetAlpha, Time.deltaTime * fadeSpeed);
    }

    // AGGIUNTO: estratto da HandleMiniGameLogic. Calcola l'angolo accumulato e conta
    // i cerchi completati. CORRETTO: _prevCursorPos = _cursorPos viene ora eseguito
    // sempre a fine metodo, fuori dall'if — esattamente come nella versione originale.
    // Era stato spostato erroneamente dentro l'if in una modifica precedente, il che
    // congelava _prevCursorPos non appena il cursore rientrava sotto MinCircleRadius,
    // rompendo il calcolo dell'angolo e quindi sia il conteggio dei giri che il fade alpha.
    private void EvaluateCirclesNumber()
    {
        if (_prevCursorPos.magnitude > MinCircleRadius && _cursorPos.magnitude > MinCircleRadius)
        {
            float prevAngle = Mathf.Atan2(_prevCursorPos.y, _prevCursorPos.x) * Mathf.Rad2Deg;
            float currAngle = Mathf.Atan2(_cursorPos.y, _cursorPos.x) * Mathf.Rad2Deg;
            _accumulatedAngle += Mathf.DeltaAngle(prevAngle, currAngle);

            if (Mathf.Abs(_accumulatedAngle) >= 360f)
            {
                _completedCircles++;
                _accumulatedAngle -= Mathf.Sign(_accumulatedAngle) * 360f;

                float progress = (float)_completedCircles / requiredCircles;
                _targetAlpha = 1f - progress;

                Debug.Log($"[PlateWashing] Cerchio completato: {_completedCircles}/{requiredCircles}");

                if (_completedCircles >= requiredCircles)
                {
                    dirtStain.alpha = 1;
                    _prevCursorPos = _cursorPos;
                    _completedCircles = 0;
                    _accumulatedAngle = 0;

                    if (_completedPlates >= plateSprites.Length - 1)
                    {
                        taskManager.CompleteTask(interactable.TaskSO.TaskName);
                        interactable.SetHasBeenCompleted(true);
                        QuitMiniGame();
                        return;
                    }

                    _completedPlates++;
                    plateImage.sprite = plateSprites[_completedPlates];

                    EvaluateInkDialogueProgress();
                }
            }
        }

        // CORRETTO: spostato fuori dall'if, ripristinando il comportamento originale
        _prevCursorPos = _cursorPos;
    }

    private void EvaluateInkDialogueProgress()
    {
        TextAsset inkJson = interactable.TaskSO.inkJson;
        if (inkJson == null) return;

        string knot = $"{inkKnotName}{_completedPlates - 1}";

        isDialogueActive = true;
        _inkManager.StartDialogue(inkJson, false, false);

        if (_inkManager.HasKnot(knot))
        {
            _inkManager.JumpToKnot(knot);
        }
        else
        {
            // AGGIUNTO: nessun dialogo scritto per questo piatto, chiudi subito senza
            // mostrare nulla. EndDialogue ripristina lo stato esattamente come se il
            // dialogo non fosse mai iniziato (incluso il toggle di PlayerDialogueController).
            Debug.Log($"[PlateWashing] Nessun knot '{knot}' trovato, salto il dialogo per questo piatto.");
            _inkManager.EndDialogue();
            isDialogueActive = false;
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

        TogglePlayerControl(false, true);

        ToggleUI(true);

        TextAsset inkJson = interactable.TaskSO.inkJson;
        _inkManager.StartDialogue(inkJson, false, false);
        _inkManager.JumpToKnot("tutorial");
        isDialogueActive = true;


        _cursorPos = Vector2.zero;
        _prevCursorPos = Vector2.zero;
        _accumulatedAngle = 0f;
        _completedCircles = 0;
        cursorRect.anchoredPosition = Vector2.zero;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();
        _completedCircles = 0;
        _accumulatedAngle = 0f;
        dirtStain.alpha = 1f;
        plateImage.sprite = plateSprites[_completedCircles];

        TogglePlayerControl(true, true);

        ToggleUI(false);
    }

    public override void ResetMiniGame()
    {
        QuitMiniGame();
        _completedCircles = 0;
        _accumulatedAngle = 0f;
        dirtStain.alpha = 1f;
        plateImage.sprite = plateSprites[_completedCircles];
        StartMiniGame();
    }

    private void ToggleUI(bool enable)
    {
        _canvasGroup.alpha = enable ? 1f : 0f;
        _canvasGroup.blocksRaycasts = enable;
        _canvasGroup.interactable = enable;
    }
}