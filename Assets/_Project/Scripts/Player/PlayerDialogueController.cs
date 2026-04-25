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
        // FIX: il controllo su IsStoryActive viene spostato FUORI dal blocco condizionale.
        // Se una storia è già attiva (es. avviata da StartingDialogueManager),
        // non serve alcun interactable per poterla continuare.
        if (_inkManager.IsStoryActive)
        {
            _inkManager.ContinueDialogue();
            return; // aggiunto return per evitare di entrare nel blocco sottostante
        }

        // Il blocco originale rimane invariato: serve solo per AVVIARE
        // un dialogo tramite un interactable.
        if (playerInteractionController != null &&
            playerInteractionController.interactableTask != null &&
            playerInteractionController.interactableTask.TaskSO != null)
        {
            _inkManager.StartDialogue(playerInteractionController.interactableTask.TaskSO.inkJson,
                                      playerInteractionController.interactableTask.TaskSO.usesVariablesInInk);
        }
    }
}