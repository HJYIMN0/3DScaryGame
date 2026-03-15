using Ink.Runtime;
using TMPro;
using UnityEngine;

public class InkManager : GenericSingleton<InkManager>
{
    public override bool IsDestroyedOnLoad() => false;
    public override bool ShouldDetatchFromParent() => true;

    [Header("Canva Settings")] // Nota tecnica: si scrive "Canvas", non "Canva", ma mantengo la tua nomenclatura.
    [SerializeField] private GameObject canvaPrefab;

    [Header("Ink settings")]
    [SerializeField] private string dayVariableNameInInk = "DAY";

    private TextMeshProUGUI canvaPrefabText;
    private GameObject canvaInstance;

    private TextAsset currentTextAsset;
    private Story currentStory;

    private GameObject _player;

    // Aggiunta una proprietà pubblica per far sapere all'esterno se una storia è attualmente in corso.
    public bool IsStoryActive => currentStory != null;

    public void StartDialogue(TextAsset inkJson, bool usesVariables) => PrepareStory(inkJson, usesVariables);

    private void SetTextAsset(TextAsset textAsset)
    {
        if (textAsset != null && textAsset != currentTextAsset)
        {
            currentTextAsset = textAsset;
        }
    }

    private void SetStory(TextAsset textAsset)
    {
        if (textAsset != null)
        {
            currentStory = new Story(textAsset.text);
        }
    }

    private void PrepareStory(TextAsset textAsset, bool usesVariables)
    {
        if (textAsset == null) return;

        SetTextAsset(textAsset);
        SetStory(textAsset);
        ToggleSystem();

        if (usesVariables)
        {
            currentStory.variablesState[dayVariableNameInInk] = GameFlowManager.Instance.CurrentDay;
        }

        ContinueDialogue();
    }

    public void ContinueDialogue()
    {
        Debug.Log("Continue Dialogue has been called.");
        if (currentStory != null && currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();
            Debug.Log($"Next line: {nextLine}");
            if (canvaPrefabText != null)
            {
                canvaPrefabText.text = nextLine;
            }
        }
        else
        {
            Debug.Log("No more lines to continue or story is null.");
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        ToggleSystem();
        currentStory = null;
    }

    private void Start()
    {
        // Rimosso InitializeCanva() e l'impostazione del testo qui.
        // Evita l'autodistruzione della logica prima ancora di interagire.
        // Mi limito ad assicurarmi che il sistema parta chiuso.
        if (canvaInstance != null)
        {
            canvaInstance.SetActive(false);
        }
    }

    private void InitializeCanva()
    {
        if (canvaInstance == null)
        {
            canvaInstance = Instantiate(canvaPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            canvaInstance.SetActive(true);
        }

        if (canvaPrefabText == null)
        {
            canvaPrefabText = canvaInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (canvaPrefabText == null)
            {
                Debug.LogError($"InkManager couldn't find TextMeshProUGUI in {canvaPrefab.name}");
            }
        }

        // RIMOSSO ContinueDialogue() da qui. 
        // Generava un salto di riga involontario poiché veniva richiamato sia qui che in PrepareStory.
    }

    private void CloseCanva()
    {
        if (canvaInstance != null && canvaInstance.activeSelf && canvaPrefabText != null)
        {
            canvaInstance.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Attempted to close canva, but it was either null or already inactive.");
        }
    }

    private void ToggleSystem()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null)
            {
                Debug.LogError("InkManager couldn't find player in the scene.");
                return;
            }
        }

        _player.GetComponent<PlayerDialogueController>().enabled = !_player.GetComponent<PlayerDialogueController>().enabled;

        if (canvaInstance == null || !canvaInstance.activeSelf)
        {
            InitializeCanva();
        }
        else
        {
            CloseCanva();
        }
    }
}