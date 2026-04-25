# TO DO
## High priority
- Dobbiamo scrivere per forza cosa fa questo il primo giorno. sennò fa troppo cagare. Ci serve qualche strumento narrativo
- buona idea! potremmo fare una voce in segreteria che ti dice cosa fare. è la stessa voce che poi "con chi credi di star parlando?"
- Video
- Day 01
- Fix Fader
- Volume e renderer 

## Scripting
- Fader potrebbe essere semplicemente un prefab che si infila dentro il transform del padre e poi si distrugge. tipo Fader = new Fader { float fadeInTime, ... }. 
così se tanto è nel singleton non si dovrebbe distruggere (fai attenzione che sia un singleton don't destroy on load)
- Save System


## 3D Modeling
- Nuova porta per l'ingresso.
- Shader blender pittorico
- Hyper realistic clock

## Art
- Main Menu 
- Decisioni sul buco
- Camera and Volume for aesthetic 
- tanti elementi surreali, estraneanti. una donna ricorrente, sporca, malata, dio di begotten.
- finto found footage
- Texture Dithering migliore
- diversi video in cui si ripete la stessa azione

## Writing
- Finire Gdd
- Organizzare un Day01
- la narrazione non è lineare. ogni tanto sei nel passato, ogni tanto nel presente. 
- ogni tanto parli con qualcuno. cioè appare del testo come se stessi avendo una conversazione con qualcuno. magari nelle giornate da "normale" è quando esci per andare a lavoro. comunque inizialmente si lascia intendere che tu stia parlando con qualcuno. Verso la fine, la rivelazione "con chi pensi di stare parlando"? potremmo aggiungere dei finti found footage come interruzione tra un momento in cui hai visto il buco e subito dopo sei solo accanto. task: vai a letto. 


# Gemini
- 04Chromatic Aberration (anche in B/W!).Le linee nere si sdoppiano. La realtà sembra vibrare.
- 05Lens Distortion (effetto barile).La stanza sembra curvarsi verso il buco al centro.5.
-  Consigli da Senior Programmer: Come implementarloIn Unity, non applicare questi effetti direttamente sulla camera.Crea un oggetto Global Volume nella scena.Crea un Volume Profile (es. Day01_Style).Aggiungi gli override: Color Adjustments (Saturazione -100), Vignette, Film Grain.


