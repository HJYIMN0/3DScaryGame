using System;
using UnityEngine;

public class InteractableTask : MonoBehaviour, iInteractable
{
    [SerializeField] private TaskSO task;
    [SerializeField] private float canvaPos = 1.15f;
    
    public event Action OnInteractionStart;
    public event Action OnInteractionEnd;
    private bool isCanvaInstantiated = false;
    private GameObject canvaInstance;
    private PlayerInteractionSystem playerInteractionSystem;

    private void Start()
    {
        if (task != null) 
        {
            DayManager.Instance.AddTask(task);
            Debug.Log($"Added task '{task.TaskName}' to DayManager.");
        }
        else
        {
            Debug.LogWarning("No task assigned to this InteractableTask.");
        }
    }

    private void Update()
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
            if (playerInteractionSystem != null)
            {
                playerInteractionSystem.ClearInteractableTaskForPlayer();
                Debug.Log("Player left, clearing interactable task for player.");
            }
        }
    }

    public void EvaluateCanvaStatus(GameObject player, Vector3 pos, Quaternion rot, GameObject canvaObj)
    {
        Debug.Log("Player is here!");
        if (isCanvaInstantiated) return;


        if (playerInteractionSystem == null)
        {
           playerInteractionSystem = player.GetComponent<PlayerInteractionSystem>();
        }
        playerInteractionSystem.SetInteractableTaskForPlayer(this);

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
    public void InteractWithTask()
    {
        if (task != null)
        {
            DayManager.Instance.CompleteTask(task.TaskName);
            Debug.Log($"Interacted with task: {task.TaskName}");
        }
        else
        {
            Debug.LogWarning("No task assigned to this interactable.");
        }
        OnInteractionStart?.Invoke();
        OnInteractionEnd?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, task.interactionRadius);
    }
}
