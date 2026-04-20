using UnityEngine;

public class Interactable_Hole : AbstractInteractable
{
    [SerializeField] private GameObject UiVideoCanva;
    public override void InteractWithTask()
    {
        Instantiate(UiVideoCanva);
        TaskManager.Instance.CompleteTask(task.TaskName);
    }

}
