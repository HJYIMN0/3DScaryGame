using System;
using UnityEngine;

public abstract class AbstractMinigame : MonoBehaviour
{
    [SerializeField] protected AbstractInteractable interactable;
    [SerializeField] protected PlayerMovementController _playerMovementController;

    public bool IsMiniGameActive { get; private set; } = false;
    public TaskManager taskManager { get; private set;  }
    public virtual void Start()
    {
        if (_playerMovementController == null)
        {
            Debug.LogWarning("PlayerMovementController reference is missing in " + gameObject.name);
        }
        if (interactable.TaskSO == null)
        {
            Debug.LogWarning("Minigame TaskSO reference is missing in " + gameObject.name);
        }

        QuitMiniGame();
        TogglePlayerControl(true);
        taskManager = TaskManager.Instance;
    }

    public abstract void StartMiniGame();
    public abstract void QuitMiniGame();
    public abstract void ResetMiniGame();
    public abstract void HandleMiniGameLogic();
    public bool IsTaskCompleted() => taskManager.CompletedTasks.Contains(interactable.TaskSO);

    public void TogglePlayerControl()
    {
        if (_playerMovementController.CanLook && _playerMovementController.CanMove)
        {
            IsMiniGameActive = true;
            _playerMovementController.StopLook();
            _playerMovementController.StopMovement();
        }
        else if (!_playerMovementController.CanLook && !_playerMovementController.CanMove)
        {
            IsMiniGameActive = false;
            _playerMovementController.StartLook();
            _playerMovementController.StartMovement();
        }
        else
        {
            Debug.LogWarning("PlayerMovementController is in an inconsistent state in " + gameObject.name);
        }
    }

    public void TogglePlayerControl(bool enableControl)
    {
        if (enableControl)
        {
            IsMiniGameActive = false;
            _playerMovementController.StartLook();
            _playerMovementController.StartMovement();
        }
        else
        {
            IsMiniGameActive = true;
            _playerMovementController.StopLook();
            _playerMovementController.StopMovement();
        }
    }  
}
