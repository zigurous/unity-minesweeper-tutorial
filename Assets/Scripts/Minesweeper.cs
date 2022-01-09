using UnityEngine;
using UnityEngine.Tilemaps;

public class Minesweeper : MonoBehaviour
{
    private Tilemap tilemap;

    public Tile[] numberTiles;
    public Tile tileUp;
    public Tile tileDown;
    public Tile tileMine;

    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    public bool debug;

    private bool[,] mines;

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
        GenerateMines();
        GenerateTiles();
    }

    private void GenerateMines()
    {
        mines = new bool[width, height];

        for (int i = 0; i < mineCount; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            while (mines[x, y])
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

            mines[x, y] = true;
        }
    }

    private void GenerateTiles()
    {
        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int position = new Vector3Int(i, j, 0);
                tilemap.SetTile(position, tileUp);
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(mousePosition);
            Process(cell);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            NewGame();

            if (debug) {
                Debug();
            }
        }
    }

    private void Debug()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int cell = new Vector3Int(i, j, 0);
                Process(cell);
            }
        }
    }

    private void Process(Vector3Int cell)
    {
        if (!tilemap.HasTile(cell)) {
            return;
        }

        if (IsMine(cell))
        {
            // TODO: game over
            tilemap.SetTile(cell, tileMine);
            return;
        }

        int mineCount = GetMineCount(cell);

        if (mineCount == 0) {
            FloodFill(cell);
        } else {
            tilemap.SetTile(cell, numberTiles[mineCount]);
        }
    }

    private void FloodFill(Vector3Int cell)
    {
        if (!IsFillable(cell)) {
            return;
        }

        tilemap.SetTile(cell, tileDown);

        FloodFill(cell + Vector3Int.up);
        FloodFill(cell + Vector3Int.down);
        FloodFill(cell + Vector3Int.left);
        FloodFill(cell + Vector3Int.right);
    }

    private bool IsFillable(Vector3Int cell)
    {
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height) {
            return false;
        }

        if (tilemap.GetTile(cell) == tileDown) {
            return false;
        }

        if (GetMineCount(cell) != 0) {
            return false;
        }

        return true;
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

                if (IsMine(new Vector3Int(x, y, 0))) {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsMine(Vector3Int cell)
    {
        return mines[cell.x, cell.y];
    }

}
