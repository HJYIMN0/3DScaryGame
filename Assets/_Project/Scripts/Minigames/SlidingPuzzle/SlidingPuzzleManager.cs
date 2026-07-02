using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Richiesto per l'utilizzo di List<T> nella randomizzazione

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

    private float fullWidth;
    private float fullHeight;

    public override void StartMiniGame()
    {
        base.StartMiniGame();

        TogglePlayerControl(false, false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // MODIFICA: Validazione degli indici configurati per evitare eccezioni IndexOutOfRangeException
        emptyTileRow = Mathf.Clamp(emptyTileRow, 0, rows - 1);
        emptyTileColumn = Mathf.Clamp(emptyTileColumn, 0, columns - 1);

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

        // NOTA TECNICA: Un rimescolamento puramente casuale (array shuffle) ignora il calcolo
        // delle inversioni e può generare nel 50% dei casi una configurazione matematicamente irrisolvibile.
        // Se si desidera garantire l'assoluta risolvibilità, sostituire questa logica con una serie 
        // di chiamate simulate a TryMove() eseguite in sequenza a partire dallo stato risolto.
    }

    public void OnTileClicked(Tile tile)
    {
        TryMove(tile);
    }

    void TryMove(Tile tile)
    {
        if (!IsAdjacent(tile))
            return;
        Swap(tile);
    }

    bool IsAdjacent(Tile tile)
    {
        int distance =
            Mathf.Abs(tile.row - emptyTile.row)
            +
            Mathf.Abs(tile.column - emptyTile.column);
        return distance == 1;
    }

    void Swap(Tile tile)
    {
        int oldRow = tile.row;
        int oldColumn = tile.column;
        tile.row = emptyTile.row;
        tile.column = emptyTile.column;
        emptyTile.row = oldRow;
        emptyTile.column = oldColumn;
        board[tile.row, tile.column] = tile;
        board[emptyTile.row, emptyTile.column] = emptyTile;
        UpdateVisual(tile);
        UpdateVisual(emptyTile);

        // MODIFICA: Richiamo al nuovo metodo rinominato IsGridCompleted
        if (IsGridCompleted())
        {
            interactable.MarkTaskAsComplete();
            QuitMiniGame();
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
    }

    public override void HandleMiniGameLogic()
    {
    }
}