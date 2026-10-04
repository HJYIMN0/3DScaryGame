using Ink.Runtime;
using System;
using UnityEngine;

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
        // Ramo "testo vuoto": INVARIATO
        if (string.IsNullOrEmpty(text))
        {
            _inkManagerUI?.CloseCanva();
            EndDialogue();
            ClearStoryAndTextAsset();
            return;
        }

        _canPlayerMove = canPlayerMove; // AGGIUNTO: salva prima del toggle (INVARIATO)

        // [MODIFICA] Prima: ToggleSystem(); veniva chiamato sempre.
        // ToggleSystem() INVERTE lo stato (ON <-> OFF). Se il dialogo è già aperto e arriva un'altra
        // StartDialogue(string), per esempio il rilancio fatto da
        // PlayerInteractionController.OnCurrentTaskDialogueEnded(), il toggle spegneva il sistema
        // invece di lasciarlo acceso.
        // Ora lo chiamiamo solo se il dialogo NON è già aperto; altrimenti aggiorniamo solo il testo.
        if (!IsDialogueOpen)
            ToggleSystem();

        _inkManagerUI?.SetText(text); // INVARIATO
    }
    public void ContinueDialogue()
    {

        if (currentStory == null)
        {
            EndDialogue();
            return;
        }

        // Se ci sono scelte attive, il player deve scegliere prima di proseguire.
        if (currentStory.currentChoices.Count > 0)
        {
            _inkManagerUI.ShowChoices(currentStory.currentChoices, SelectChoice);
            return;
        }

        if (currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();

            // Salta righe vuote consecutive (fine knot senza testo, marker, ecc.).
            // Fermati appena trovi una riga valida o una scelta.
            while (string.IsNullOrWhiteSpace(nextLine)
                   && currentStory.currentChoices.Count == 0
                   && currentStory.canContinue)
            {
                nextLine = currentStory.Continue();
            }

            // Se dopo i salti la riga è ancora vuota e non ci sono scelte,
            // la storia è terminata: chiudi subito senza mostrare il box vuoto.
            if (string.IsNullOrWhiteSpace(nextLine) && currentStory.currentChoices.Count == 0)
            {
                EndDialogue();
                return;
            }

            _inkManagerUI?.SetText(nextLine);

            // Dopo aver settato il testo, se ci sono scelte mostrale.
            if (currentStory.currentChoices.Count > 0)
            {
                _inkManagerUI?.ShowChoices(currentStory.currentChoices, SelectChoice);
            }
        }
        else
        {
            EndDialogue();
        }
    }

    public void SelectChoice(int index)
    {
        currentStory.ChooseChoiceIndex(index);

        _inkManagerUI.HideChoices();

        ContinueDialogue();
    }

    public void JumpToKnot(string knot)
    {
        if (currentStory == null)
        {
            Debug.LogWarning("[InkManager] JumpToKnot called but currentStory is null.");
            return;
        }
        currentStory.ChoosePathString(knot);
        ContinueDialogue();
    }

    public bool HasKnot(string knot)
    {
        if (currentStory == null) return false;
        return currentStory.KnotContainerWithName(knot) != null;
    }

    public void EndDialogue()
    {
        currentStory = null;
        ToggleSystem();
        onDialogueEnd?.Invoke(currentTextAsset);
    }

    private void ToggleSystem()
    {
        if (_playerDialogueController != null)
        {
            bool activate = !_playerDialogueController.enabled;

            if (activate)
            {
                // IMPORTANTE: PRIMA di abilitare il componente, altrimenti
                // OnEnable() ferma il movimento con il valore di default (false).
                _playerDialogueController.SetCanMoveDuringDialogue(_canPlayerMove);
            }

            _playerDialogueController.enabled = activate;
        }

        _inkManagerUI?.ToggleCanva();
    }

    public void ClearStoryAndTextAsset()
    {
        currentStory = null;
        currentTextAsset = null;
    }
}