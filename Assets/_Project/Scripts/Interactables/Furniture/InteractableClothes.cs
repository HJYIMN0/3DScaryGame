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
        taskManager.CompleteTask(task.TaskName);
        fader.StartCoroutine(fader.FadeIn(fadeDuration));
        StartCoroutine(WaitForFadeOut());
        Destroy(this.gameObject);
    }

    public IEnumerator WaitForFadeOut()
    {
        while (fader.CanvasGroupAlpha < 1f)
        {
            yield return null;
        }
        fader.StartCoroutine(fader.FadeOut(fadeDuration));
        holeGameObject.SetActive(true);

    }

}
