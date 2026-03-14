using UnityEngine;

public class PlayerInteractionSystem : MonoBehaviour
{
    public InteractableTask interactableTask { get; private set; }
    //public FirstPersonController playerController;


    public void SetInteractableTaskForPlayer(InteractableTask taskToInteractWith)
    {
        Debug.Log("Setting interactable task for player: " + taskToInteractWith.name);
        interactableTask = taskToInteractWith;
    }
    public void ClearInteractableTaskForPlayer()
    {
        Debug.Log("Clearing interactable task for player.");
        interactableTask = null;
    }
}
