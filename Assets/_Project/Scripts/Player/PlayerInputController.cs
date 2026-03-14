using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestisce il lifecycle dell'InputSystem e la lettura dei valori di input.
/// Espone proprietà pubbliche che altri controller (PlayerMovementController, ecc.)
/// possono leggere senza dover reimplementare IPlayerActions.
/// </summary>
public class PlayerInputController : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    // -------------------------------------------------------------------------
    // Proprietà pubbliche lette dagli altri controller
    // -------------------------------------------------------------------------

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public Action OnInteractAction;

    // -------------------------------------------------------------------------
    // Stato interno
    // -------------------------------------------------------------------------

    private InputSystem_Actions _inputActions;


    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Player.AddCallbacks(this);
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        _inputActions.Player.RemoveCallbacks(this);
        _inputActions.Dispose();
    }

    private void LateUpdate()
    {
        // JumpPressed è edge-triggered: viene consumato una volta per frame
        // da chi lo legge (PlayerMovementController), poi resettato qui.
        JumpPressed = false;
    }

    // -------------------------------------------------------------------------
    // Implementazione IPlayerActions
    // -------------------------------------------------------------------------

    public void OnMove(InputAction.CallbackContext context)
        => MoveInput = context.ReadValue<Vector2>();

    public void OnLook(InputAction.CallbackContext context)
        => LookInput = context.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) JumpPressed = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
        => IsSprinting = context.ReadValueAsButton();

    public void OnCrouch(InputAction.CallbackContext context)
        => IsCrouching = context.ReadValueAsButton();

    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context)
    {
        // ERRORE PRECEDENTE: Mancanza di output in console per tracciare l'esecuzione.
        // MODIFICA: Mantenuto l'early exit originale e inserito il debug testuale.
        // Se non siamo nel frame esatto in cui il tasto viene premuto (started), esce.
        if (!context.started) return;

        Debug.Log("Interazione: Tasto E premuto.");
        OnInteractAction?.Invoke();
    }
    public void OnPrevious(InputAction.CallbackContext  context) { }
    public void OnNext(InputAction.CallbackContext context) { }
}