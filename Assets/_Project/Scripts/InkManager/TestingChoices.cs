using Unity.VisualScripting;
using UnityEngine;

public class TestingChoices : MonoBehaviour
{
    [SerializeField] private TextAsset testChoiceDialogue;
    [SerializeField] private InkManager inkManager;
    void Start()
    {
        inkManager.StartDialogue(testChoiceDialogue, false, false);
    }
}
