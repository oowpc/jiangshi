using UnityEngine;
using Jiangshi.Grid;

namespace Jiangshi.Core
{
    public enum TerrainType { Grass, Snow, Dirt, Water }

    public sealed class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float terrainScale = 0.04f;
        [SerializeField] private float vegetationScale = 0.08f;
        [SerializeField] private int seed;

        [Header("Tilesets (3x3 = 9 sprites, order: TL,T,TR,L,C,R,BL,B,BR)")]
        [SerializeField] private Sprite[] grassTileset;
        [SerializeField] private Sprite[] snowTileset;
        [SerializeField] private Sprite[] dirtTileset;
        [SerializeField] private Sprite[] waterTileset;

        private TerrainType[,] terrainMap;

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

            // Pass 1: determine terrain types
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

            // Pass 2: place sprites with auto-tiling
            var parent = new GameObject("Terrain").transform;

            for (var x = 0; x < w; x++)
            {
                for (var y = 0; y < h; y++)
                {
                    var type = terrainMap[x, y];
                    var sprite = GetAutoTileSprite(x, y, type);
                    if (sprite == null) continue;

                    var go = new GameObject($"T_{x}_{y}");
                    go.transform.SetParent(parent);
                    go.transform.position = gridManager.GridToWorld(new GridPosition(x, y));
                    go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = -1;
                }
            }
        }

        // 3x3 tileset layout:
        // [0]TL [1]T  [2]TR
        // [3]L  [4]C  [5]R
        // [6]BL [7]B  [8]BR
        private Sprite GetAutoTileSprite(int x, int y, TerrainType type)
        {
            var tileset = GetTileset(type);
            if (tileset == null || tileset.Length < 9)
            {
                // Fallback: use index 0 or first available
                return tileset != null && tileset.Length > 0 ? tileset[0] : null;
            }

            var same = type;
            var t = GetTerrain(x, y + 1) == same;
            var b = GetTerrain(x, y - 1) == same;
            var l = GetTerrain(x - 1, y) == same;
            var r = GetTerrain(x + 1, y) == same;

            // Pick from 3x3 based on which edges border different terrain
            int index;
            if (!t && !l) index = 0;       // top-left corner
            else if (!t && !r) index = 2;   // top-right corner
            else if (!b && !l) index = 6;   // bottom-left corner
            else if (!b && !r) index = 8;   // bottom-right corner
            else if (!t) index = 1;         // top edge
            else if (!b) index = 7;         // bottom edge
            else if (!l) index = 3;         // left edge
            else if (!r) index = 5;         // right edge
            else index = 4;                 // center (all same)

            return tileset[index];
        }

        private Sprite[] GetTileset(TerrainType type)
        {
            return type switch
            {
                TerrainType.Grass => grassTileset,
                TerrainType.Snow => snowTileset,
                TerrainType.Dirt => dirtTileset,
                TerrainType.Water => waterTileset,
                _ => grassTileset
            };
        }
    }
}
