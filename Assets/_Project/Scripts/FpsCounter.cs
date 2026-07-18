using TMPro;
using UnityEngine;

/// <summary>
/// Conta gli FPS a runtime e li mostra su un componente TextMeshProUGUI esposto nell'Inspector.
/// </summary>
public class FpsCounterToTextAsset : MonoBehaviour
{
    [Header("Riferimento esposto")]
    [Tooltip("Componente TextMeshProUGUI su cui mostrare gli FPS.")]
    public TextMeshProUGUI fpsText;

    [Header("Impostazioni")]
    [Tooltip("Ogni quanti secondi calcolare e aggiornare il valore di FPS mostrato.")]
    public float updateInterval = 0.5f;

    [Tooltip("Formato del testo mostrato. {0} viene sostituito con il valore numerico degli FPS.")]
    public string displayFormat = "FPS: {0:F0}";

    private float _accumulatedTime;
    private int _frameCount;

    private void Update()
    {
        _accumulatedTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedTime >= updateInterval)
        {
            float fps = _frameCount / _accumulatedTime;
            UpdateText(fps);

            _accumulatedTime = 0f;
            _frameCount = 0;
        }
    }

    private void UpdateText(float fps)
    {
        if (fpsText == null)
        {
            Debug.LogWarning("FpsCounterToTextAsset: nessun TextMeshProUGUI assegnato.");
            return;
        }

        fpsText.text = string.Format(displayFormat, fps);
    }
}