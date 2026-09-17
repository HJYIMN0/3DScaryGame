using System;
using UnityEngine;
using UnityEngine.Events;

public class StartingDialogueManager : MonoBehaviour
{
    [SerializeField] private TextAsset InkDialogue;
    [SerializeField] private InkManager inkManager;
    [SerializeField] private bool canPlayerMoveOnDialogue = false;

    public TextAsset InkDialogueAsset => InkDialogue;
    private void Start()
    {
        inkManager.StartDialogue(InkDialogue, false, canPlayerMoveOnDialogue);
    }
}
