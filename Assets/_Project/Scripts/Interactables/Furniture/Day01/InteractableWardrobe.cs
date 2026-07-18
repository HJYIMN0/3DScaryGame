using UnityEngine;

public class InteractableWardrobe : AbstractInteractable
{
    public override void ExecuteInteraction()
    {
        PLayTaskSfx();
        MarkTaskAsComplete();
    }
}
