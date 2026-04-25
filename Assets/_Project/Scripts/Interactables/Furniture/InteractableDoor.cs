using System.Collections;
using UnityEngine;

public class InteractableDoor : AbstractInteractable
{
    public override void InteractWithTask()
    {
        GoToWork();
    }

    private void GoToWork() 
    {
        GameFlowManager.Instance.LoadScene(GameFlowManager.Instance.CurrentDay + 1);
    }

}
