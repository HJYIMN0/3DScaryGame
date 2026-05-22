// Esempio: busta sul tavolo con due finali basati su una variabile
VAR chosen_ending = 0

Hai trovato una busta sul tavolo. Cosa fai?
+ [Aprila]
    -> open_envelope
+ [Ignorala]
    -> ignore_envelope

=== open_envelope ===
~ chosen_ending = 1
Dentro c'è una lettera che cambia tutto.
Hai saltato una riga
-> END

=== ignore_envelope ===
~ chosen_ending = 2
Decidi di non immischiarti. Forse era meglio così.
Hai saltato una riga
-> END