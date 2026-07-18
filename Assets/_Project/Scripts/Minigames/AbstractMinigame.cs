using System;
using Unity.Cinemachine;
using UnityEngine;

public abstract class AbstractMinigame : MonoBehaviour
{
    [SerializeField] protected AbstractInteractable interactable;
    [SerializeField] protected CinemachineCamera minigameCamera;

    protected PlayerInputController playerInputController;

    protected bool isDialogueActive = false;
    public bool IsMiniGameActive { get; private set; } = false;
    public bool HasMiniGameBeenCompleted => interactable.HasBeenCompleted;
    public TaskManager taskManager { get; private set; }

    public void SetPlayerInputController(PlayerInputController playerInputController)
    {
        if (this.playerInputController != playerInputController)
        {
            this.playerInputController = playerInputController;
        }
    }
    public virtual void Start()
    {
        if (interactable.TaskSO == null)
        {
            Debug.LogWarning("Minigame TaskSO reference is missing in " + gameObject.name);
        }

        QuitMiniGame();
        //TogglePlayerControl(true, true);
        taskManager = TaskManager.Instance;
    }

    public virtual void StartMiniGame()
    {
        IsMiniGameActive = true;

        if (minigameCamera != null && !minigameCamera.gameObject.activeSelf)
            minigameCamera.gameObject.SetActive(true);
        if (playerInputController != null)
        {
            if (playerInputController.CameraController != null)
            {
                playerInputController.CameraController.enabled = false;
            }
        }
    }
    public virtual void QuitMiniGame()
    {
        IsMiniGameActive = false;

        if (minigameCamera != null && minigameCamera.gameObject.activeSelf)
            minigameCamera.gameObject.SetActive(false);

        TogglePlayerControl(true, true);
        Cursor.visible = false;

        //playerInputController?.CameraController.StartLook();
        if (playerInputController != null)
        {
            if (playerInputController.CameraController != null)
            {
                playerInputController.CameraController.enabled = true;
            }
        }
    }
    public abstract void ResetMiniGame();
    public abstract void HandleMiniGameLogic();
    public bool IsTaskCompleted() => taskManager.CompletedTasks.Contains(interactable.TaskSO);

    public void TogglePlayerControl(bool canMove, bool canLook)
    {
        if (playerInputController == null) return;

        if (canLook)
            playerInputController.InputActions.Player.Look.Enable();

        if (canMove)
            playerInputController.InputActions.Player.Move.Enable();

        if (!canLook)
            playerInputController.InputActions.Player.Look.Disable();

        if (!canMove)
            playerInputController.InputActions.Player.Move.Disable();
    }
}