using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events; // Richiesto per l'utilizzo di List<T> nella randomizzazione

// Il nome della classe è rimasto invariato per preservare l'integrità del sistema di minigiochi
public class SlidingPuzzleManager : AbstractMinigame
{
    [Header("Board Settings")]
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 4;
    [SerializeField] private float spacing = 1f;
    [SerializeField] private float offset = 5f;

    // NUOVO: Campi di configurazione per impostare a priori la posizione del tassello vuoto
    [Header("Empty Tile Configuration")]
    [SerializeField] private int emptyTileRow = 3;
    [SerializeField] private int emptyTileColumn = 3;

    [Header("Prefabs")]
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Tile emptyTilePrefab;

    [Header("Puzzle Image")]
    [SerializeField] private Sprite[] tileSprites;

    private Tile[,] board;
    private Tile emptyTile;

    // MODIFICA: tassello selezionato al primo click, in attesa del secondo click su una tessera
    // adiacente con cui scambiarla. Serve per permettere lo scambio tra due tessere qualsiasi
    // (non solo con quella vuota), come richiesto per garantire la risolvibilità del rimescolamento casuale.
    private Tile selectedTile;

    private float fullWidth;
    private float fullHeight;

    public UnityEvent OnMiniGameComplete;

    public override void StartMiniGame()
    {
        base.StartMiniGame();

        TogglePlayerControl(false, false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // MODIFICA: Validazione degli indici configurati per evitare eccezioni IndexOutOfRangeException
        emptyTileRow = Mathf.Clamp(emptyTileRow, 0, rows - 1);
        emptyTileColumn = Mathf.Clamp(emptyTileColumn, 0, columns - 1);

        // MODIFICA: azzeramento di un'eventuale selezione residua da una precedente sessione del minigioco
        selectedTile = null;

        // Genera la scacchiera salvando lo stato iniziale corretto (homeRow e homeColumn)
        if (board == null)
            GenerateBoard();

        // NUOVO: Esecuzione del rimescolamento subito dopo aver garantito la presenza della board
        RandomizeGrid();
    }

    void GenerateBoard()
    {
        if (board != null)
        {
            foreach (Tile existingTile in board)
            {
                if (existingTile != null)
                    Destroy(existingTile.gameObject);
            }
        }

        board = new Tile[rows, columns];

        fullWidth = columns * spacing + (columns - 1) * offset;
        fullHeight = rows * spacing + (rows - 1) * offset;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                // MODIFICA: La cella vuota viene determinata in base ai parametri impostati a priori
                bool isEmptyCell = (row == emptyTileRow && column == emptyTileColumn);
                Tile tile;

                if (isEmptyCell)
                {
                    tile = Instantiate(emptyTilePrefab, transform);
                    tile.isEmpty = true;
                    emptyTile = tile; // Assegnazione immediata del riferimento globale
                }
                else
                {
                    tile = Instantiate(tilePrefab, transform);

                    int spriteIndex = row * columns + column;
                    if (tile.image != null && spriteIndex < tileSprites.Length)
                        tile.image.sprite = tileSprites[spriteIndex];
                }

                Vector2 position = new Vector2(
                    column * (spacing + offset) - fullWidth / 2f,
                    -row * (spacing + offset) + fullHeight / 2f
                );

                tile.GetComponent<RectTransform>().anchoredPosition = position;

                // Definizione della posizione logica iniziale coincidente con quella di completamento
                tile.row = row;
                tile.column = column;

                // SALVATAGGIO CONFIGURAZIONE ORIGINALE: questi valori rimangono immutati durante il gioco
                tile.homeRow = row;
                tile.homeColumn = column;

                tile.manager = this;

                board[row, column] = tile;
            }
        }
    }

    // NUOVO METODO: Gestisce il rimescolamento delle posizioni logiche e visive di ogni tassello
    void RandomizeGrid()
    {
        if (board == null) return;

        // Trasferimento temporaneo di tutti i tasselli in una lista lineare
        List<Tile> tilesList = new List<Tile>();
        foreach (Tile tile in board)
        {
            if (tile != null) tilesList.Add(tile);
        }

        // Generazione di un elenco contenente tutte le coordinate geometriche della griglia
        List<Vector2Int> availableCoords = new List<Vector2Int>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                availableCoords.Add(new Vector2Int(r, c));
            }
        }

        Tile[,] randomizedBoard = new Tile[rows, columns];

        // Assegnazione casuale delle coordinate disponibili a ciascun tassello
        foreach (Tile tile in tilesList)
        {
            int randomIndex = Random.Range(0, availableCoords.Count);
            Vector2Int assignedCoord = availableCoords[randomIndex];
            availableCoords.RemoveAt(randomIndex); // Rozione per evitare sovrapposizioni

            // Aggiornamento della posizione logica corrente del tassello
            tile.row = assignedCoord.x;
            tile.column = assignedCoord.y;

            // Inserimento del tassello all'interno delle nuove coordinate della matrice logica
            randomizedBoard[assignedCoord.x, assignedCoord.y] = tile;

            // Allineamento della componente RectTransform UI alla nuova posizione assegnata
            UpdateVisual(tile);
        }

        // Sostituzione della vecchia matrice di gioco con quella rimescolata
        board = randomizedBoard;

        // MODIFICA: la nota tecnica precedente segnalava che un rimescolamento puramente casuale poteva
        // generare configurazioni irrisolvibili, perché nel puzzle scorrevole classico si può muovere solo
        // il tassello adiacente al vuoto (mosse limitate alle sole permutazioni pari). Ora che TryMove/Swap
        // permettono lo scambio tra due tessere adiacenti qualsiasi (vedi sotto), le trasposizioni adiacenti
        // generano l'intero gruppo delle permutazioni: qualunque configurazione rimescolata è quindi risolvibile.
    }

    public void OnTileClicked(Tile tile)
    {
        TryMove(tile);
    }

    void TryMove(Tile tile)
    {
        // MODIFICA: sostituita la logica "un click sposta la tessera verso il vuoto" con una selezione
        // a due click, per permettere lo scambio tra due tessere adiacenti qualsiasi, non necessariamente
        // con quella vuota (altrimenti, come richiesto, il puzzle non è sempre risolvibile).
        if (selectedTile == null)
        {
            // Primo click: memorizza la tessera scelta, in attesa della seconda
            selectedTile = tile;
            return;
        }

        if (selectedTile != tile && IsAdjacent(selectedTile, tile))
        {
            Swap(selectedTile, tile);
        }

        // Sia in caso di scambio avvenuto, sia in caso di adiacenza non valida (o doppio click sulla
        // stessa tessera), la selezione si resetta per essere pronti alla prossima coppia di click
        selectedTile = null;
    }

    // MODIFICA: firma cambiata da IsAdjacent(Tile) a IsAdjacent(Tile, Tile) per confrontare due
    // tessere qualsiasi tra loro, invece di confrontare sempre una tessera con il solo emptyTile
    bool IsAdjacent(Tile a, Tile b)
    {
        int distance =
            Mathf.Abs(a.row - b.row)
            +
            Mathf.Abs(a.column - b.column);
        return distance == 1;
    }

    // MODIFICA: firma cambiata da Swap(Tile) a Swap(Tile, Tile) per scambiare le posizioni logiche
    // e visive di due tessere qualsiasi, non solo di una tessera con emptyTile
    void Swap(Tile a, Tile b)
    {
        int aRow = a.row;
        int aColumn = a.column;
        a.row = b.row;
        a.column = b.column;
        b.row = aRow;
        b.column = aColumn;
        board[a.row, a.column] = a;
        board[b.row, b.column] = b;
        UpdateVisual(a);
        UpdateVisual(b);

        // MODIFICA: Richiamo al nuovo metodo rinominato IsGridCompleted
        if (IsGridCompleted())
        {
            interactable.MarkTaskAsComplete();
            QuitMiniGame();
            interactable.SetHasBeenCompleted(true);
            OnMiniGameComplete?.Invoke();
            this.gameObject.SetActive(false);
        }
    }

    // MODIFICA: Rinominato da IsSolved a IsGridCompleted.
    // Esegue la verifica confrontando lo stato logico alterato dalla randomizzazione con quello Home.
    private bool IsGridCompleted()
    {
        foreach (Tile tile in board)
        {
            if (tile.row != tile.homeRow || tile.column != tile.homeColumn)
                return false;
        }
        return true;
    }

    void UpdateVisual(Tile tile)
    {
        Vector2 pos = new Vector2(tile.column * (spacing + offset) - fullWidth / 2f,
                                  -tile.row * (spacing + offset) + fullHeight / 2f);

        tile.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    public override void ResetMiniGame()
    {
        GenerateBoard();
        RandomizeGrid(); // Assicura che la griglia venga nuovamente randomizzata al reset

        // MODIFICA: azzeramento della selezione in corso, per evitare che un click residuo da prima
        // del reset generi uno scambio indesiderato sulla nuova board
        selectedTile = null;
    }

    public override void QuitMiniGame()
    {
        base.QuitMiniGame();

        //this.gameObject.SetActive(false);
    }

    public override void HandleMiniGameLogic()
    {
    }
}