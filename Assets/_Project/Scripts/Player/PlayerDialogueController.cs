using System;
using UnityEngine;

public class PlayerDialogueController : MonoBehaviour
{
    private PlayerInteractionController playerInteractionController;
    private PlayerMovementController playerMovementController;
    private PlayerInputController playerInputController;
    private InkManager _inkManager;

    public bool IsDialogueActive => _inkManager != null && _inkManager.IsStoryActive;

    private void Awake()
    {
        playerInteractionController = GetComponent<PlayerInteractionController>();
        playerInteractionController.enabled = true;
        playerMovementController = GetComponent<PlayerMovementController>();
        playerMovementController.enabled = true;
        playerInputController = GetComponent<PlayerInputController>();

        this.enabled = false;
    }

    private void Start()
    {
        if (_inkManager == null)
        {
            _inkManager = InkManager.Instance;
        }
    }

    private void OnEnable()
    {
        playerMovementController.StopMovement();
        if (_inkManager == null)
        {
            try 
            {
                _inkManager = InkManager.Instance; 
                
            }            
            catch(NullReferenceException e)
            {
                //Qui è dove far partire la coroutine che aspetta almeno un frame prima
                //di cercare l'istanza di InkManager, in modo da assicurarsi che sia stata creata.
                Debug.LogError("InkManager instance not found: " + e.Message);
            }
        }
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
        // MODIFICATO: distingue tre casi:
        // 1. Storia Ink attiva → continua la storia
        // 2. Canvas aperto ma nessuna storia (testo plain text) → chiudi il canvas
        // 3. Nessun dialogo aperto → avvia una nuova storia tramite l'interactable
        if (_inkManager.IsStoryActive)
        {
            _inkManager.ContinueDialogue();
            return;
        }

        // AGGIUNTO: caso plain text — chiude il canvas senza tentare di avviare una storia
        if (_inkManager.IsDialogueOpen)
        {
            _inkManager.EndDialogue();
            return;
        }

        if (playerInteractionController != null &&
            playerInteractionController.interactableTask != null &&
            playerInteractionController.interactableTask.TaskSO != null)
        {
            _inkManager.StartDialogue(playerInteractionController.interactableTask.TaskSO.inkJson,
                                      playerInteractionController.interactableTask.TaskSO.usesVariablesInInk);
        }
    }
}