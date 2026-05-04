# TO DO
## High priority
- Finire Gdd e Pitch.pdf
- Videos
- Day 01
- Volume e renderer 
- VFXs per il buco 
- forse dovremmo evitare che in day 01 la prima cosa che fai è sistemare i vestiti. magari facciamo compiere almeno 3 altri tasks prima.
- nuovo testo inizio video Blender:
"this tree is alive" (?)
- sito con Google site
- metti su artStation i shader che abbiamo fatto

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
- What is a number that a man may know it, and a man, that he may know a number?"
- No place to go
Nothing to come back to
Perhaps void is all I yield for. 
Tomorrow is all I have left
I pray it never comes. 
But it comes and it comes. 
Tomorrow always comes. 
Not for me
Not again
Let me rot. 
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
## Baking
 - Fase 1: ZBrush (Ottimizzazione e Creazione High/Low Poly)
Nota tecnica: ZBrush lavora in modo ottimale con mesh dense. Assicurati che il software di fotogrammetria (es. RealityCapture, Metashape) abbia già convertito la tua "nuvola di punti" in una mesh grezza ad altissima risoluzione con i colori fusi nei vertici (Vertex Colors o Polypaint in ZBrush) o con una texture applicata.
1. Importazione e Pulizia (High Poly)
Importa il file (generalmente .obj o .fbx) in ZBrush tramite il menu Tool > Import.
Attiva il Polypaint: Vai su Polypaint > Colorize per assicurarti di vedere i colori catturati dalla fotogrammetria.
Pulisci la mesh: Usa pennelli come Smooth, TrimCurve o SelectLasso per eliminare la geometria indesiderata (es. parti del terreno o artefatti del processo di scansione).
Chiudi i buchi: Se la scansione ha dei buchi, vai su Geometry > Modify Topology > Close Holes.
2. Creazione del Low Poly
Ora che hai il tuo "High Poly" pulito, devi creare la versione leggera per Unity. Hai due strade:
Metodo A (Decimation Master - Consigliato per oggetti inanimati/statici): 1. Vai su ZPlugin > Decimation Master.
2. Clicca su Pre-process Current.
3. Imposta la percentuale di poligoni desiderata (es. 5% o 10% a seconda del dettaglio necessario).
4. Clicca su Decimate Current. Otterrai una mesh leggera ma che mantiene i volumi generali.
Metodo B (ZRemesher - Consigliato se l'oggetto deve essere animato o deformarsi):
Duplica il tuo subtool High Poly.
Sul duplicato, vai su Geometry > ZRemesher e imposta il Target Polygon Count desiderato, poi clicca il pulsante ZRemesher.
3. Esportazione
Rinomina il subtool pesante in NomeOggetto_HP ed esportalo come .obj o .fbx (assicurati che esporti i Vertex Colors).
Rinomina il subtool leggero (decimato o remeshato) in NomeOggetto_LP ed esportalo.
Fase 2: Blender (UV Mapping e Baking)
Questo è il passaggio cruciale in cui trasferiamo i dettagli micro-geometrici e i colori fotografici dal modello pesante a quello leggero.
1. Preparazione e UV Unwrapping
Apri Blender e importa entrambi i file (NomeOggetto_HP e NomeOggetto_LP).
Nascondi momentaneamente l'HP.
Seleziona il modello LP, entra in Edit Mode (Tab), seleziona tutti i vertici (A) e procedi all'UV Unwrapping (U > Smart UV Project per oggetti inanimati complessi, oppure usa i Seams manuali per un controllo migliore). Questa è la "tela" su cui verranno dipinte le texture.
2. Setup per il Baking (Cycles)
Vai nel pannello Render Properties (l'icona della fotocamera a destra) e cambia il Render Engine da Eevee a Cycles.
Scorri in basso fino a trovare la sezione Bake.
Spunta la casella Selected to Active. Questo dice a Blender di prendere i dati dall'oggetto selezionato per primo (HP) e "cuocerli" sull'oggetto selezionato per ultimo (LP).
Apri una finestra Shader Editor. Seleziona il tuo LP e creagli un nuovo Materiale.
Nello Shader Editor, aggiungi un nodo Image Texture (Shift+A > Texture > Image Texture).
Clicca su New nel nodo, crea una texture (es. 2048x2048 o 4096x4096), chiamala Oggetto_Normal e imposta il Color Space su Non-Color. Mantieni questo nodo selezionato (evidenziato in bianco): è qui che Blender salverà il bake.
3. Baking della Normal Map (I dettagli geometrici)
Nel pannello Bake, imposta Bake Type su Normal.
Nella sezione Selected to Active, imposta un valore di Extrusion (es. 0.01m o 0.05m). Questo crea una gabbia virtuale. Se il bake presenta macchie nere o rosse, aumenta leggermente questo valore.
Nell'Outliner (la lista degli oggetti), seleziona PRIMA l'High Poly (NomeOggetto_HP), tieni premuto Ctrl, e seleziona POI il Low Poly (NomeOggetto_LP).
Clicca sul pulsante Bake. Una volta terminato, salva l'immagine generata dal pannello Image Editor (Alt+S).
4. Baking della Diffuse/Albedo Map (I colori della scansione)
Seleziona solo l'High Poly. Nello Shader Editor, assicurati che i suoi Vertex Colors siano collegati al Base Color del suo materiale (puoi usare il nodo Color Attribute).
Seleziona il Low Poly, crea un nuovo nodo Image Texture, chiamalo Oggetto_Albedo (lascia Color Space su sRGB) e tienilo selezionato.
Seleziona di nuovo HP, Ctrl + click su LP.
Nel pannello Bake, cambia Bake Type in Diffuse.
Importante: Sotto le opzioni del Diffuse Bake, togli le spunte a Direct e Indirect. Lascia spuntato SOLO Color.
Clicca su Bake e salva l'immagine.
Consiglio: Puoi ripetere questo processo cambiando il Bake Type per generare una Ambient Occlusion (AO) e una Roughness Map, molto utili per il PBR di Unity.
5. Esportazione per Unity
Elimina o disabilita l'High Poly.
Seleziona il tuo Low Poly e vai su File > Export > FBX.
Nelle impostazioni di esportazione FBX, spunta Limit to: Selected Objects.
Fase 3: Unity 6 (Importazione e Setup del Materiale)
Ora hai una mesh leggera e altamente performante, corredata dalle texture che la faranno apparire identica a quella fotogrammetrica da milioni di poligoni.
1. Importazione
Trascina il tuo file .fbx esportato da Blender e le texture generate (Oggetto_Albedo.png, Oggetto_Normal.png, ecc.) nella finestra Project di Unity 6.
Cruciale: Seleziona la tua Normal Map nella finestra Project. Nell'Inspector a destra, cambia il Texture Type in Normal map e clicca su Apply in basso. Se non lo fai, Unity applicherà la texture come se fosse un colore standard, generando artefatti di luce.
2. Setup del Materiale
Fai click destro nella finestra Project > Create > Material. Chiamalo Mat_OggettoScansionato.
A seconda della pipeline che stai usando in Unity 6 (URP, HDRP o Standard/Built-in), lo shader di default andrà benissimo (es. Universal Render Pipeline/Lit).
Trascina la tua texture Oggetto_Albedo nello slot Base Map (o Albedo).
Trascina la tua Oggetto_Normal nello slot Normal Map.
Se hai bakeato un'Ambient Occlusion, inseriscila nello slot Occlusion Map.
3. Assegnazione
Trascina il tuo modello FBX dalla finestra Project direttamente nella tua Scene o Hierarchy.
Trascina il materiale Mat_OggettoScansionato appena creato sopra il modello nella scena.
