using UnityEngine;
using Jiangshi.Grid;

namespace Jiangshi.Core
{
    public sealed class MapGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TerrainGenerator terrainGenerator;
        [SerializeField] private GameObject forestPrefab;
        [SerializeField] private GameObject ironOrePrefab;
        [SerializeField] private GameObject copperOrePrefab;
        [SerializeField] private float forestThreshold = 0.6f;
        [SerializeField] private int ironClusterCount = 6;
        [SerializeField] private int ironClusterSize = 5;
        [SerializeField] private int copperClusterCount = 6;
        [SerializeField] private int copperClusterSize = 5;
        [SerializeField] private int clearRadius = 8;

        private void Start()
        {
            if (terrainGenerator != null)
                terrainGenerator.Generate();
            Generate();
        }

        private void Generate()
        {
            if (gridManager == null) return;

            var center = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);

            SpawnForestByNoise(center);
            SpawnClusters(ironOrePrefab, ironClusterCount, ironClusterSize, false, CellContent.IronOre, center);
            SpawnClusters(copperOrePrefab, copperClusterCount, copperClusterSize, false, CellContent.CopperOre, center);
        }

        private void SpawnForestByNoise(Vector2Int center)
        {
            if (forestPrefab == null || terrainGenerator == null) return;

            for (var x = 0; x < gridManager.Width; x++)
            {
                for (var y = 0; y < gridManager.Height; y++)
                {
                    if (Mathf.Abs(x - center.x) < clearRadius && Mathf.Abs(y - center.y) < clearRadius)
                        continue;

                    var edgeDist = Mathf.Min(x, y, gridManager.Width - 1 - x, gridManager.Height - 1 - y);
                    if (edgeDist < 5) continue;

                    var cell = gridManager.GetCell(new GridPosition(x, y));
                    if (cell == null || cell.IsOccupied || !cell.IsWalkable) continue;

                    if (terrainGenerator.GetTerrain(x, y) == TerrainType.Water) continue;

                    if (terrainGenerator.GetVegetationDensity(x, y) < forestThreshold) continue;

                    cell.IsOccupied = true;
                    cell.IsBuildable = false;
                    cell.IsWalkable = false;
                    cell.Content = CellContent.Forest;

                    Instantiate(forestPrefab, gridManager.GridToWorld(new GridPosition(x, y)), forestPrefab.transform.rotation);
                }
            }
        }

        private void SpawnClusters(GameObject prefab, int clusterCount, int clusterSize, bool blocksWalking, CellContent content, Vector2Int center)
        {
            if (prefab == null) return;

            for (var c = 0; c < clusterCount; c++)
            {
                int sx = 0, sy = 0;
                var found = false;
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    sx = Random.Range(3, gridManager.Width - 3);
                    sy = Random.Range(3, gridManager.Height - 3);
                    if (Mathf.Abs(sx - center.x) < clearRadius && Mathf.Abs(sy - center.y) < clearRadius)
                        continue;
                    var seedCell = gridManager.GetCell(new GridPosition(sx, sy));
                    if (seedCell == null || seedCell.IsOccupied || !seedCell.IsWalkable) continue;
                    found = true;
                    break;
                }
                if (!found) continue;

                var open = new System.Collections.Generic.Queue<GridPosition>();
                open.Enqueue(new GridPosition(sx, sy));
                var placed = 0;

                while (open.Count > 0 && placed < clusterSize)
                {
                    var pos = open.Dequeue();
                    var cell = gridManager.GetCell(pos);
                    if (cell == null || cell.IsOccupied || !cell.IsWalkable) continue;
                    if (Mathf.Abs(pos.X - center.x) < clearRadius && Mathf.Abs(pos.Y - center.y) < clearRadius)
                        continue;

                    cell.IsOccupied = true;
                    cell.IsBuildable = false;
                    if (blocksWalking)
                        cell.IsWalkable = false;
                    cell.Content = content;
                    Instantiate(prefab, gridManager.GridToWorld(pos), prefab.transform.rotation);
                    placed++;

                    var dirs = new GridPosition[] {
                        new(pos.X + 1, pos.Y), new(pos.X - 1, pos.Y),
                        new(pos.X, pos.Y + 1), new(pos.X, pos.Y - 1)
                    };
                    Shuffle(dirs);
                    foreach (var d in dirs)
                        open.Enqueue(d);
                }
            }
        }

        private void Shuffle(GridPosition[] arr)
        {
            for (var i = arr.Length - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
