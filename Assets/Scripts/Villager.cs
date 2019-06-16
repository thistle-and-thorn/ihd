using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Villager : MonoBehaviour {

    public static readonly string TAG = "Villager";

	public GridStage gridStage;
	[SerializeField]
	float speed = 2.0f;
	[SerializeField]
	float waterSpeed = 0.25f;
	Vector2 villagerSize;
	public bool alive = true;

    void Start () 
	{
        gameObject.tag = TAG;
		var villagerWidth = (float) this.GetComponent<SpriteRenderer>().bounds.size.x;
		var villagerHeight = (float) this.GetComponent<SpriteRenderer>().bounds.size.y;
		villagerSize = new Vector2(villagerWidth, villagerHeight);
	}
	
	void Update () 
	{
		var collidingObject = Physics2D.OverlapBox(transform.position, villagerSize / 2, 0.0f).gameObject;
        Tile tile = collidingObject.GetComponent<Tile>();


		float localSpeed = tile.type == TileType.WATER ? waterSpeed : speed;
		if (tile.type == TileType.FIRE) { FuckingDie(); }

		if (alive) 
		{
			float step = localSpeed * Time.deltaTime;
            var nearest = GetNearestVillage();
            if (nearest)
            {
                transform.position = Vector2.MoveTowards(transform.position, nearest.position, step);
            }
		}
		
	}

	Tile GetNearestVillage ()
	{
		var closestVillage = gridStage.villageTiles
            .Where(t => t != null)
			.OrderBy(t=> Vector2.Distance(transform.position, t.position))
			.FirstOrDefault();

        return closestVillage;
	}

	void FuckingDie () {
		this.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0);
		alive = false;
	}
}
