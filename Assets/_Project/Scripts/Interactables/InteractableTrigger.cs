using UnityEngine;

public class InteractableTrigger : MonoBehaviour
{
    [SerializeField] private AbstractInteractable interactable;
    [SerializeField] private float canvaPos = 1.15f;

    private PlayerInteractionController playerInteractionController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            interactable.EvaluateCanvaStatus(other.gameObject.GetComponent<PlayerInteractionController>(),
                                        new Vector3(interactable.gameObject.transform.position.x, transform.position.y + canvaPos, interactable.gameObject.transform.position.z)
                                        , Camera.main.transform.rotation, interactable.TaskSO.canvaObj);

            playerInteractionController = other.gameObject.GetComponent<PlayerInteractionController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            interactable.DeactivateCanvas();

            // 2. Pulisce il riferimento all'interactable sul player e lo story di Ink
            if (playerInteractionController != null && playerInteractionController.interactableTask == interactable)
            {
                playerInteractionController.ClearInteractableTaskForPlayer();
                InkManager.Instance.ClearStoryAndTextAsset();
                Debug.Log("Player left, clearing interactable task for player.");
            }
        }
    }
}