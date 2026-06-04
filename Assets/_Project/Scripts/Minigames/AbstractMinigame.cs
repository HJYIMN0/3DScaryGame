using System;
using UnityEngine;

public abstract class AbstractMinigame : MonoBehaviour
{
    [SerializeField] protected AbstractInteractable interactable;
    [SerializeField] protected PlayerInputController _playerInputController;

    public bool IsMiniGameActive { get; private set; } = false;
    public bool HasMiniGameBeenCompleted => interactable.HasBeenCompleted;
    public TaskManager taskManager { get; private set;  }
    public virtual void Start()
    {
        if (_playerInputController == null)
        {
            Debug.LogWarning("PlayerInputController reference is missing in " + gameObject.name);
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
        if (_playerInputController.InputActions.Player.Look.enabled && _playerInputController.InputActions.Player.Move.enabled)
        {
            IsMiniGameActive = true;
            _playerInputController.InputActions.Player.Look.Disable();
            _playerInputController.InputActions.Player.Move.Disable();
        }
        else if (!_playerInputController.InputActions.Player.Look.enabled && !_playerInputController.InputActions.Player.Move.enabled)
        {
            IsMiniGameActive = false;
            _playerInputController.InputActions.Player.Look.Enable();
            _playerInputController.InputActions.Player.Move.Enable();
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
            _playerInputController.InputActions.Player.Look.Enable();
            _playerInputController.InputActions.Player.Move.Enable();
        }
        else
        {
            IsMiniGameActive = true;
            _playerInputController.InputActions.Player.Look.Disable();
            _playerInputController.InputActions.Player.Move.Disable();
        }
    }  
}
