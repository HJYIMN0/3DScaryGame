# TO DO
## High priority
- Videos
- Concludere Day 01
- Nuovi Volume e renderer per altre giornate
- VFXs per il buco 
- forse dovremmo evitare che in day 01 la prima cosa che fai è sistemare i vestiti. magari facciamo compiere almeno 3 altri tasks prima.
- nuovo testo inizio video Blender:
"this tree is alive" (?)
- Forse potremmo farlo tipo racconto "non so perché bevevo il caffè... non è mai stata mia abitudine. " oppure indagine "sappiamo che bevve il caffè quel giorno"

## Scripting
- Save System
- Dividere InkManager in InkManager e InkUIManager
- ma invece di tutto il casino che abbiamo fatto per phone task dentro task manager perché non facciamo una nuova classe per il player tipo player task manager con un un bel bool has answered phone e semplicemente al task phone glielo mettiamo true

## 3D Modeling
- Nuova porta per l'ingresso.
- Shader blender pittorico
- Hyper realistic clock
- Hyper realistic phone
- potremmo riusare la medusa

## Art
- Main Menu 
- Decisioni sul buco
- tanti elementi surreali, estraneanti. una donna ricorrente, sporca, malata, dio di begotten.
- finto found footage
- Texture Dithering migliore
- diversi video in cui si ripete la stessa azione
- check come usare fotogrammetria su Unity (meaning, come si fa il bake su Unity6? lo faccio su blender e poi esporto?)
- Voglio una casa più grande!

## Writing
- Finire Gdd
- Organizzare un Day01
- la narrazione non è lineare. ogni tanto sei nel passato, ogni tanto nel presente. 
- ogni tanto parli con qualcuno. cioè appare del testo come se stessi avendo una conversazione con qualcuno. magari nelle giornate da "normale" è quando esci per andare a lavoro. comunque inizialmente si lascia intendere che tu stia parlando con qualcuno. Verso la fine, la rivelazione "con chi pensi di stare parlando"? potremmo aggiungere dei finti found footage come interruzione tra un momento in cui hai visto il buco e subito dopo sei solo accanto. task: vai a letto. 

## Video
- Per il video iniziale:
- - togliere gli allarmi? lasciamo solo il telefono?
- - allungherei un po' la parte "are we destined to be alone?".
- - gli fx di disturbo sono troppo presenti? Abbassiamone l'opacità 
- Finire di dipingere le tele piccole e riprendere di sfondarle con un coltello
- scavare buche sul terreno, in spiaggia...
- Distruggere il telefono scansionato con fotogrammetria con il martello della campagna 

## Ideas
- Not playing manager : controlla se Vector3 del player è 0 da abbastanza tempo e fa partire qualche strana cosa. "non fare una scelta è già di per sé una scelta".
- a un certo punto entri in un nuovo corridoio della casa. è stretto e lungo.. Non c'era mai stato un corrdoio lì.
- Magari interagire con il computer?
- Non rispondere per troppo tempo al telefono comporta qualcosa.

# Gemini
## Volumetric Fog
Realizzare una nebbia volumetrica che copra il pavimento di un'intera casa in Unity 6 richiede precisione. Non basta creare un cubo; dobbiamo assicurarci che lo shader "legga" la profondità della scena per non disegnare nebbia sopra i mobili o attraverso i muri.

Ecco la guida definitiva, passo dopo passo.

---

### Fase 1: Preparazione di Unity 6 (URP)

Prima di tutto, dobbiamo assicurarci che Unity sia configurato per permettere allo shader di vedere gli oggetti (Depth Texture).

1. Vai nella cartella **Settings** del tuo progetto.
2. Seleziona l'asset **Universal Render Pipeline Asset** (quello con l'icona tonda blu).
3. Nell'Inspector, sotto la voce **General**, assicuratevi che la casella **Depth Texture** sia spuntata.
4. Sotto **Quality**, assicurati che **HDR** sia attivo per una migliore resa luminosa.

---

### Fase 2: Creazione dello Shader "FloorFog"

Dobbiamo scrivere un codice specifico che calcoli la nebbia solo "dentro" il volume del cubo, rispettando il pavimento.

1. Nella finestra *Project*, fai tasto destro -> **Create -> Shader -> Universal Render Pipeline -> HLSL Shader** (o semplicemente un file di testo rinominato `.shader`).
2. Chiamalo `FloorFogShader`.
3. Incolla questo codice (ottimizzato per performance su grandi aree):

```hlsl
Shader "Custom/FloorFogAAA"
{
    Properties
    {
        _FogColor("Color", Color) = (0.5, 0.6, 0.7, 1)
        _Density("Density", Range(0, 5)) = 1.0
        _HeightFalloff("Height Falloff", Range(0, 10)) = 2.0
        _NoiseScale("Noise Scale", Float) = 0.5
        _StepSize("Step Size", Range(0.1, 2.0)) = 0.5
        _MaxSteps("Max Steps", Int) = 32
        _NoiseTex3D("3D Noise", 3D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        
        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front // Importante: renderizziamo l'interno del cubo

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float4 screenPos : TEXCOORD0; };

            float _Density, _HeightFalloff, _NoiseScale, _StepSize;
            int _MaxSteps;
            float4 _FogColor;
            TEXTURE3D(_NoiseTex3D); SAMPLER(sampler_NoiseTex3D);

            Varyings vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target {
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(uv);
                float3 sceneWorldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                
                float3 rayStart = _WorldSpaceCameraPos;
                float3 rayDir = normalize(sceneWorldPos - rayStart);
                float sceneDist = distance(rayStart, sceneWorldPos);
                
                // Iniziamo il raggio o dalla camera (se dentro) o dalla faccia del cubo
                float currentDist = 0;
                float transmittance = 1.0;
                float3 finalColor = 0;

                for (int i = 0; i < _MaxSteps; i++) {
                    if (currentDist >= sceneDist || transmittance < 0.01) break;

                    float3 p = rayStart + rayDir * currentDist;
                    
                    // Controlliamo se siamo vicini al pavimento (Height Falloff)
                    float heightFactor = exp(-(p.y - (unity_ObjectToWorld._m13 - 0.5)) * _HeightFalloff);
                    float noise = SAMPLE_TEXTURE3D(_NoiseTex3D, sampler_NoiseTex3D, p * _NoiseScale).r;
                    float d = noise * _Density * heightFactor;

                    if (d > 0.01) {
                        float stepTransmittance = exp(-d * _StepSize);
                        finalColor += _FogColor.rgb * d * transmittance * _StepSize;
                        transmittance *= stepTransmittance;
                    }
                    currentDist += _StepSize;
                }

                return float4(finalColor, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }
}

```

---

### Fase 3: Setup del Volume nella Hierarchy

Ora creiamo l'oggetto fisico nella tua casa.

1. **Crea il Cubo:** Tasto destro nella Hierarchy -> **3D Object -> Cube**. Chiamalo `GlobalFogVolume`.
2. **Posizionamento:**
* **Scale:** Imposta la scala X e Z in modo da coprire tutta la pianta della casa (es. `X: 50, Z: 50`).
* **Scale Y:** Imposta l'altezza massima della nebbia. Se vuoi che arrivi alle ginocchia, metti `Scale Y: 1`.
* **Position:** Posiziona il cubo in modo che la sua metà inferiore sia "affondata" nel pavimento.


3. **Materiale:**
* Crea un nuovo Materiale (`M_FloorFog`).
* Cambia lo Shader in cima a `Custom/FloorFogAAA`.
* Assegna una texture di Noise 3D (fondamentale, senza questa non vedrai nulla).
* Trascina il materiale sul cubo.



---

### Fase 4: I Settings "Minuziosi" per il realismo

Per rendere l'effetto AAA, regola questi parametri nell'Inspector del Materiale:

* **Step Size:** Impostalo a `0.8` o `1.0`. Più è basso, più la nebbia è definita ma pesa sulla GPU. Visto che copre tutta la casa, non scendere troppo.
* **Max Steps:** Tienilo tra `16` e `32`. Se vedi dei "gradini" nella nebbia, aumenta leggermente.
* **Height Falloff:** Questo è il parametro magico. Portalo a `3.0` o superiore per fare in modo che la nebbia sia densa a terra e "svanisca" verso l'alto invece di avere un taglio netto in cima al cubo.
* **Noise Scale:** Regola quanto sono grandi le "nuvole" di vapore sul pavimento. Prova `0.2`.

### Cosa abbiamo ottenuto?

Abbiamo creato un **Volume Intelligente**. Grazie al codice `SampleSceneDepth`, la nebbia saprà esattamente dove sono le gambe dei tavoli, i muri e le scale. Non "entrerà" dentro i mobili solidi, ma vi girerà intorno, creando un'atmosfera da film horror o thriller di alta qualità.

**Ultimo consiglio Pro:** Se la casa ha più piani, non usare un unico cubo gigante, ma crea un `GlobalFogVolume` per ogni piano, così potrai gestire l'altezza della nebbia stanza per stanza.

# Proprietà intellettuali
## 3D Noise
https://github.com/SebLague/Clouds