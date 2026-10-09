using System;
using System.Collections; // [MODIFICA] Necessario per IEnumerator, usato dalla coroutine di rotazione pingpong
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.Arm;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int row;
    public int column;

    [HideInInspector] public int homeRow;
    [HideInInspector] public int homeColumn;


    public bool isEmpty;
    [HideInInspector] public SlidingPuzzleManager manager;
    [HideInInspector] public Image image;

    // [MODIFICA] Scala originale del prefab, salvata in Awake. Serve per applicare i moltiplicatori
    // (selectedScale / highlightScale) sempre rispetto alla scala di partenza, e per ripristinarla in ResetVisual.
    private Vector3 baseScale;

    // [MODIFICA] Riferimento alla coroutine di rotazione in corso (null se la tile non è selezionata).
    // Mi serve per poterla fermare con StopCoroutine e per evitare di avviarne due contemporaneamente.
    private Coroutine selectedRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();

        // [MODIFICA] Memorizzo la scala iniziale una sola volta, qui, per non dover chiamare nulla di costoso dopo
        baseScale = transform.localScale;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTileClicked(this);
    }

    /// <summary>
    /// Scala la tile di scaleMultiplier e avvia la rotazione Z in pingpong tra minRotationZ e maxRotationZ
    /// alla velocità rotationSpeed (gradi al secondo). Tutti i valori arrivano dai campi esposti nel manager.</summary>
    /// <param name="scaleMultiplier"></param>
    /// <param name="rotationSpeed"></param>
    /// <param name="minRotationZ"></param>
    /// <param name="maxRotationZ"></param>
    public void StartSelectedAnimation(float scaleMultiplier, float rotationSpeed, float minRotationZ, float maxRotationZ)
    {
        // Se per qualche motivo c'era già una rotazione attiva la fermo, per non avere due coroutine
        // che scrivono la stessa rotazione contemporaneamente
        if (selectedRoutine != null)
            StopCoroutine(selectedRoutine);

        transform.localScale = baseScale * scaleMultiplier;

        // Avvio della coroutine che gestisce solo la rotazione, conservando il riferimento per fermarla dopo
        selectedRoutine = StartCoroutine(SelectedRotationRoutine(rotationSpeed, minRotationZ, maxRotationZ));
    }

    /// <summary>
    /// Applica solo la scala di evidenziazione, senza avviare la rotazione. Utile per distinguere le tile evidenziate da quella selezionata.
    /// </summary>
    /// <param name="scaleMultiplier"></param>
    public void SetHighlight(float scaleMultiplier)
    {
        transform.localScale = baseScale * scaleMultiplier;
    }
    /// <summary>
    /// l'eventuale animazione. Chiamato dal manager quando la selezione termina o il minigioco viene (ri)aperto.
    /// </summary>
    public void ResetVisual()
    {
        // Ferma la coroutine solo se esiste, poi azzera il riferimento
        if (selectedRoutine != null)
        {
            StopCoroutine(selectedRoutine);
            selectedRoutine = null;
        }

        transform.localScale = baseScale;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Rotazione pingpong sull'asse Z. Parte da 0 gradi verso il massimo, rimbalza
    /// sui due limiti e continua finché non viene fermata da ResetVisual.Ho scritto il pingpong a mano
    /// (angolo + direzione) invece di usare Mathf.PingPong perché così la tile parte da 0 senza scatti iniziali.
    /// Non alloca memoria nel ciclo: Quaternion.Euler e i float sono tipi valore.
    /// </summary>
    /// <param name="rotationSpeed"></param>
    /// <param name="minRotationZ"></param>
    /// <param name="maxRotationZ"></param>
    /// <returns></returns>

    private IEnumerator SelectedRotationRoutine(float rotationSpeed, float minRotationZ, float maxRotationZ)
    {
        float angle = 0f;       // Angolo Z corrente in gradi, parte dalla rotazione neutra
        float direction = 1f;   // +1 = verso il massimo, -1 = verso il minimo

        while (true)
        {
            angle += direction * rotationSpeed * Time.deltaTime;

            // [MODIFICA] Al raggiungimento di un limite blocco l'angolo al limite e inverto la direzione (pingpong)
            if (angle >= maxRotationZ)
            {
                angle = maxRotationZ;
                direction = -1f;
            }
            else if (angle <= minRotationZ)
            {
                angle = minRotationZ;
                direction = 1f;
            }

            // [MODIFICA] Applica la rotazione solo sull'asse Z (l'unico che ha senso per un elemento UI 2D)
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null; // [MODIFICA] Aspetta il frame successivo
        }
    }
}