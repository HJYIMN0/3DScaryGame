using System.Collections;
using UnityEngine;

public class InteractableClothes : AbstractInteractable
{

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private GameObject holeGameObject;
    [SerializeField] private GameObject fadeCanvaPrefab;

    private GameObject fadeInstance;
    protected override void Start()
    {
        base.Start();
        holeGameObject.SetActive(false);
    }

    public override void ExecuteInteraction()
    {
        if (HasBeenCompleted) return;

        taskManager.CompleteTask(task.TaskName);
        fadeInstance = Instantiate(fadeCanvaPrefab, Vector3.zero, Quaternion.identity);
        fadeInstance.GetComponent<Fader>().StartCoroutine(fadeInstance.GetComponent<Fader>().FadeIn(fadeDuration));
        StartCoroutine(WaitAndFadeOut());
    }
    public IEnumerator WaitAndFadeOut()
    {
        while (!fadeInstance.GetComponent<Fader>().HasFadedIn)
        {
            yield return null;
        }
        Destroy(this.gameObject);
        fadeInstance.GetComponent<Fader>().StartCoroutine(fadeInstance.GetComponent<Fader>().FadeOut(fadeDuration, fadeDuration));
        holeGameObject.SetActive(true);
        TaskManager.Instance.AddTask(holeGameObject.GetComponent<AbstractInteractable>().TaskSO);

    }

}
