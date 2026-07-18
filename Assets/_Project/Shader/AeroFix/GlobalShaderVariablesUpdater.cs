using UnityEngine;

// L'attributo [ExecuteAlways] garantisce l'esecuzione dello script sia in Play Mode 
// che all'interno dell'Editor (Scene View). È un requisito tecnico fondamentale per 
// visualizzare correttamente il rendering del raymarching (e la stabilità del noise) 
// senza dover avviare il gioco.
[ExecuteAlways]
public class GlobalShaderVariablesUpdater : MonoBehaviour
{
    // Memorizzazione dell'hash della stringa. 
    // Ricavare l'ID intero tramite Shader.PropertyToID una singola volta è una 
    // pratica di ottimizzazione standard. Evita l'allocazione di memoria e il parsing 
    // di stringhe ad ogni iterazione del ciclo Update, riducendo l'overhead sulla CPU.
    private static readonly int FrameCountId = Shader.PropertyToID("_FrameCount");

    private void Update()
    {
        // Shader.SetGlobalInt inietta il valore a livello globale nella pipeline di rendering.
        // Qualsiasi materiale o shader attualmente in uso che dichiari "int _FrameCount;" 
        // riceverà automaticamente l'indice del fotogramma corrente (Time.frameCount).
        Shader.SetGlobalInt(FrameCountId, Time.frameCount);
    }
}