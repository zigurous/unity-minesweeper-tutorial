using System.Collections;
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
        state = new Cell[width, height];
        gameover = false;

        GenerateCells();
        GenerateMines();
        GenerateNumbers();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        board.Draw(state);
    }

    private void GenerateCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                state[x, y] = new Cell
                {
                    position = new Vector3Int(x, y, 0),
                    type = Cell.Type.Empty
                };
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
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            NewGame();
            return;
        }

        if (!gameover)
        {
            if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                Reveal();
            } else if (Input.GetMouseButton(2)) {
                Chord();
            } else if (Input.GetMouseButtonUp(2)) {
                Unchord();
            }
        }
    }

    private void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell)) return;
        if (cell.revealed) return;

        cell.flagged = !cell.flagged;
        state[cell.position.x, cell.position.y] = cell;
        board.Draw(state);
    }

    private void Reveal()
    {
        if (TryGetCellAtMousePosition(out Cell cell)) {
            Reveal(cell);
        }
    }

    private void Reveal(Cell cell)
    {
        if (cell.revealed) return;
        if (cell.flagged) return;

        switch (cell.type)
        {
            case Cell.Type.Mine:
                Explode(cell);
                break;

            case Cell.Type.Empty:
                StartCoroutine(Flood(cell));
                CheckWinCondition();
                break;

            default:
                cell.revealed = true;
                state[cell.position.x, cell.position.y] = cell;
                CheckWinCondition();
                break;
        }

        board.Draw(state);
    }

    private IEnumerator Flood(Cell cell)
    {
        // Recursive exit conditions
        if (cell.revealed) yield break;
        if (cell.type == Cell.Type.Mine || cell.type == Cell.Type.Invalid) yield break;

        // Reveal the cell
        cell.revealed = true;
        state[cell.position.x, cell.position.y] = cell;

        // Wait before continuing the flood
        board.Draw(state);
        yield return null;

        // Keep flooding if the cell is empty, otherwise stop at numbers
        if (cell.type == Cell.Type.Empty)
        {
            StartCoroutine(Flood(GetCell(cell.position.x - 1, cell.position.y)));
            StartCoroutine(Flood(GetCell(cell.position.x + 1, cell.position.y)));
            StartCoroutine(Flood(GetCell(cell.position.x, cell.position.y - 1)));
            StartCoroutine(Flood(GetCell(cell.position.x, cell.position.y + 1)));
        }
    }

    private void Chord()
    {
        // unchord previous cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];
                cell.chorded = false;
                state[x, y] = cell;
            }
        }

        // chord new cells
        if (TryGetCellAtMousePosition(out Cell chord))
        {
            for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
            {
                for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
                {
                    int x = chord.position.x + adjacentX;
                    int y = chord.position.y + adjacentY;

                    if (TryGetCell(x, y, out Cell cell))
                    {
                        cell.chorded = !cell.revealed && !cell.flagged;
                        state[x, y] = cell;
                    }
                }
            }
        }

        board.Draw(state);
    }

    private void Unchord()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                if (cell.chorded) {
                    Unchord(cell);
                }
            }
        }

        board.Draw(state);
    }

    private void Unchord(Cell chord)
    {
        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                int x = chord.position.x + adjacentX;
                int y = chord.position.y + adjacentY;

                if (TryGetCell(x, y, out Cell adjacent))
                {
                    if (adjacent.revealed && adjacent.type == Cell.Type.Number)
                    {
                        if (GetAdjacentFlagAmount(adjacent) >= adjacent.number)
                        {
                            chord.chorded = false;
                            state[chord.position.x, chord.position.y] = chord;
                            Reveal(chord);
                            return;
                        }
                    }
                }
            }
        }

        chord.chorded = false;
        state[chord.position.x, chord.position.y] = chord;
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

    private void CheckWinCondition()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = state[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed) {
                    return; // no win
                }
            }
        }

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

    private bool IsValid(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private Cell GetCell(int x, int y)
    {
        if (IsValid(x, y)) {
            return state[x, y];
        } else {
            return default;
        }
    }

    private Cell GetCellAtMousePosition()
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return GetCell(cellPosition.x, cellPosition.y);
    }

    private bool TryGetCell(int x, int y, out Cell cell)
    {
        cell = GetCell(x, y);
        return cell.type != Cell.Type.Invalid;
    }

    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        cell = GetCellAtMousePosition();
        return cell.type != Cell.Type.Invalid;
    }

    private int GetAdjacentFlagAmount(Cell cell)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                if (TryGetCell(x, y, out Cell adjacent) && !adjacent.revealed && adjacent.flagged) {
                    count++;
                }
            }
        }

        return count;
    }

}
