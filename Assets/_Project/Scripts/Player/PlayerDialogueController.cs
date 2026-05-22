using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerDialogueController : MonoBehaviour
{
    private PlayerInteractionController playerInteractionController;
    private PlayerMovementController playerMovementController;
    private PlayerInputController playerInputController;
    private InkManager inkManager;
    private InkManagerUI inkManagerUI;

    public bool IsDialogueActive => inkManager != null && inkManager.IsStoryActive;

    private void Awake()
    {
        playerInteractionController = GetComponent<PlayerInteractionController>();
        playerInteractionController.enabled = true;
        playerMovementController = GetComponent<PlayerMovementController>();
        playerMovementController.enabled = true;
        playerInputController = GetComponent<PlayerInputController>();
        inkManager = GetComponent<InkManager>();
        inkManagerUI = GetComponent<InkManagerUI>();

        this.enabled = false;
    }

    private void OnEnable()
    {
        playerMovementController.StopMovement();
        playerInputController.OnAttackAction += HandleDialogue;
        Debug.Log("PlayerDialogueController enabled and subscribed to OnAttackAction.");
    }

    private void OnDisable()
    {
        playerMovementController.StartMovement();
        playerInputController.OnAttackAction -= HandleDialogue;
    }

    // Rimosso OnDestroy(). È ridondante poiché OnDisable viene sempre chiamato durante la distruzione.

    private void HandleDialogue()
    {
        if (inkManager.IsStoryActive)
        {
            // MODIFICATO: rimosso IsPointerOverGameObject() — non affidabile con UI sempre presente.
            // Si blocca direttamente se ci sono scelte in attesa di essere compiute.
            if (inkManagerUI.HasActiveChoices) return;

            inkManager.ContinueDialogue();
            return;
        }

        if (inkManager.IsDialogueOpen)
        {
            inkManager.EndDialogue();
            return;
        }

        if (playerInteractionController != null &&
            playerInteractionController.interactableTask != null &&
            playerInteractionController.interactableTask.TaskSO != null)
        {
            inkManager.StartDialogue(playerInteractionController.interactableTask.TaskSO.inkJson,
                                      playerInteractionController.interactableTask.TaskSO.usesVariablesInInk);
        }
    }
}