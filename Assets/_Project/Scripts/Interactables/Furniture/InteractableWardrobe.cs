using UnityEngine;

public class InteractableWardrobe : AbstractInteractable
{
    public override void InteractWithTask()
    {
        if (!taskManager.HasAnsweredThePhone)
        {
            Debug.Log("Player interacted with the clothes, but hasn't answered the phone yet.");
            ShowDialogue(task.answerThePhoneText, false);
            return;
        }

        PLayTaskSfx();
        MarkTaskAsComplete();
    }
}
