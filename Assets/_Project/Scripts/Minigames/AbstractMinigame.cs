using System;
using Unity.Cinemachine;
using UnityEngine;

public abstract class AbstractMinigame : MonoBehaviour
{
    [SerializeField] protected AbstractInteractable interactable;
    [SerializeField] protected CinemachineCamera minigameCamera;

    protected PlayerInputController playerInputController;
    protected PlayerMovementController playerMovementController;

    protected bool isDialogueActive = false;
    public bool IsMiniGameActive { get; private set; } = false;
    public bool HasMiniGameBeenCompleted => interactable.HasBeenCompleted;
    public TaskManager taskManager { get; private set; }

    public void SetPlayerInputController(PlayerInputController playerInputController)
    {
        if (this.playerInputController != playerInputController)
        {
            this.playerInputController = playerInputController;
            // AGGIUNTO: recupera PlayerMovementController insieme a PlayerInputController,
            // dato che stanno sullo stesso GameObject del Player. Se playerInputController
            // è null (es. ClearActiveMiniGameForPlayer), diventa null anche questo.
            playerMovementController = playerInputController != null
                ? playerInputController.GetComponent<PlayerMovementController>()
                : null;
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

        // AGGIUNTO: blocca la rotazione della camera del player durante il
        // minigioco. TogglePlayerControl(false, true) lascia l'azione Look
        // abilitata (serve al minigioco per muovere il cursore), quindi non
        // basta: qui disattiviamo specificamente HandleLook() in
        // PlayerMovementController tramite il suo flag CanLook.
        playerMovementController?.StopLook();
    }
    public virtual void QuitMiniGame()
    {
        IsMiniGameActive = false;

        if (minigameCamera != null && minigameCamera.gameObject.activeSelf)
            minigameCamera.gameObject.SetActive(false);

        TogglePlayerControl(true, true);
        Cursor.visible = false;
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