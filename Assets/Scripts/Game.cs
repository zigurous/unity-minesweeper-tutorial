using UnityEngine;

public class Game : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private Cell[,] state;
    private bool gameover;

    private void OnValidate()
    {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;

        board = GetComponentInChildren<Board>();
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        state = new Cell[width, height];
        gameover = false;

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        board.Draw(state);
    }

    private void GenerateCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = new Cell();
                cell.position = new Vector3Int(x, y, 0);
                cell.type = Cell.Type.Empty;
                state[x, y] = cell;
            }
        }
    }

    private void GenerateMines()
    {
        for (int i = 0; i < mineCount; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (state[x, y].type == Cell.Type.Mine)
            {
                x++;

                if (x >= width)
                {
                    x = 0;
                    y++;

                    if (y >= height) {
                        y = 0;
                    }
                }
            }

            state[x, y].type = Cell.Type.Mine;
        }
    }

    private void GenerateNumbers()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine) {
                    continue;
                }

                cell.number = CountMines(x, y);

                if (cell.number > 0) {
                    cell.type = Cell.Type.Number;
                }

                state[x, y] = cell;
            }
        }
    }

    private int CountMines(int cellX, int cellY)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = cellX + adjacentX;
                int y = cellY + adjacentY;

                if (GetCell(x, y).type == Cell.Type.Mine) {
                    count++;
                }
            }
        }

        return count;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) {
            NewGame();
        }
        else if (!gameover)
        {
            if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                Reveal();
            }
        }
    }

    private void Flag()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);

        // Only flag if not revealed
        if (GetCell(cellPosition.x, cellPosition.y, out Cell cell) && !cell.revealed)
        {
            cell.flagged = !cell.flagged;
            state[cell.position.x, cell.position.y] = cell;
            board.Draw(state);
        }
    }

    private void Reveal()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);

        // Only reveal if not already revealed and not flagged
        if (GetCell(cellPosition.x, cellPosition.y, out Cell cell) && !cell.revealed && !cell.flagged)
        {
            if (cell.type == Cell.Type.Mine) {
                Explode(cell);
            } else {
                Reveal(cell);
            }

            board.Draw(state);
        }
    }

    private void Reveal(Cell cell)
    {
        if (cell.type == Cell.Type.Empty) {
            Flood(cell);
        }

        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        if (HasWon()) {
            Win();
        }
    }

    private void Flood(Cell cell)
    {
        // Recursive exit conditions
        if (cell.revealed) return;
        if (cell.type == Cell.Type.Mine) return;
        if (!IsValid(cell.position.x, cell.position.y)) return;

        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        // Keep flooding if the cell is empty, stop at a number
        if (cell.type == Cell.Type.Empty)
        {
            Flood(GetCell(cell.position.x - 1, cell.position.y));
            Flood(GetCell(cell.position.x + 1, cell.position.y));
            Flood(GetCell(cell.position.x, cell.position.y - 1));
            Flood(GetCell(cell.position.x, cell.position.y + 1));
        }
    }

    private void Explode(Cell cell)
    {
        Debug.Log("Game Over!");
        gameover = true;

        // Set the mine as exploded
        cell.exploded = true;
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        // Reveal all other mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = state[x, y];

                if (cell.type == Cell.Type.Mine)
                {
                    cell.revealed = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private void Win()
    {
        Debug.Log("Winner!");
        gameover = true;

        // Flag all the mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                    state[x, y] = cell;
                }
            }
        }
    }

    private bool HasWon()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed) {
                    return false;
                }
            }
        }

        return true;
    }

    private Cell GetCell(int x, int y)
    {
        Cell cell;
        GetCell(x, y, out cell);
        return cell;
    }

    private bool GetCell(int x, int y, out Cell cell)
    {
        if (IsValid(x, y))
        {
            cell = state[x, y];
            return true;
        }
        else
        {
            cell = new Cell();
            cell.position = new Vector3Int(x, y, 0);
            return false;
        }
    }

    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

}
