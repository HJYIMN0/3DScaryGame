using UnityEngine;

public class StartingDialogueManager : MonoBehaviour
{
    [SerializeField] private TextAsset InkDialogue;

    private void Start()
    {
        InkManager.Instance.StartDialogue(InkDialogue, false);
    }
}
