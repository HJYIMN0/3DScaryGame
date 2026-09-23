using System;
using UnityEngine;
using UnityEngine.Events;

public class StartingDialogueManager : MonoBehaviour
{
    [SerializeField] private TextAsset inkDialogue;
    [SerializeField] private InkManager inkManager;
    [SerializeField] private bool canPlayerMoveOnDialogue = false;

    public TextAsset InkDialogue => inkDialogue;

    public TextAsset InkDialogueAsset => InkDialogue;
    private void Start()
    {
        inkManager.StartDialogue(InkDialogue, false, canPlayerMoveOnDialogue);
    }
}
