using TMPro;
using Ink.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InkManagerUI : MonoBehaviour
{
    [Header("Canva Settings")]
    [SerializeField] private GameObject canvaPrefab;

    [Header("Choice Settings")]
    [SerializeField] private GameObject choiceButtonPrefab;
    // RIMOSSO: choicesManagerPrefab — non veniva usato nel codice

    private Transform choiceContainerLayout;
    private TextMeshProUGUI canvaPrefabText;
    private GameObject canvaInstance;
    private PlayerMovementController playerMovementController;

    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    public bool IsDialogueOpen { get; private set; } = false;
    public bool HasActiveChoices => _activeChoiceButtons.Count > 0;

    private void Awake()
    {
        playerMovementController = GetComponent<PlayerMovementController>();
    }

    public void SetText(string text)
    {
        if (canvaPrefabText != null)
            canvaPrefabText.text = text;
    }

    public void ShowChoices(List<Choice> choices, Action<int> onChoiceSelected)
    {
        HideChoices();

        // AGGIUNTO: mostra il cursore quando ci sono scelte da compiere
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerMovementController.StopLook();

        for (int i = 0; i < choices.Count; i++)
        {
            int capturedIndex = i;
            GameObject btn = Instantiate(choiceButtonPrefab, choiceContainerLayout);
            btn.GetComponentInChildren<TextMeshProUGUI>().text = choices[i].text;
            btn.GetComponent<Button>().onClick.AddListener(() => onChoiceSelected(capturedIndex));
            _activeChoiceButtons.Add(btn);
        }
    }

    public void HideChoices()
    {
        foreach (var btn in _activeChoiceButtons)
            Destroy(btn);
        _activeChoiceButtons.Clear();

        playerMovementController.StartLook();
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

            // AGGIUNTO: ripristina il lock del cursore alla chiusura del dialogo
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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