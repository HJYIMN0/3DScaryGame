using Ink.Runtime;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class InkManager : MonoBehaviour
{
    [Header("Ink Settings")]
    [SerializeField] private string dayVariableNameInInk = "Day";

    private TextAsset currentTextAsset;
    private Story currentStory;

    private InkManagerUI _inkManagerUI;
    private PlayerDialogueController _playerDialogueController;
    private bool _canPlayerMove = false;

    public bool IsStoryActive => currentStory != null;
    public bool IsDialogueOpen => _inkManagerUI != null && _inkManagerUI.IsDialogueOpen;

    public Action<TextAsset> onDialogueEnd;

    private void Awake()
    {
        _inkManagerUI = GetComponent<InkManagerUI>();
        _playerDialogueController = GetComponent<PlayerDialogueController>();

        if (_inkManagerUI == null)
            Debug.LogError("[InkManager] InkManagerUI component not found on this GameObject.");
        if (_playerDialogueController == null)
            Debug.LogError("[InkManager] PlayerDialogueController component not found on this GameObject.");
    }

    private void Start()
    {
        Debug.Log($"InkManager = {dayVariableNameInInk}{GameFlowManager.Instance.CurrentDay}");
    }

    // MODIFICATO: tutte le StartDialogue accettano ora canPlayerMove
    public void StartDialogue(TextAsset inkJson, bool usesVariables, bool canPlayerMove = false)
        => PrepareStory(inkJson, usesVariables, canPlayerMove);

    public void StartDialogue(TextAsset inkJson, bool usesVariables, int differentDay, bool canPlayerMove = false)
        => PrepareStory(inkJson, usesVariables, differentDay, canPlayerMove);

    public void StartDialogue(string text, bool canPlayerMove = false)
        => PrepareStory(text, canPlayerMove);

    private void SetTextAsset(TextAsset textAsset)
    {
        if (textAsset != null && textAsset != currentTextAsset)
            currentTextAsset = textAsset;
    }

    private void SetStory(TextAsset textAsset)
    {
        if (textAsset != null)
            currentStory = new Story(textAsset.text);
    }

    // MODIFICATO: accetta canPlayerMove e lo salva prima di chiamare ToggleSystem
    private void PrepareStory(TextAsset textAsset, bool usesVariables, bool canPlayerMove)
    {
        if (textAsset == null) return;

        _canPlayerMove = canPlayerMove; // AGGIUNTO: salva prima del toggle

        SetTextAsset(textAsset);
        SetStory(textAsset);
        ToggleSystem();

        if (usesVariables)
        {
            currentStory.variablesState[dayVariableNameInInk] = GameFlowManager.Instance.CurrentDay;
            string targetKnot = $"{dayVariableNameInInk}{GameFlowManager.Instance.CurrentDay}";
            currentStory.ChoosePathString(targetKnot);
        }

        ContinueDialogue();
    }

    // MODIFICATO: accetta canPlayerMove e lo salva prima di chiamare ToggleSystem
    private void PrepareStory(TextAsset textAsset, bool usesVariables, int differentDay, bool canPlayerMove)
    {
        if (textAsset == null) return;

        _canPlayerMove = canPlayerMove; // AGGIUNTO: salva prima del toggle

        SetTextAsset(textAsset);
        SetStory(textAsset);
        ToggleSystem();

        if (usesVariables)
        {
            currentStory.variablesState[dayVariableNameInInk] = differentDay;
            string targetKnot = $"{dayVariableNameInInk}{differentDay}";
            currentStory.ChoosePathString(targetKnot);
        }

        ContinueDialogue();
    }

    // MODIFICATO: accetta canPlayerMove e lo salva prima di chiamare ToggleSystem
    private void PrepareStory(string text, bool canPlayerMove)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.Log("PrepareStory called with empty text. Closing dialogue.");
            _inkManagerUI?.CloseCanva();
            EndDialogue();
            ClearStoryAndTextAsset();
            return;
        }

        _canPlayerMove = canPlayerMove; // AGGIUNTO: salva prima del toggle

        ToggleSystem();
        _inkManagerUI?.SetText(text);
    }

    public void ContinueDialogue()
    {
        Debug.Log("Continue Dialogue has been called.");

        // AGGIUNTO: se ci sono scelte attive, il player deve scegliere prima di proseguire
        if (currentStory != null && currentStory.currentChoices.Count > 0)
        {
            Debug.Log("Waiting for player choice. ContinueDialogue blocked.");
            return;
        }

        if (currentStory != null && currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();
            _inkManagerUI?.SetText(nextLine);

            // AGGIUNTO: dopo aver letto la riga, controlla se ci sono scelte da mostrare.
            // Se sì, le passa alla UI con il callback SelectChoice; non procede da solo.
            if (currentStory.currentChoices.Count > 0)
            {
                _inkManagerUI?.ShowChoices(currentStory.currentChoices, SelectChoice);
            }
        }
        else
        {
            Debug.Log("No more lines to continue or story is null.");
            EndDialogue();
        }
    }

    // AGGIUNTO: chiamato da InkManagerUI quando il player preme un bottone di scelta.
    // Registra la scelta in Ink, nasconde i bottoni e continua il flusso normalmente.
    public void SelectChoice(int index)
    {
        currentStory.ChooseChoiceIndex(index);
        _inkManagerUI?.HideChoices();
        ContinueDialogue();
    }

    public void EndDialogue()
    {
        ToggleSystem();
        onDialogueEnd?.Invoke(currentTextAsset);
        currentStory = null;
    }

    private void ToggleSystem()
    {
        // MODIFICATO: il PlayerDialogueController viene disabilitato solo se canPlayerMove è false.
        // Così il player può continuare a muoversi durante dialoghi puramente visivi.
        if (!_canPlayerMove && _playerDialogueController != null)
            _playerDialogueController.enabled = !_playerDialogueController.enabled;

        _inkManagerUI?.ToggleCanva();
    }

    public void ClearStoryAndTextAsset()
    {
        currentStory = null;
        currentTextAsset = null;
    }
}