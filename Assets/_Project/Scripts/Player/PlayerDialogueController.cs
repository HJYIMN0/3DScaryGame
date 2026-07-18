using UnityEngine;

public class PlayerDialogueController : MonoBehaviour
{
    private PlayerInteractionController _playerInteractionController;
    private PlayerInputController _input;
    private InkManager _inkManager;
    private InkManagerUI _inkManagerUI;

    public bool IsDialogueActive => _inkManager != null && _inkManager.IsStoryActive;
    public bool HasActiveChoices => _inkManagerUI != null && _inkManagerUI.HasActiveChoices;

    private void Awake()
    {
        _playerInteractionController = GetComponent<PlayerInteractionController>();
        _input = GetComponent<PlayerInputController>();
        _inkManager = GetComponent<InkManager>();
        _inkManagerUI = GetComponent<InkManagerUI>();

        this.enabled = false;
    }

    private void OnEnable()
    {
        _input?.MovementController?.StopMovement();
    }

    private void OnDisable()
    {
        _input?.MovementController?.StartMovement();
    }

    private void Update()
    {
        // Avanzamento dialogo / attacco
        if (_input?.InputActions.Player.Attack.WasPressedThisFrame() == true)
            HandleDialogue();

        // Navigazione scelte — solo quando ci sono scelte attive
        if (HasActiveChoices)
        {
            if (_input.InputActions.Player.Previous.WasPressedThisFrame())
                _inkManagerUI.SelectPreviousChoice();

            if (_input.InputActions.Player.Next.WasPressedThisFrame())
                _inkManagerUI.SelectNextChoice();

            if (_input.InputActions.Player.Jump.WasPressedThisFrame())
                _inkManagerUI.ConfirmSelectedChoice();
        }
    }

    private void HandleDialogue()
    {
        if (_inkManager.IsStoryActive)
        {
            if (HasActiveChoices) return;
            _inkManager.ContinueDialogue();
            return;
        }

        if (_inkManager.IsDialogueOpen)
        {
            _inkManager.EndDialogue();
            return;
        }
        if (_playerInteractionController != null && _playerInteractionController.interactableTask != null)
        {
            _playerInteractionController.interactableTask.InteractWithTask();
        }
    }
}