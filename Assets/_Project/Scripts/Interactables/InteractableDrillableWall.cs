using UnityEngine;

public class InteractableDrillableWall : AbstractInteractable
{
    public override void ExecuteInteraction()
    {   
        Debug.Log("Drillable wall interaction executed!");
        StartMiniGame();
    }
}
