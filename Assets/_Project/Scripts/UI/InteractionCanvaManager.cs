using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionCanvaManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private AbstractInteractable interactable;

    // Chiamato esplicitamente da chi istanzia questo GameObject,
    // così non dipendiamo né da OnEnable né dalla gerarchia Parent.
    public void Initialize(AbstractInteractable owner)
    {
        interactable = owner;

        PlayerInputController playerInputController = interactable.GetPlayerInteractionController().gameObject.GetComponent<PlayerInputController>();
        if (playerInputController == null)
        {
            Debug.LogError("PlayerInputController not found on the player GameObject.");
            return;
        }
        text.text = $"{playerInputController.InputActions.Player.Interact.GetBindingDisplayString(0)} to Interact with {interactable.TaskSO.TaskName}";
    }
}