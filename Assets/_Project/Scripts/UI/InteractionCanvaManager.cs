using TMPro;
using UnityEngine;

public class InteractionCanvaManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private AbstractInteractable interactable;

    // Chiamato esplicitamente da chi istanzia questo GameObject,
    // così non dipendiamo né da OnEnable né dalla gerarchia Parent.
    public void Initialize(AbstractInteractable owner)
    {
        interactable = owner;
        text.text = $"Interact with {interactable.TaskSO.TaskName}";
    }
}