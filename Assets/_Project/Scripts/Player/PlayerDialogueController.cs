using UnityEngine;
using System;

public class PlayerDialogueController : MonoBehaviour
{
    private PlayerInteractionController _playerInteractionController;
    private PlayerInputController _input;
    private InkManager _inkManager;
    private InkManagerUI _inkManagerUI;

    private bool _canMoveDuringDialogue = false;
    private bool _choicesActive = false;
    private bool _movementStopped = false;

    public bool IsDialogueActive => _inkManager != null && _inkManager.IsStoryActive;
    public bool HasActiveChoices => _inkManagerUI != null && _inkManagerUI.HasActiveChoices;
    public Action onDialogueEnd;

    private void Awake()
    {
        _playerInteractionController = GetComponent<PlayerInteractionController>();
        _input = GetComponent<PlayerInputController>();
        _inkManager = GetComponent<InkManager>();
        _inkManagerUI = GetComponent<InkManagerUI>();

        this.enabled = false;
    }

    /// <summary>
    /// Imposta se il player può muoversi durante il dialogo.
    /// Va chiamato prima di abilitare il componente, oppure anche dopo:
    /// in quel caso la modifica viene applicata subito.
    /// </summary>
    public void SetCanMoveDuringDialogue(bool value)
    {
        _canMoveDuringDialogue = value;
        if (isActiveAndEnabled) ApplyMovementState();
    }

    /// <summary>
    /// Chiamato dalla UI quando compaiono/scompaiono le scelte.
    /// Quando le scelte sono attive blocca SEMPRE movimento e camera,
    /// indipendentemente da canPlayerMove. Quando scompaiono, ripristina
    /// lo stato coerente con _canMoveDuringDialogue.
    /// </summary>
    public void SetChoicesActive(bool active)
    {
        _choicesActive = active;

        if (isActiveAndEnabled)
            ApplyMovementState();

        if (active)
            _input?.CameraController?.StopLook();
        else
            _input?.CameraController?.StartLook();
    }

    private void ApplyMovementState()
    {
        bool shouldStop = !_canMoveDuringDialogue || _choicesActive;

        if (shouldStop)
        {
            if (!_movementStopped)
            {
                _input?.MovementController?.StopMovement();
                _movementStopped = true;
            }
        }
        else
        {
            if (_movementStopped)
            {
                _input?.MovementController?.StartMovement();
                _movementStopped = false;
            }
        }
    }

    private void OnEnable()
    {
        _movementStopped = false;
        ApplyMovementState();
    }

    private void OnDisable()
    {
        if (_movementStopped)
        {
            _input?.MovementController?.StartMovement();
            _movementStopped = false;
        }
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
            onDialogueEnd?.Invoke();
            _inkManager.EndDialogue();
            return;
        }

        if (_playerInteractionController != null && _playerInteractionController.interactableTask != null)
        {
            _playerInteractionController.interactableTask.InteractWithTask();
        }
    }
}