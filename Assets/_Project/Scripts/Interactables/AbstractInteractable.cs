using System;
using UnityEngine;
public abstract class AbstractInteractable : MonoBehaviour
{
    [SerializeField] protected TaskSO task;
    [SerializeField] private float canvaPos = 1.15f;
    private bool isCanvaInstantiated = false;
    private GameObject canvaInstance;
    private PlayerInteractionController playerInteractionController;

    public bool HasBeenInteractedWith { get; protected set; } = false;

    protected virtual void Start()
    {
        if (task != null) 
        {
            TaskManager.Instance.AddTask(task);
            Debug.Log($"Added task '{task.TaskName}' to DayManager.");
        }
        else
        {
            Debug.LogWarning("No task assigned to this InteractableTask.");
        }
    }

    protected virtual void Update()
    {
        Collider[] player = Physics.OverlapSphere(transform.position, task.interactionRadius, LayerMask.GetMask("Player"));
        if (player.Length > 0)
        {
            EvaluateCanvaStatus(player[0].gameObject, 
                                new Vector3(transform.position.x, transform.position.y + canvaPos, transform.position.z)
                                , transform.rotation, task.canvaObj);
        }
        else
        {
            if (isCanvaInstantiated && canvaInstance != null)
            {
                canvaInstance.SetActive(false);
                isCanvaInstantiated = false;
                Debug.Log("Player left, deactivating canva.");
            }
            if (playerInteractionController != null && playerInteractionController.interactableTask == this)
            {
                playerInteractionController.ClearInteractableTaskForPlayer();
                Debug.Log("Player left, clearing interactable task for player.");
            }
        }
    }

    public void EvaluateCanvaStatus(GameObject player, Vector3 pos, Quaternion rot, GameObject canvaObj)
    {
        Debug.Log("Player is here!");
        if (isCanvaInstantiated) return;


        if (playerInteractionController == null)
        {
           playerInteractionController = player.GetComponent<PlayerInteractionController>();
        }
        playerInteractionController.SetInteractableTaskForPlayer(this);

        if (!isCanvaInstantiated && canvaInstance != null) 
            {
                canvaInstance.transform.position = pos;
                canvaInstance.transform.rotation = rot;
                canvaInstance.SetActive(true);
                isCanvaInstantiated = true;
                Debug.Log("Canva wasn't null, but not instantiated. Instantiating now...");
            }
        else if (!isCanvaInstantiated && canvaInstance == null)
        {
           canvaInstance = Instantiate(canvaObj, pos, rot);
           canvaInstance.transform.SetParent(transform); // Set the parent to the interactable object
           canvaInstance.SetActive(true);
           isCanvaInstantiated = true;
           Debug.Log("Canva was null, instantiating now...");
        }
    }
    public abstract void InteractWithTask();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, task.interactionRadius);
    }
}
