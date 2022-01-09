using UnityEngine;
using UnityEngine.Tilemaps;

public class Minesweeper : MonoBehaviour
{
    public const int MINE = -1;
    public const int EMPTY = 0;

    private Tilemap tilemap;

    public Tile[] numberTiles;
    public Tile tileUp;
    public Tile tileDown;
    public Tile tileMine;
    public Tile tileFlag;

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private int[,] state;

    private void OnValidate()
    {
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        tilemap = GetComponent<Tilemap>();
    }

    private void Start()
    {
        NewGame();
    }

    private void NewGame()
    {
        state = new int[width, height];

        GenerateMines();
        GenerateTiles();
        PrecomputeState();
    }

    private void GenerateMines()
    {
        for (int i = 0; i < mineCount; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (state[x, y] == MINE)
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

            state[x, y] = MINE;
        }
    }

    private void GenerateTiles()
    {
        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int cell = new Vector3Int(i, j, 0);
                tilemap.SetTile(cell, tileUp);
            }
        }
    }

    private void PrecomputeState()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (state[i, j] == MINE) {
                    continue;
                }

                Vector3Int cell = new Vector3Int(i, j, 0);
                int mineCount = GetMineCount(cell);

                if (mineCount == 0) {
                    state[i, j] = EMPTY;
                } else {
                    state[i, j] = mineCount;
                }
            }
        }
    }

    private int GetMineCount(Vector3Int cell)
    {
        int count = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) {
                    continue;
                }

                int x = cell.x + i;
                int y = cell.y + j;

                if (x < 0 || x >= width || y < 0 || y >= height) {
                    continue;
                }

                if (state[x, y] == MINE) {
                    count++;
                }
            }
        }

        return count;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(mousePosition);

            if (tilemap.GetTile(cell) != tileFlag) {
                Process(cell);
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(mousePosition);
            Flag(cell);
        }

        if (Input.GetKeyDown(KeyCode.N)) {
            NewGame();
        }
    }

    private void Process(Vector3Int cell)
    {
        if (!tilemap.HasTile(cell)) {
            return;
        }

        if (state[cell.x, cell.y] == MINE)
        {
            // TODO: lose menu
            tilemap.SetTile(cell, tileMine);
            return;
        }

        if (state[cell.x, cell.y] == EMPTY) {
            FloodFill(cell);
        } else {
            tilemap.SetTile(cell, numberTiles[state[cell.x, cell.y]]);
        }

        if (HasWon()) {
            // TODO: win menu
        }
    }

    private void FloodFill(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) return;
        if (state[cell.x, cell.y] == MINE) return;
        if (tilemap.GetTile(cell) == tileDown) return;

        int mineCount = state[cell.x, cell.y];
        tilemap.SetTile(cell, numberTiles[mineCount]);

        if (mineCount == EMPTY)
        {
            FloodFill(cell + Vector3Int.up);
            FloodFill(cell + Vector3Int.down);
            FloodFill(cell + Vector3Int.left);
            FloodFill(cell + Vector3Int.right);
        }
    }

    private void Flag(Vector3Int cell)
    {
        TileBase currentTile = tilemap.GetTile(cell);

        if (currentTile == tileFlag) {
            tilemap.SetTile(cell, tileUp);
        } else if (currentTile == tileUp) {
            tilemap.SetTile(cell, tileFlag);
        }
    }

    private bool HasWon()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int cell = new Vector3Int(i, j, 0);
                TileBase tile = tilemap.GetTile(cell);

                if ((tile == tileUp || tile == tileFlag) && state[i, j] != MINE) {
                    return false;
                }
            }
        }

        return true;
    }

}
