using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gestisce esclusivamente il lifecycle di InputSystem_Actions.
/// Espone l'asset pubblicamente — ogni controller legge ciò che gli serve
/// direttamente tramite polling (WasPressedThisFrame, IsPressed, ReadValue).
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    public InputSystem_Actions InputActions { get; private set; }

    private void Awake()
    {
        InputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        InputActions.Player.Enable();
    }

    private void OnDisable()
    {
        InputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        InputActions.Dispose();
    }
}