using System.Collections;
using UnityEngine;

public class DialogueCoroutineManager
    : MonoBehaviour
{
    [SerializeField] private TextAsset[] nextDialogueAssets;
    [SerializeField] private float waitTimeBeforeFirstDialogue = 2f;
    [SerializeField] private float waitTimeAfterDialogueEnd = 1f;

    [SerializeField] private StartingDialogueManager startingDialogueManager;
    [SerializeField] private InkManager inkManager;

    private void Start()
    {
        if (startingDialogueManager != null && inkManager != null && startingDialogueManager.InkDialogue != null)
        {
            inkManager.onDialogueEnd += OnStartingDialogueEnd;
        }
        else if (startingDialogueManager.InkDialogue == null)
        {
            Debug.Log("Starting dialogue!");
            StartCoroutine(PlayAllDialogues());
        }
    }

    private void OnStartingDialogueEnd(TextAsset dialogue)
    {
        if (dialogue != null && dialogue == startingDialogueManager.InkDialogueAsset)
        {
            StopAllCoroutines();
            StartCoroutine(PlayAllDialogues());
        }
    }

    private IEnumerator PlayAllDialogues()
    {
        // Attesa prima del primo dialogo della catena
        yield return new WaitForSeconds(waitTimeBeforeFirstDialogue);

        // Itera su tutti i dialogue assets dell'array
        for (int i = 0; i < nextDialogueAssets.Length; i++)
        {
            if (nextDialogueAssets[i] == null)
            {
                Debug.LogWarning($"nextDialogueAssets[{i}] is null, skipping.");
                continue;
            }

            inkManager.StartDialogue(nextDialogueAssets[i], false, true);

            // Attende che il dialogo corrente termini
            yield return new WaitUntil(() => !inkManager.IsStoryActive);

            // Attesa dopo la fine del dialogo (tranne eventualmente l'ultimo, se vuoi)
            yield return new WaitForSeconds(waitTimeAfterDialogueEnd);
        }

        GameFlowManager.Instance.LoadNextDay(GameFlowManager.Instance.FadeDuration);
        Debug.Log("All dialogues completed.");
    }
}