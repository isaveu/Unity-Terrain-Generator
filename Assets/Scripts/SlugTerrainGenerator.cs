using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SlugTerrainGenerator : MonoBehaviour {
			
	enum TILES { deep_water    = 49, 
			     shallow_water = 4, 
		         sand          = 22, 
			     grass         = 13, 
		         soft_montain  = 31, 
				 hard_montain  = 40}

	public int   _width = 64;
	public int   _height = 64;
	public float _mapScale = 0.25f;
	public float _timeScale = 0.15f;

	// Contains all the sprites 
	private Sprite[] _tileSet;

	// Contains the tile values
	private int [,] _tileMap;

	// Contains the game objects which will receive the sprites
	private GameObject[,] _tiles;

	private GameObject _terrain;
	private GameObject _decoration;

	private float _noiseSeed;
	private float _seedTimer;
	private Dictionary<int,int> _transitions;

	private readonly int  NEIGHBORS_AMOUNT = 9;

	// Use this for initialization
	void Start () {

		// Load all tile textures
		_tileSet = Resources.LoadAll<Sprite>("Textures/tileset");

		_tileMap = new int[_width, _height];

		_terrain = new GameObject ();
		_terrain.name = "Terrain";

		_decoration = new GameObject ();
		_decoration.name = "Decoration";

		_transitions = ParseTileTransitionsFile ("tile_transitions");

		InitTerrain ();
	}

	void Update() {

		if (Input.GetKeyUp (KeyCode.R)) {

			foreach(Transform child in _decoration.transform) {
				Destroy(child.gameObject);
			}

			_seedTimer = 0f;
			_noiseSeed = Time.time;

			GenerateTerrain (_noiseSeed);
			RenderTerrain ();
		}

		if (Input.GetKey (KeyCode.T) || Input.GetKey (KeyCode.Y)) {

			foreach(Transform child in _decoration.transform) {
				Destroy(child.gameObject);
			}

			float timeDirection = Input.GetKey (KeyCode.Y) ? -1f : 1f;

			_seedTimer += Time.deltaTime * _timeScale * timeDirection;
				
			GenerateTerrain (_noiseSeed + _seedTimer);
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
	
	void GenerateTerrain(float time) {

		PerlinNoise noiseGen = new PerlinNoise (0);

		for (int i = 0; i < _width; i++) {

			for (int j = 0; j < _height; j++) {

				float x = (float)i / (float)_width;
				float y = (float)j / (float)_height;

				float noise = noiseGen.FractalNoise3D(x, y, time, 2, _mapScale + 0.01f, 1.75f);

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

		noiseValue = Mathf.Clamp (noiseValue, -2f, 2f);

		if (noiseValue >= -2f  && noiseValue < 0f)
			return (int)TILES.deep_water;

		if (noiseValue >= 0f   && noiseValue < 0.25f)
			return (int)TILES.shallow_water;

		if (noiseValue >= 0.25f && noiseValue < 0.4f)
			return (int)TILES.sand;

		if (noiseValue >= 0.4f  && noiseValue < 0.75f)
			return (int)TILES.grass;

		if (noiseValue >= 0.75f && noiseValue < 0.95f)
			return (int)TILES.soft_montain;

		return (int)TILES.hard_montain;
	}

	int DecideTile(bool []equalNeighbors) {
	
		int sum = 0;

		for (int i = 0; i < NEIGHBORS_AMOUNT; i++) {

			if (equalNeighbors [i])
				sum += (int)Mathf.Pow (2f, (float)i);
		}

		if(_transitions.ContainsKey(sum))
			return _transitions [sum];

		return 0;
	}

	Dictionary<int, int> ParseTileTransitionsFile(string filepath) {

		Dictionary<int, int> tileTransitions = new Dictionary<int, int> ();
		TextAsset file = Resources.Load (filepath) as TextAsset;

		foreach (string line in file.text.Split('\n')) {

			string[] transition = line.Split (' ');

			for(int i = 0; i < transition.Length - 1; i++) {

				int key = int.Parse (transition [i]);
				int value = int.Parse (transition [transition.Length - 1]);

				tileTransitions.Add(key, value);
			}
		}

		return tileTransitions;
	}
}
