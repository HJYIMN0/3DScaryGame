using UnityEngine;

[CreateAssetMenu(fileName = "New Task", menuName = "Task")]
public class TaskSO : ScriptableObject
{
    public string TaskName;
    public string TaskDescription;
    public bool isMandatory;

    public float interactionRadius = 1f;

    public GameObject canvaObj;
}
