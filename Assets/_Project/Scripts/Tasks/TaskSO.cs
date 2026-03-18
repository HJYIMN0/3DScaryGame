using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "New Task", menuName = "Task")]
public class TaskSO : ScriptableObject
{
    [Header("Task Attributes")]
    public string TaskName;
    public string TaskDescription;
    public bool isMandatory;

    [Header("Properties")]
    public GameObject canvaObj;

    [Header("Ink Attributes")]
    public bool isInkTask = false;
    public TextAsset inkJson;
    public bool usesVariablesInInk = false;
}
