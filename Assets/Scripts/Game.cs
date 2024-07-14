using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class Game : MonoBehaviour
{
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private CellGrid grid;
    private bool gameover;
    private bool generated;

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
        StopAllCoroutines();

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        gameover = false;
        generated = false;

        grid = new CellGrid(width, height);
        board.Draw(grid);
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
            if (Input.GetMouseButtonDown(0)) {
                Reveal();
            } else if (Input.GetMouseButtonDown(1)) {
                Flag();
            } else if (Input.GetMouseButton(2)) {
                Chord();
            } else if (Input.GetMouseButtonUp(2)) {
                Unchord();
            }
        }
    }

    private void Reveal()
    {
        if (TryGetCellAtMousePosition(out Cell cell))
        {
            if (!generated)
            {
                grid.GenerateMines(cell, mineCount);
                grid.GenerateNumbers();
                generated = true;
            }

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
                CheckWinCondition();
                break;
        }

        board.Draw(grid);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (gameover) yield break;
        if (cell.revealed) yield break;
        if (cell.type == Cell.Type.Mine) yield break;

        cell.revealed = true;
        board.Draw(grid);

        yield return null;

        if (cell.type == Cell.Type.Empty)
        {
            if (grid.TryGetCell(cell.position.x - 1, cell.position.y, out Cell left)) {
                StartCoroutine(Flood(left));
            }
            if (grid.TryGetCell(cell.position.x + 1, cell.position.y, out Cell right)) {
                StartCoroutine(Flood(right));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y - 1, out Cell down)) {
                StartCoroutine(Flood(down));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y + 1, out Cell up)) {
                StartCoroutine(Flood(up));
            }
        }
    }

    private void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell)) return;
        if (cell.revealed) return;

        cell.flagged = !cell.flagged;
        board.Draw(grid);
    }

    private void Chord()
    {
        // unchord previous cells
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y].chorded = false;
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

                    if (grid.TryGetCell(x, y, out Cell cell)) {
                        cell.chorded = !cell.revealed && !cell.flagged;
                    }
                }
            }
        }

        board.Draw(grid);
    }

    private void Unchord()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

                if (cell.chorded) {
                    Unchord(cell);
                }
            }
        }

        board.Draw(grid);
    }

    private void Unchord(Cell chord)
    {
        chord.chorded = false;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0) {
                    continue;
                }

                int x = chord.position.x + adjacentX;
                int y = chord.position.y + adjacentY;

                if (grid.TryGetCell(x, y, out Cell cell))
                {
                    if (cell.revealed && cell.type == Cell.Type.Number)
                    {
                        if (grid.CountAdjacentFlags(cell) >= cell.number)
                        {
                            Reveal(chord);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void Explode(Cell cell)
    {
        gameover = true;

        // Set the mine as exploded
        cell.exploded = true;
        cell.revealed = true;

        // Reveal all other mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = grid[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.revealed = true;
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
                Cell cell = grid[x, y];

                // All non-mine cells must be revealed to have won
                if (cell.type != Cell.Type.Mine && !cell.revealed) {
                    return; // no win
                }
            }
        }

        gameover = true;

        // Flag all the mines
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];

                if (cell.type == Cell.Type.Mine) {
                    cell.flagged = true;
                }
            }
        }
    }

    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

}
