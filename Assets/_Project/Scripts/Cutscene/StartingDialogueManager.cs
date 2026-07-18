using System;
using UnityEngine;

public class StartingDialogueManager : MonoBehaviour
{
    [SerializeField] private TextAsset InkDialogue;
    [SerializeField] private InkManager inkManager;

    private void Start()
    {
        inkManager.StartDialogue(InkDialogue, false);
    }
}
