using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PlateWashingMiniGame : AbstractMinigame
{
    [Header("UI References")]
    [SerializeField] private RectTransform _cursorRect;

    [Header("Minigame Settings")]
    [SerializeField] private float lookSensitivity = 300f;
    [SerializeField] private int requiredCircles = 3;
    [SerializeField] private bool isDebugMode = false;
    [SerializeField] private bool canReplayMiniGame = true;

    private CanvasGroup _canvasGroup;
    private RectTransform _uiPanelRect;

    private Vector2 _playerLookInput;
    private Vector2 _cursorPos;
    private Vector2 _prevCursorPos;
    private float _accumulatedAngle;
    private int _completedCircles;

    private const float MinCircleRadius = 20f;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _uiPanelRect = GetComponent<RectTransform>();
    }

    public override void Start()
    {
        base.Start();

        if (isDebugMode)
            StartMiniGame();
    }

    private void Update()
    {
        if (!IsMiniGameActive) return;

        if (_playerInputController.InputActions.Player.Quit.IsPressed())
        {
            QuitMiniGame();
            return;
        }

        HandleMiniGameLogic();
    }

    public override void HandleMiniGameLogic()
    {
        _playerLookInput = _playerInputController.InputActions.Player.Look.ReadValue<Vector2>();

        if (_playerLookInput == Vector2.zero) return;

        _cursorPos += _playerLookInput * lookSensitivity * Time.deltaTime;

        Vector2 halfSize = _uiPanelRect.rect.size * 0.5f;
        _cursorPos.x = Mathf.Clamp(_cursorPos.x, -halfSize.x, halfSize.x);
        _cursorPos.y = Mathf.Clamp(_cursorPos.y, -halfSize.y, halfSize.y);

        _cursorRect.anchoredPosition = _cursorPos;

        if (_prevCursorPos.magnitude > MinCircleRadius && _cursorPos.magnitude > MinCircleRadius)
        {
            float prevAngle = Mathf.Atan2(_prevCursorPos.y, _prevCursorPos.x) * Mathf.Rad2Deg;
            float currAngle = Mathf.Atan2(_cursorPos.y, _cursorPos.x) * Mathf.Rad2Deg;
            _accumulatedAngle += Mathf.DeltaAngle(prevAngle, currAngle);

            if (Mathf.Abs(_accumulatedAngle) >= 360f)
            {
                _completedCircles++;
                _accumulatedAngle -= Mathf.Sign(_accumulatedAngle) * 360f;

                if (_completedCircles >= requiredCircles)
                {
                    taskManager.CompleteTask(interactable.TaskSO.TaskName);
                    interactable.SetHasBeenCompleted(true);
                    QuitMiniGame();
                    return;
                }
            }
        }

        _prevCursorPos = _cursorPos;
    }

    public override void StartMiniGame()
    {
        if (IsTaskCompleted() && !canReplayMiniGame)
        {
            Debug.Log("This task has already been completed!");
            return;
        }

        // CORREZIONE CRITICA: Ordine delle operazioni.
        // Prima chiamiamo TogglePlayerControl. Questo disabiliterà sia 'Move' che 'Look' del giocatore 
        // nel caso standard, e imposterà IsMiniGameActive a true.
        TogglePlayerControl(false);

        // Successivamente, forziamo l'abilitazione della singola azione 'Look' in modo 
        // che possa essere letta da HandleMiniGameLogic.
        _playerInputController.InputActions.Player.Look.Enable();

        ToggleUI(true);

        _cursorPos = Vector2.zero;
        _prevCursorPos = Vector2.zero;
        _accumulatedAngle = 0f;
        _completedCircles = 0;
        _cursorRect.anchoredPosition = Vector2.zero;
    }

    public override void QuitMiniGame()
    {
        TogglePlayerControl(true);

        ToggleUI(false);
    }

    public override void ResetMiniGame()
    {
        QuitMiniGame();
        _completedCircles = 0;
        _accumulatedAngle = 0f;
        StartMiniGame();
    }

    private void ToggleUI(bool enable)
    {
        _canvasGroup.alpha = enable ? 1f : 0f;
        _canvasGroup.blocksRaycasts = enable;
        _canvasGroup.interactable = enable;
    }
}