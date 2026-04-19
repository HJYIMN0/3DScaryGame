using UnityEngine;

public class Interactable_Hole : AbstractInteractable
{
    [SerializeField] private GameObject UiVideoCanva;
    public override void InteractWithTask()
    {
        int day = GameFlowManager.Instance.CurrentDay;
        switch (day)
        {
            case 0:
                Debug.Log("Hole interacted on day 0. No scene change.");
                break;
            case 1:
                Debug.Log("Hole interacted on day 1. No scene change.");
                break;
            case 2:
                Debug.Log("Hole interacted on day 2. No scene change.");
                break;
            default:
                Debug.LogError($"Invalid day index: {day}. Cannot load scene.");
                break;
        }
        Instantiate(UiVideoCanva);
    }

}
