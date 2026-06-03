using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Jiangshi.Grid;
using UnityGrid = UnityEngine.Grid;

namespace Jiangshi.Core
{
    public enum TerrainType { Grass, Snow, Dirt, Water }

    public sealed class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float terrainScale = 0.04f;
        [SerializeField] private float vegetationScale = 0.08f;
        [SerializeField] private int terrainSortingOrder = -5;
        [SerializeField] private int seed;

        [Header("Tilesets (3x3 = 9 sprites, order: TL,T,TR,L,C,R,BL,B,BR)")]
        [SerializeField] private Sprite[] grassTileset;
        [SerializeField] private Sprite[] snowTileset;
        [SerializeField] private Sprite[] dirtTileset;
        [SerializeField] private Sprite[] waterTileset;

        private TerrainType[,] terrainMap;
        private readonly Dictionary<Sprite, Tile> tileCache = new();
        private const string TerrainObjectName = "Terrain";

        public TerrainType GetTerrain(int x, int y)
        {
            if (terrainMap == null || x < 0 || y < 0 || x >= gridManager.Width || y >= gridManager.Height)
                return TerrainType.Grass;
            return terrainMap[x, y];
        }

        public float GetVegetationDensity(int x, int y)
        {
            var nx = (x + seed + 500) * vegetationScale;
            var ny = (y + seed + 500) * vegetationScale;
            return Mathf.PerlinNoise(nx, ny);
        }

        private void Awake()
        {
            if (seed == 0) seed = Random.Range(0, 99999);
        }

        public void Generate()
        {
            if (gridManager == null) return;

            var w = gridManager.Width;
            var h = gridManager.Height;
            terrainMap = new TerrainType[w, h];

            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var nx = (x + seed) * terrainScale;
                    var ny = (y + seed) * terrainScale;
                    var noise = Mathf.PerlinNoise(nx, ny);

                    var type = noise switch
                    {
                        < 0.25f => TerrainType.Water,
                        < 0.45f => TerrainType.Dirt,
                        < 0.7f => TerrainType.Grass,
                        _ => TerrainType.Snow
                    };

                    var edgeDist = Mathf.Min(x, y, w - 1 - x, h - 1 - y);
                    if (edgeDist < 5 && type == TerrainType.Water)
                        type = TerrainType.Dirt;

                    terrainMap[x, y] = type;

                    if (type == TerrainType.Water)
                    {
                        var cell = gridManager.GetCell(new GridPosition(x, y));
                        if (cell != null)
                        {
                            cell.IsWalkable = false;
                            cell.IsBuildable = false;
                        }
                    }
                }
            }

            var tilemap = CreateTerrainTilemap();

            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var type = terrainMap[x, y];
                    var sprite = GetAutoTileSprite(x, y, type);
                    if (sprite == null) continue;

                    tilemap.SetTile(new Vector3Int(x, y, 0), GetTile(sprite));
                }
            }
        }

        private Tilemap CreateTerrainTilemap()
        {
            var existing = GameObject.Find(TerrainObjectName);
            if (existing != null)
            {
                DestroyGeneratedObject(existing);
            }

            var gridObject = new GameObject(TerrainObjectName);
            gridObject.transform.position = gridManager.GridToWorld(new GridPosition(0, 0)) -
                new Vector3(gridManager.CellSize * 0.5f, 0f, gridManager.CellSize * 0.5f);
            gridObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var unityGrid = gridObject.AddComponent<UnityGrid>();
            unityGrid.cellSize = new Vector3(gridManager.CellSize, gridManager.CellSize, 1f);

            var tilemapObject = new GameObject("Ground");
            tilemapObject.transform.SetParent(gridObject.transform, false);

            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

            var renderer = tilemapObject.AddComponent<TilemapRenderer>();
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            renderer.sortingOrder = terrainSortingOrder;

            return tilemap;
        }

        private Tile GetTile(Sprite sprite)
        {
            if (tileCache.TryGetValue(sprite, out var tile))
            {
                return tile;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tileCache[sprite] = tile;
            return tile;
        }

        private static void DestroyGeneratedObject(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Sprite GetAutoTileSprite(int x, int y, TerrainType type)
        {
            var tileset = GetTileset(type);
            if (tileset == null || tileset.Length < 9)
                return tileset != null && tileset.Length > 0 ? tileset[0] : null;

            var same = type;
            var t = GetTerrain(x, y + 1) == same;
            var b = GetTerrain(x, y - 1) == same;
            var l = GetTerrain(x - 1, y) == same;
            var r = GetTerrain(x + 1, y) == same;

            int index;
            if (!t && !l) index = 0;
            else if (!t && !r) index = 2;
            else if (!b && !l) index = 6;
            else if (!b && !r) index = 8;
            else if (!t) index = 1;
            else if (!b) index = 7;
            else if (!l) index = 3;
            else if (!r) index = 5;
            else index = 4;

            return tileset[index];
        }

        private Sprite[] GetTileset(TerrainType type)
        {
            var ts = type switch
            {
                TerrainType.Snow => snowTileset,
                TerrainType.Dirt => dirtTileset,
                TerrainType.Water => waterTileset,
                _ => grassTileset
            };
            return ts != null && ts.Length > 0 ? ts : grassTileset;
        }
    }
}
