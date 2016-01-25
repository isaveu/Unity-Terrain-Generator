using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LibNoise.Unity;
using LibNoise.Unity.Generator;
using LibNoise.Unity.Operator;

public class SlugTerrainGenerator : MonoBehaviour {
			
	enum TILES { deep_water    = 49, 
			     shallow_water = 4, 
		         sand          = 22, 
			     grass         = 13, 
		         soft_montain  = 31, 
				 hard_montain  = 40}

	public int   _width;
	public int   _height;
	public float _scale;

	// Contains all the sprites 
	private Sprite[] _tileSet;

	// Contains the tile values
	private int [,] _tileMap;

	// Contains the game objects which will receive the sprites
	private GameObject[,] _tiles;

	private GameObject _terrain;
	private GameObject _decoration;

	private readonly int  NEIGHBORS_AMOUNT = 9;

	// Use this for initialization
	void Start () {

		int   octaves = 6;
		float frequency = 1.0f;
		float persistence = 0.5f;

		// Load all tile textures
		_tileSet = Resources.LoadAll<Sprite>("Textures/tileset");

		_tileMap = new int[_width, _height];

		_terrain = new GameObject ();
		_terrain.name = "Terrain";

		_decoration = new GameObject ();
		_decoration.name = "Decoration";

		InitTerrain ();
	}

	void Update() {

		if (Input.GetKeyDown (KeyCode.R)) {

			foreach(Transform child in _decoration.transform) {
				Destroy(child.gameObject);
			}

			float seedX = Random.Range (0, SlugUtils.TILE_SIZE);
			float seedY = Random.Range (0, SlugUtils.TILE_SIZE);
			float seedZ = Random.Range (1, 4);

			GenerateTerrain (seedX, seedY, seedZ);

			RenderTerrain ();
		}
	}

	void InitTerrain() {

		_tiles = new GameObject[_width,_height];

		for (int i = 0; i < _width; i++) {
		
			for (int j = 0; j < _height; j++) {

				_tiles [i, j] = CreateTile (i, j, _terrain.transform);
			}
		}
	}

	GameObject CreateTile(int i, int j, Transform terrain) {

		float worldTileSize = (float)(SlugUtils.TILE_SIZE - 1)/ (float)SlugUtils.PIXEL_UNIT;

		// Creates a game object for the tile
		GameObject gameObj = new GameObject ();
		gameObj.name = "Tile_" + i + "_" + j;

		// Set position and parent
		gameObj.transform.parent = terrain;
		gameObj.transform.position = new Vector2 (i, j) * worldTileSize;

		// Add sprite renderer to support the texture
		gameObj.AddComponent<SpriteRenderer> ();

		return gameObj;
	}
	
	void GenerateTerrain(float seedX, float seedY, float randomPow) {

		for (int i = 0; i < _width; i++) {

			for (int j = 0; j < _height; j++) {

				float x = seedX + (float)i / (float)_width * _scale;
				float y = seedY + (float)j / (float)_height * _scale;

				float noise = Mathf.Pow(Mathf.PerlinNoise (x, y), randomPow);

				_tileMap[i,j] = NoiseToTile (noise);
			}
		}
	}

	void RenderTerrain() {

		for (int i = 0; i < _width; i++) {

			for (int j = 0; j < _height; j++) {

				int tileVarition = 0;
					
				switch(_tileMap [i, j]) {

				case (int)TILES.shallow_water:
					tileVarition = HandleTransitionTiles(i, j, (int)TILES.shallow_water,  (int)TILES.deep_water);
					break;

				case (int)TILES.sand:
					tileVarition = HandleTransitionTiles(i, j, (int)TILES.sand,  (int)TILES.shallow_water);
					break;

				case (int)TILES.grass:
					tileVarition = HandleTransitionTiles(i, j, (int)TILES.grass,  (int)TILES.sand);
					break;

				case (int)TILES.soft_montain:
					tileVarition = HandleTransitionTiles(i, j, (int)TILES.soft_montain,  (int)TILES.grass);
					break;

				case (int)TILES.hard_montain:
					tileVarition = HandleTransitionTiles(i, j, (int)TILES.hard_montain,  (int)TILES.soft_montain);
					break;
				}
					
				RenderTile (_tiles[i,j], _tileMap[i,j] + tileVarition);
			}
		}
	}

	int HandleTransitionTiles(int x, int y, int fromTile, int toTile) {

		int tileVarition = 0;

		int r = _tileMap.GetLength (0) - 1;
		int c = _tileMap.GetLength (1) - 1;

		bool[] equalNeighbors = new bool[NEIGHBORS_AMOUNT];

		for (int i = x - 1; i <= x + 1; i++) {

			for (int j = y - 1; j <= y + 1; j++) {

				int index = (i - x + 1) * 3 + (j - y + 1);

				bool value = false;

				if(i >= 0 && i <= r && j >= 0 && j <= c)
					value = (_tileMap [i, j] == toTile);

				equalNeighbors [index] = value;
			}
		}
			
		tileVarition = DecideTile (equalNeighbors);

		if (tileVarition != 0) {
			
			GameObject backTile = CreateTile (x, y, _decoration.transform);
			backTile.transform.position = _tiles [x, y].transform.position + Vector3.forward;
			RenderTile (backTile, toTile);
		}

		return tileVarition;
	}

	void RenderTile(GameObject tile, int tileValue) {

		SpriteRenderer tileRenderer = tile.GetComponent<SpriteRenderer> ();
		tileRenderer.sprite = _tileSet[tileValue];
	}

	int NoiseToTile(float noiseValue) {

		noiseValue = Mathf.Clamp (noiseValue, 0f, 1f);

		if (noiseValue >= 0f && noiseValue < 0.3f)
			return (int)TILES.deep_water;

		if (noiseValue >= 0.3f && noiseValue < 0.4f)
			return (int)TILES.shallow_water;

		if (noiseValue >= 0.4f && noiseValue < 0.495f)
			return (int)TILES.sand;

		if (noiseValue >= 0.495f && noiseValue < 0.7f)
			return (int)TILES.grass;

		if (noiseValue >= 0.7f && noiseValue < 0.9f)
			return (int)TILES.soft_montain;

		if (noiseValue >= 0.9f && noiseValue <= 1f)
			return (int)TILES.hard_montain;

		return (int)TILES.hard_montain;
	}

	int DecideTile(bool []equalNeighbors) {
	
		int sum = 0;

		for (int i = 0; i < NEIGHBORS_AMOUNT; i++) {

			if (equalNeighbors [i])
				sum += (int)Mathf.Pow (2f, (float)i);
		}

		if (sum == 294 || sum == 35  || sum == 38  || 
			sum == 257 || sum == 39  || sum == 295 || sum == 34) 
			return -4;

		if (sum == 32  || sum == 36  || sum == 288 || sum == 292) 
			return -3;
		 
		if (sum == 480 || sum == 416 || sum == 420 || sum == 484 || 
			sum == 164 || sum == 224 || sum == 226 || sum == 160) 
			return -2;

		if (sum == 3   || sum == 5   || sum == 6   || sum == 7)   
			return -1;

		if (sum == 192 || sum == 128 || sum == 384 || sum == 448) 
			return  1;

		if (sum == 11  || sum == 79  || sum == 75  || sum == 15 || 
			sum == 14  || sum == 10)  
			return  2;

		if (sum == 8   || sum == 9   || sum == 72  || sum == 73)  
			return  3;

		if (sum == 200 || sum == 201 || sum == 456 || sum == 457  || 
			sum == 137 || sum == 136 || sum == 392) 
			return  4;

		return 0;
	}
}
