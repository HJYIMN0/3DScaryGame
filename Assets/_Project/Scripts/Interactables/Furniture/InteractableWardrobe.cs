using UnityEngine;

public class InteractableWardrobe : AbstractInteractable
{
    public override void InteractWithTask()
    {        
        PLayTaskSfx();
        MarkTaskAsComplete();
    }
}
