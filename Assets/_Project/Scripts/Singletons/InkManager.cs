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
    public bool IsStoryActive => currentStory != null;

    private void Update()
    {
        Debug.Log($"current text asset = {currentTextAsset?.name}");
        Debug.Log($", current story = {(currentStory != null ? "active" : "null")}");
    }
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
            // FIX: assicurati che il nome corrisponda esattamente a quello nel file Ink
            currentStory.variablesState[dayVariableNameInInk] = GameFlowManager.Instance.CurrentDay;

            // FIX: salta esplicitamente al knot corrispondente al giorno corrente.
            // Senza questo, la storia non ha contenuto radice da cui partire e termina subito.
            string targetKnot = $"Day{GameFlowManager.Instance.CurrentDay}";
            currentStory.ChoosePathString(targetKnot);
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

    public void ClearStoryAndTextAsset()
    {
        currentStory = null;
        currentTextAsset = null;
    }
}