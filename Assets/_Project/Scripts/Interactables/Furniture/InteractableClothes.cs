using System.Collections;
using UnityEngine;

public class InteractableClothes : AbstractInteractable
{

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private GameObject holeGameObject;
    TaskManager taskManager;
    Fader fader;
    protected override void Start()
    {
        base.Start();
        taskManager = TaskManager.Instance;
        fader = Fader.Instance;

        holeGameObject.SetActive(false);
    }

    public override void InteractWithTask()
    {
        if (HasBeenInteractedWith) return;

        taskManager.CompleteTask(task.TaskName);
        fader.StartCoroutine(fader.FadeIn(fadeDuration));
        StartCoroutine(WaitForFadeOut());
    }

    public IEnumerator WaitForFadeOut()
    {
        while (fader.CanvasGroupAlpha < 0.9f)
        {
            yield return null;
        }
        Destroy(this.gameObject);
        fader.StartCoroutine(fader.FadeOut(fadeDuration, fadeDuration));
        holeGameObject.SetActive(true);
        TaskManager.Instance.AddTask(holeGameObject.GetComponent<AbstractInteractable>().TaskSO);

    }

}
