using UnityEngine;

public enum TileType
{
    WATER,
    GRASS,
    FIRE,
    VILLAGE,
    BURNT_VILLAGE,
    EDGE
}

public class Tile : MonoBehaviour
{
    public static readonly string TAG = "Tile";

    public System.Random rnd = new System.Random();
    public int x;
    public int y;
    public TileType type;
    bool selected;

    public Vector3 position
    {
        get { return gameObject.transform.position; }
    }

    public void Init (float seed, int x, int y, Vector3 worldPos)
    {
        gameObject.tag = TAG;
        SetPos(x, y, worldPos);
        InitType(seed);
        RenderTile();
    }

    public void SetPos(int x, int y, Vector3 worldPosition)
    {
        this.x = x;
        this.y = y;
        gameObject.transform.position = worldPosition;
    }

    void InitType (float seed)
    {
        float elevScale = 0.25f;
        float noiseElevation = Mathf.PerlinNoise((x * elevScale) + seed, (y * elevScale) + seed);

        // default to grass
        type = TileType.GRASS;

        if (noiseElevation < 0.15f)
        {
            type = TileType.FIRE;
        }
        else if (noiseElevation > 0.6f)
        {
            type = TileType.WATER;
        }
        else if (noiseElevation >= 0.50f && noiseElevation <= 0.6f)
        {
            // 10%
            if (rnd.Next(100) < 5)
            {
               type = TileType.VILLAGE;
            }
        }
    }

    public void SetType(TileType type)
    {
        this.type = type;
        RenderTile();
    }

    public void SetSelected(bool isSelected)
    {
        selected = isSelected;
        RenderTile();
    }

    public bool IsSelectable()
    {
        return type != TileType.VILLAGE && type != TileType.BURNT_VILLAGE && type != TileType.FIRE;
    }

    public void RenderTile ()
    {
        float opacity = 1f;
        if (selected)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new Color(200, 200, 200);
        }
        else
        {
            if (type == TileType.FIRE)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(0.79f, 0.29f, 0.08f, opacity);
            }
            else if (type == TileType.WATER)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(0.16f, 0.62f, 0.59f, opacity);
            }
            else if (type == TileType.GRASS)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(0f, 0.6f, 0.2f, opacity);
            }
            else if (type == TileType.VILLAGE)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(0.4f, 0.2f, 0f);
            }
            else if (type == TileType.BURNT_VILLAGE)
            {
                gameObject.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0);
            }
        }
    }

    bool IsFlammable
    {
        get {
            return (
                type == TileType.GRASS
                || type == TileType.VILLAGE
            );
        }
    }

    void DestroyVillage()
    {
        if (type != TileType.VILLAGE) { return; }
        SetType(TileType.BURNT_VILLAGE);
        //TODO: Add a negative multiplier, or something
    }

    void CatchFire()
    {
        if (type == TileType.VILLAGE)
        {
            DestroyVillage();
        }
        else if (type == TileType.GRASS)
        {
            SetType(TileType.FIRE);
        }
    }

    public void UpdateTile (TileType[,] grid)
    {
        // do some updates based on surrounding tiles
        TileType leftTileType = x > 0 ? grid[x - 1, y] : TileType.EDGE;
        TileType rightTileType = x < grid.GetLength(0) - 1 ? grid[x + 1, y] : TileType.EDGE;
        TileType topTileType = y > 0 ? grid[x, y - 1] : TileType.EDGE;
        TileType bottomTileType = y < grid.GetLength(1) - 1 ? grid[x, y + 1] : TileType.EDGE;
        // check for nearby fire
        if (IsFlammable)
        {
            if (leftTileType == TileType.FIRE)
            {
                CatchFire();
            }
            if (rightTileType == TileType.FIRE) {
                CatchFire();
            }
            if (topTileType == TileType.FIRE)
            {
                CatchFire();
            }
            if (bottomTileType == TileType.FIRE)
            {
                CatchFire();
            }
        }
    }
}
