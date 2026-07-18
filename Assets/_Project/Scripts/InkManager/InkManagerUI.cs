using Ink.Runtime;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InkManagerUI : MonoBehaviour
{
    [Header("Canva Settings")]
    [SerializeField] private GameObject canvaPrefab;

    [Header("Choice Settings")]
    [SerializeField] private GameObject choiceButtonPrefab;

    private Transform choiceContainerLayout;
    private TextMeshProUGUI canvaPrefabText;
    private GameObject canvaInstance;
    private PlayerInputController playerInputController;

    // MODIFICATO: le scelte sono ora stato interno della classe.
    // ShowChoices() non prende più List<Choice> come parametro esterno — la lista
    // viene salvata qui e usata da Select*/Confirm senza che il chiamante debba
    // mantenerla vivo.
    private List<Choice> _choices = new List<Choice>();
    private Action<int> _onChoiceSelected;

    // MODIFICATO: indice della scelta attualmente evidenziata nella UI.
    private int _selectedIndex = -1;

    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    public bool IsDialogueOpen { get; private set; } = false;
    public bool HasActiveChoices => _activeChoiceButtons.Count > 0;

    private void Awake()
    {
        playerInputController = GetComponent<PlayerInputController>();
    }

    public void SetText(string text)
    {
        if (canvaPrefabText != null)
            canvaPrefabText.text = text;
    }

    public void ShowChoices(List<Choice> choices, Action<int> onChoiceSelected)
    {
        // MODIFICA:
        // prima di mostrare nuove scelte assicuriamoci che
        // qualsiasi stato precedente sia stato eliminato.

        HideChoices();

        // MODIFICA:
        // memorizziamo internamente la lista corrente
        // e il callback da eseguire alla conferma.

        _choices = choices;
        _onChoiceSelected = onChoiceSelected;

        // MODIFICA:
        // se esiste almeno una scelta, selezioniamo
        // automaticamente la prima.

        _selectedIndex = choices.Count > 0 ? 0 : -1;

        playerInputController.CameraController.StopLook();

        for (int i = 0; i < choices.Count; i++)
        {
            int capturedIndex = i;

            GameObject btn =
                Instantiate(choiceButtonPrefab,
                            choiceContainerLayout);

            btn.GetComponentInChildren<TextMeshProUGUI>().text =
                choices[i].text;

            Button button =
                btn.GetComponent<Button>();

            // MODIFICA:
            // disabilitiamo completamente
            // la navigation automatica di Unity.

            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            button.onClick.AddListener(() =>
                SelectAndConfirm(capturedIndex));

            _activeChoiceButtons.Add(btn);
        }

        UpdateSelectionHighlight();

        Debug.Log($"ShowChoices() : {choices.Count} choices");
    }

    // AGGIUNTO: helper privato — aggiorna l'aspetto visivo dei bottoni in base a _selectedIndex.
    // Cambia colore/alpha del bottone selezionato vs gli altri.
    // Se non hai un sistema di highlight, puoi personalizzare questa logica.
    private void UpdateSelectionHighlight()
    {
        if (_selectedIndex < 0)
            return;

        if (_selectedIndex >= _activeChoiceButtons.Count)
            return;

        Button btn =
            _activeChoiceButtons[_selectedIndex]
            .GetComponent<Button>();

        if (btn == null)
            return;

        EventSystem.current
            .SetSelectedGameObject(btn.gameObject);

        Debug.Log(
            $"Selected choice {_selectedIndex}");
    }

    // AGGIUNTO: helper privato che combina selezione e conferma per i click sui bottoni.
    private void SelectAndConfirm(int index)
    {
        _selectedIndex = index;
        ConfirmSelectedChoice();
    }

    public void SelectPreviousChoice()
    {
        if (!HasActiveChoices || _choices.Count == 0) return;

        // MODIFICATO: Clamp invece di modulo — non si va oltre il primo elemento.
        _selectedIndex = Mathf.Clamp(_selectedIndex - 1, 0, _choices.Count - 1);
        UpdateSelectionHighlight();
    }

    public void SelectNextChoice()
    {
        if (!HasActiveChoices || _choices.Count == 0) return;

        // MODIFICATO: Clamp invece di modulo — non si va oltre l'ultimo elemento.
        _selectedIndex = Mathf.Clamp(_selectedIndex + 1, 0, _choices.Count - 1);
        UpdateSelectionHighlight();
    }

    public void ConfirmSelectedChoice()
    {
        if (!HasActiveChoices)
            return;

        if (_selectedIndex < 0)
            return;

        if (_selectedIndex >= _choices.Count)
            return;

        if (_onChoiceSelected == null)
            return;

        Debug.Log(
            $"Confirming choice {_selectedIndex}");

        // MODIFICA:
        // invochiamo solamente il callback.

        // NON puliamo lo stato qui.

        // Infatti SelectChoice()
        // potrebbe richiamare immediatamente
        // ContinueDialogue()
        // che potrebbe creare nuove scelte.

        _onChoiceSelected.Invoke(_selectedIndex);
    }
    public void HideChoices()
    {
        foreach (var btn in _activeChoiceButtons)
            Destroy(btn);
        _activeChoiceButtons.Clear();

        // NON cancellare la callback qui
        _choices.Clear();
        _selectedIndex = -1;

        playerInputController?.CameraController?.StartLook();
    }

    public void ToggleCanva()
    {
        if (canvaInstance == null || !canvaInstance.activeSelf)
            InitializeCanva();
        else
            CloseCanva();
    }

    public void CloseCanva()
    {
        Debug.Log("[InkManagerUI] Closing Canva...");
        if (canvaInstance != null && canvaInstance.activeSelf && canvaPrefabText != null)
        {
            HideChoices();

            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;

            canvaInstance.SetActive(false);
            IsDialogueOpen = false;
        }
        else
        {
            Debug.LogWarning("Attempted to close canva, but it was either null or already inactive.");
        }
    }

    private void InitializeCanva()
    {
        Debug.Log("[InkManagerUI] Initializing Canva...");
        if (canvaInstance == null)
        {
            canvaInstance = Instantiate(canvaPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            canvaInstance.SetActive(true);
        }

        if (choiceContainerLayout == null)
        {
            choiceContainerLayout = canvaInstance.GetComponentInChildren<HorizontalLayoutGroup>().transform;
            if (choiceContainerLayout == null)
                Debug.LogError("[InkManagerUI] HorizontalLayoutGroup non trovato nel canvas prefab.");
        }

        if (canvaPrefabText == null)
        {
            canvaPrefabText = canvaInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (canvaPrefabText == null)
                Debug.LogError($"[InkManagerUI] TextMeshProUGUI non trovato in {canvaPrefab.name}");
        }

        IsDialogueOpen = true;
    }
}