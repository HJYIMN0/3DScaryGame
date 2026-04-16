# TO DO
## Scripting
- Script interazione con con buco: Cambiare posizione camera
- Fadeble estratto da DayManager. Facciamo un prefab con un onEnable? Oppure un singleton, perché ci serve che resti durante il caricamento
- Save System


## 3D Modeling
- Nuova porta per l'ingresso.
- Spostare Pivot porta per farla aprire correttamente.

## Art
- Serve una vera texture per il dithering. Facciamola Quadrata, ma bella grande. Per esempio 4096 * 4096 - ne ho fatta una per fullscreen a 1920*1080
- Main Menu 
- Decisioni sul buco
- Camera and Volume for aesthetic 
- Il dithering applicato alla camera non funziona con le linee. Dobbiamo per forza usare i punti
- Anche con i punti, il dithering applicato alla camera fa proprio cacare. Rivediamo lo shader in modo che sia implementato nel singolo Mtl.
- tanti elementi surreali, estraneanti. una donna ricorrente, sporca, malata, dio di begotten.
- finto found footage


## Writing
- Finire Gdd
- Organizzare un Day00
- - N.b: Probabilmente il Day00, lo riafaremo per avere un inizio ben strutturato alla fine del primo prototipo, ma intanto, così abbiamo una base per preparare un first playable
- la narrazione non è lineare. ogni tanto sei nel passato, ogni tanto nel presente. 
- ogni tanto parli con qualcuno. cioè appare del testo come se stessi avendo una conversazione con qualcuno. magari nelle giornate da "normale" è quando esci per andare a lavoro. comunque inizialmente si lascia intendere che tu stia parlando con qualcuno. Verso la fine, la rivelazione "con chi pensi di stare parlando"? potremmo aggiungere dei finti found footage come interruzione tra un momento in cui hai visto il buco e subito dopo sei solo accanto. task: vai a letto. 


# Gemini
- 04Chromatic Aberration (anche in B/W!).Le linee nere si sdoppiano. La realtà sembra vibrare.
- 05Lens Distortion (effetto barile).La stanza sembra curvarsi verso il buco al centro.5.
-  Consigli da Senior Programmer: Come implementarloIn Unity, non applicare questi effetti direttamente sulla camera.Crea un oggetto Global Volume nella scena.Crea un Volume Profile (es. Day01_Style).Aggiungi gli override: Color Adjustments (Saturazione -100), Vignette, Film Grain.


