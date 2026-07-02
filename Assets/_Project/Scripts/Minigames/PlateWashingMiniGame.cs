using Unity.Cinemachine;
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
    // AGGIUNTO: quantità di pulizia accumulata, cresce mentre il cursore resta
    // dentro il rect del piatto. Sostituisce _accumulatedAngle/_completedCircles.
    private float _currentCleanAmount;
    private int _completedPlates;

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

            if (_completedPlates >= plateSprites.Length - 1)
            {
                taskManager.CompleteTask(interactable.TaskSO);
                interactable.SetHasBeenCompleted(true);
                QuitMiniGame();
                return;
            }

            _completedPlates++;
            plateImage.sprite = plateSprites[_completedPlates];

            EvaluateInkDialogueProgress();
        }
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
        // MODIFICATO: non c'è più _prevCursorPos/_accumulatedAngle da resettare,
        // resettiamo invece _currentCleanAmount.
        _currentCleanAmount = 0f;
        cursorRect.anchoredPosition = Vector2.zero;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();
        _currentCleanAmount = 0f;
        dirtStain.alpha = 1f;
        plateImage.sprite = plateSprites[_completedPlates];

        if (playerInputController != null )
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
}