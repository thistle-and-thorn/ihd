using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GridStage : MonoBehaviour
{
    static readonly bool TILE_UPDATE_ENABLED = true;
    static readonly float TILE_UPDATE_TIMEOUT_DURATION = 1.3f;

    public System.Random rnd = new System.Random();

    // Menus
    public GameObject game;
    public GameObject mainMenu;

    // GameObjects
    public GameObject tile;
    public GameObject villager;
    public GameObject gameOver;
    public GameObject waveLabel;
    public GameObject win; //TODO: Remove this, we're not using it.

    // Audio
    public AudioSource menuAudio;
    public AudioSource gameAudio;

    public Tile[,] grid = new Tile[12, 12];
    int NUMBER_OF_VILLAGERS = 5;

    float seed;
    Tile lastClickedTile;
    Coroutine updateRoutine;

    int waveCount = 1;
    public List<Tile> villageTiles = new List<Tile>();
    List<Villager> villagers = new List<Villager>();
    List<Tile> grassTiles = new List<Tile>();
    int _prevGrassCount = 0;
    int _prevVillageCount = 0;
    bool FireIsSpreading
    {
        get { return _prevGrassCount > grassTiles.Count || _prevVillageCount > villageTiles.Count; }
    }

    int AliveVillagerCount()
    {
        return villagers.Where(t => t.alive).Count();
    }

    // Use this for initialization
    void Start()
    {
        MainMenuVisible(true);
        gameAudio.mute = true;
        menuAudio.Play();
        gameAudio.Play();
    }

    void MainMenuVisible(bool visible)
    {
        mainMenu.SetActive(visible);
        game.SetActive(!visible);
    }

    public void StartGame()
    {
        MainMenuVisible(false);
        RestartGame(true);
        gameAudio.mute = false;
    }

    public void ShowMainMenu()
    {
        gameAudio.mute = true;
        MainMenuVisible(true);
    }

    public void RestartGame(bool cleanup)
    {
        if (cleanup)
        {
            RenderWaveCount(1);
            lastClickedTile = null;
            if (updateRoutine != null) { StopCoroutine(updateRoutine); }
            if (villageTiles.Count != 0) { villageTiles = new List<Tile>(); }
            GameOverVisible(false);
        }

        // Start new game
        GenerateGrid();

        if (ValidGame())
        {
            if (cleanup)
            {
                GenerateVillagers();
            }

            // Start tile updates
            if (TILE_UPDATE_ENABLED)
            {
                updateRoutine = StartCoroutine(UpdateTiles());
            }
        }
        else
        {
            // Restart until we find a village
            RestartGame(cleanup);
        }
    }

    void RenderWaveCount(int count)
    {
        var comp = waveLabel.GetComponent<Text>();
        comp.text = string.Format("{0}", count);
    }

    void NextWave()
    {
        RenderWaveCount(++waveCount);

        var deadVillagers = villagers.Where(v => !v.alive).ToArray();
        for (var vIdx = 0; vIdx < deadVillagers.Length; vIdx++)
        {
            var v = deadVillagers[vIdx];
            if (!v.alive)
            {
                DestroyImmediate(v.gameObject);
                villagers.RemoveAt(vIdx);
            }
        }
        RestartGame(false);
    }

    void GenerateGrid()
    {
        var tiles = GameObject.FindGameObjectsWithTag(Tile.TAG);
        for (int tIdx = 0; tIdx < tiles.Length; tIdx++)
        {
            var currTile = tiles[tIdx];
            if (currTile != null)
            {
                var tObj = currTile.GetComponent<Tile>();
                grid[tObj.x, tObj.y] = null;
                DestroyImmediate(currTile);
            }
        }

        seed = rnd.Next(100);
        Vector3 origin = new Vector3(0, 0, 0);
        var sr = tile.GetComponent<SpriteRenderer>();
        sr.size = new Vector2(.64f, .64f);
        float tileWidth = sr.size.x;
        float tileHeight = sr.size.y;
        Vector2 gridSize = new Vector2(tileWidth * grid.GetLength(0), tileHeight * grid.GetLength(1));

        Vector2 topLeft = new Vector2(
            -(tileWidth * grid.GetLength(0) / 2) + (tileWidth / 2),
            tileHeight * grid.GetLength(1) / 2
        );
        Vector3 pointer = topLeft;

        var index = 0;
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            pointer.x = topLeft.x;
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                index++;
                grid[x, y] = CreateTile(pointer, new Vector2(x, y));
                pointer.x += tileWidth;
            }
            pointer.y -= tileHeight;
        }
    }

    Tile CreateTile (Vector3 worldPos, Vector2 gridPos)
    {
        var tileObj = Instantiate(tile, worldPos, Quaternion.identity, game.transform);
        var gridTile = tileObj.GetComponent<Tile>();
        gridTile.Init(seed, (int)gridPos.x, (int)gridPos.y, worldPos);

        // Track certain ytiles for game state
        if (gridTile.type == TileType.VILLAGE) { villageTiles.Add(gridTile); }
        else if (gridTile.type == TileType.GRASS) { grassTiles.Add(gridTile); }

        return gridTile;
    }

    void GenerateVillagers ()
    {
        var vs = GameObject.FindGameObjectsWithTag(Villager.TAG);
        for (int vIdx = 0; vIdx < vs.Length; vIdx++)
        {
            DestroyImmediate(vs[vIdx]);
        }

        for (int i = 0; i < NUMBER_OF_VILLAGERS; i++)
        {
            var x = rnd.Next(grid.GetLength(0));
            var y = rnd.Next(grid.GetLength(1));
            GameObject vObj = Instantiate(villager, grid[x, y].position, Quaternion.identity, game.transform);
            Villager v = vObj.GetComponent<Villager>();
            villagers.Add(v);
            v.gridStage = this;
        }
    }

    bool ValidGame()
    {
        bool foundVillage = false;
        bool foundFire = false;

        System.Func<bool> valid = () => foundVillage && foundFire;

        for (var y=0; y < grid.GetLength(1); y++)
        {
            for (var x=0; x < grid.GetLength(0); x++)
            {
                var type = grid[x, y].type;
                if (type == TileType.VILLAGE)
                {
                    foundVillage = true;
                }
                else if (type == TileType.FIRE)
                {
                    foundFire = true;
                }

                if (valid()) { break; }
            }

            if (valid()) { break; }
        }

        return foundVillage && foundFire;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectTile();
        }
    }

    void SelectTile()
    {
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hitInfo = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
        if (hitInfo.collider)
        {
            GameObject mouseTileObj = hitInfo.collider.gameObject;
            if (lastClickedTile != null)
            {
                if (mouseTileObj.CompareTag(Tile.TAG))
                {
                    var clickedTile = mouseTileObj.GetComponent<Tile>();
                    if (clickedTile.IsSelectable())
                    {
                        SwapPosition(lastClickedTile, clickedTile);
                    }
                }
                lastClickedTile.SetSelected(false);
                lastClickedTile = null;
            }
            else
            {
                if (mouseTileObj.CompareTag(Tile.TAG))
                {
                    Tile mouseTile = mouseTileObj.GetComponent<Tile>();
                    if (mouseTile.IsSelectable())
                    {
                        lastClickedTile = mouseTile;
                        mouseTile.SetSelected(true);
                    }
                }
            }

        }
    }

    void SwapPosition(Tile a, Tile b)
    {
        var aGridPosX = a.x;
        var aGridPosY = a.y;
        var bGridPosX = b.x;
        var bGridPosY = b.y;
        var aWorldPos = new Vector3(a.transform.position.x, a.transform.position.y, a.transform.position.z);
        var bWorldPos = new Vector3(b.transform.position.x, b.transform.position.y, b.transform.position.z);

        // Swap in logical
        b.SetPos(aGridPosX, aGridPosY, aWorldPos);
        grid[aGridPosX, aGridPosY] = b;
        a.SetPos(bGridPosX, bGridPosY, bWorldPos);
        grid[bGridPosX, bGridPosY] = a;
    }

    IEnumerator UpdateTiles()
    {
        yield return new WaitForSeconds(TILE_UPDATE_TIMEOUT_DURATION);

        _prevVillageCount = villageTiles.Count;
        _prevGrassCount = grassTiles.Count;

        var gridSnapshot = new TileType[grid.GetLength(0), grid.GetLength(1)];
        for (var y = 0; y < grid.GetLength(1); y++)
        {
            for (var x = 0; x < grid.GetLength(0); x++)
            {
                gridSnapshot[x, y] = grid[x, y].type;
            }
        }

        for (var y = 0; y < grid.GetLength(1); y++)
        {
            for (var x = 0; x < grid.GetLength(0); x++)
            {
                var currTile = grid[x, y];
                var isGrassTile = currTile.type == TileType.GRASS;

                currTile.UpdateTile(gridSnapshot);

                if (currTile.type == TileType.BURNT_VILLAGE) { villageTiles.Remove(currTile); }
                if (isGrassTile && currTile.type == TileType.FIRE) { grassTiles.Remove(currTile); }
            }
        }
        if (villageTiles.Count <= 0 || AliveVillagerCount() <= 0)
        {
            GameOverVisible(true);
        }
        else if (!FireIsSpreading)
        {
            NextWave();
        }
        else
        {
            updateRoutine = StartCoroutine(UpdateTiles());
        }
    }

    void GameOverVisible (bool visible)
    {
        var gameOverCanvas = gameOver.GetComponent<Canvas>();
        gameOverCanvas.enabled = visible;
    }
}
