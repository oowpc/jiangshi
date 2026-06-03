using UnityEngine;
using Jiangshi.Grid;

namespace Jiangshi.Core
{
    public sealed class MapGenerator : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TerrainGenerator terrainGenerator;
        [SerializeField] private GameObject forestPrefab;
        [SerializeField] private Sprite[] forestSprites;
        [SerializeField] private GameObject ironOrePrefab;
        [SerializeField] private GameObject copperOrePrefab;
        [SerializeField] private float forestThreshold = 0.6f;
        [SerializeField] private int ironClusterCount = 6;
        [SerializeField] private int ironClusterSize = 5;
        [SerializeField] private int copperClusterCount = 6;
        [SerializeField] private int copperClusterSize = 5;
        [SerializeField] private int clearRadius = 8;

        [Header("Forest Visuals")]
        [SerializeField] private Vector2 forestWidthScaleRange = new(1.15f, 1.45f);
        [SerializeField] private Vector2 forestHeightScaleRange = new(1.55f, 2.15f);
        [SerializeField] private float forestPositionJitter = 0.35f;
        [SerializeField] private int forestBaseSortingOrder = 5000;
        [SerializeField] private float forestSortingUnitsPerWorld = 10f;

        [Header("Ore Decals")]
        [SerializeField] private Vector2 oreDecalScaleRange = new(1.35f, 1.7f);
        [SerializeField] private float oreDecalPositionJitter = 0.15f;
        [SerializeField] private bool randomizeOreDecalRotation = true;

        [Header("Initial Zombies")]
        [SerializeField] private Units.UnitData zombieData;
        [SerializeField] private Units.UnitManager unitManager;
        [SerializeField] private int initialZombieCount = 64;
        [SerializeField] private int initialZombieEdgeBand = 14;
        [SerializeField] private int initialZombieBoundaryMargin = 3;
        [SerializeField] private float initialZombiePositionJitter = 0.35f;
        [SerializeField] private int initialZombieSpawnAttempts = 80;
        [SerializeField] private Transform defaultTarget;

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
            SpawnInitialZombies(center);
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

                    SpawnForestVisual(new GridPosition(x, y));
                }
            }
        }

        private void SpawnForestVisual(GridPosition position)
        {
            if (forestPrefab == null)
            {
                return;
            }

            var worldPosition = gridManager.GridToWorld(position);
            if (forestPositionJitter > 0f)
            {
                worldPosition.x += Random.Range(-forestPositionJitter, forestPositionJitter);
                worldPosition.z += Random.Range(-forestPositionJitter, forestPositionJitter);
            }

            var instance = Instantiate(forestPrefab, worldPosition, Quaternion.identity);
            var spriteRenderer = instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                var sprite = PickForestSprite();
                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }

                spriteRenderer.flipX = Random.value < 0.5f;
                ApplyForestDepthSorting(spriteRenderer, worldPosition, Random.Range(-2, 3));
            }

            var widthScale = Random.Range(
                Mathf.Min(forestWidthScaleRange.x, forestWidthScaleRange.y),
                Mathf.Max(forestWidthScaleRange.x, forestWidthScaleRange.y));
            var heightScale = Random.Range(
                Mathf.Min(forestHeightScaleRange.x, forestHeightScaleRange.y),
                Mathf.Max(forestHeightScaleRange.x, forestHeightScaleRange.y));
            instance.transform.localScale = new Vector3(widthScale, heightScale, 1f);

            DisableColliders(instance);
        }

        private Sprite PickForestSprite()
        {
            if (forestSprites == null || forestSprites.Length == 0)
            {
                return null;
            }

            return forestSprites[Random.Range(0, forestSprites.Length)];
        }

        private void ApplyForestDepthSorting(SpriteRenderer spriteRenderer, Vector3 worldPosition, int offset)
        {
            spriteRenderer.sortingOrder = forestBaseSortingOrder - Mathf.RoundToInt(worldPosition.z * forestSortingUnitsPerWorld) + offset;
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
                    var instance = Instantiate(prefab, GetClusterObjectPosition(pos, content), GetClusterObjectRotation(prefab, content));
                    ApplyClusterObjectVariation(instance, content);
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

        private Vector3 GetClusterObjectPosition(GridPosition position, CellContent content)
        {
            var worldPosition = gridManager.GridToWorld(position);
            if (IsOreContent(content) && oreDecalPositionJitter > 0f)
            {
                worldPosition.x += Random.Range(-oreDecalPositionJitter, oreDecalPositionJitter);
                worldPosition.z += Random.Range(-oreDecalPositionJitter, oreDecalPositionJitter);
            }

            return worldPosition;
        }

        private Quaternion GetClusterObjectRotation(GameObject prefab, CellContent content)
        {
            if (!IsOreContent(content) || !randomizeOreDecalRotation)
            {
                return prefab.transform.rotation;
            }

            return Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        }

        private void ApplyClusterObjectVariation(GameObject instance, CellContent content)
        {
            if (!IsOreContent(content))
            {
                return;
            }

            var minScale = Mathf.Min(oreDecalScaleRange.x, oreDecalScaleRange.y);
            var maxScale = Mathf.Max(oreDecalScaleRange.x, oreDecalScaleRange.y);
            var scale = Random.Range(minScale, maxScale);
            instance.transform.localScale = new Vector3(scale, scale, scale);
        }

        private static bool IsOreContent(CellContent content)
        {
            return content == CellContent.IronOre || content == CellContent.CopperOre;
        }

        private static void DisableColliders(GameObject instance)
        {
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (var collider in instance.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = false;
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

        private void SpawnInitialZombies(Vector2Int center)
        {
            if (zombieData == null || unitManager == null) return;

            for (var i = 0; i < initialZombieCount; i++)
            {
                for (var attempt = 0; attempt < initialZombieSpawnAttempts; attempt++)
                {
                    var gridPosition = PickPerimeterZombiePosition();
                    if (Mathf.Abs(gridPosition.X - center.x) < clearRadius && Mathf.Abs(gridPosition.Y - center.y) < clearRadius)
                        continue;

                    var cell = gridManager.GetCell(gridPosition);
                    if (cell == null || !cell.IsWalkable) continue;

                    var pos = gridManager.GridToWorld(gridPosition);
                    pos = ApplyInitialZombieJitter(pos);
                    var unit = unitManager.Spawn(zombieData, pos, Quaternion.identity);
                    if (unit is Units.Zombie zombie && defaultTarget != null)
                        zombie.SetTarget(defaultTarget);
                    break;
                }
            }
        }

        private GridPosition PickPerimeterZombiePosition()
        {
            var marginX = Mathf.Clamp(initialZombieBoundaryMargin, 0, Mathf.Max(0, (gridManager.Width - 1) / 2));
            var marginY = Mathf.Clamp(initialZombieBoundaryMargin, 0, Mathf.Max(0, (gridManager.Height - 1) / 2));
            var minX = marginX;
            var minY = marginY;
            var maxX = Mathf.Max(minX, gridManager.Width - 1 - marginX);
            var maxY = Mathf.Max(minY, gridManager.Height - 1 - marginY);
            var bandX = Mathf.Clamp(initialZombieEdgeBand, 1, Mathf.Max(1, gridManager.Width / 2));
            var bandY = Mathf.Clamp(initialZombieEdgeBand, 1, Mathf.Max(1, gridManager.Height / 2));

            if (Random.value < 0.5f)
            {
                var y = Random.value < 0.5f
                    ? Random.Range(minY, Mathf.Min(maxY + 1, bandY))
                    : Random.Range(Mathf.Max(minY, gridManager.Height - bandY), maxY + 1);
                return new GridPosition(Random.Range(minX, maxX + 1), Mathf.Clamp(y, minY, maxY));
            }

            var x = Random.value < 0.5f
                ? Random.Range(minX, Mathf.Min(maxX + 1, bandX))
                : Random.Range(Mathf.Max(minX, gridManager.Width - bandX), maxX + 1);
            return new GridPosition(Mathf.Clamp(x, minX, maxX), Random.Range(minY, maxY + 1));
        }

        private Vector3 ApplyInitialZombieJitter(Vector3 position)
        {
            if (initialZombiePositionJitter <= 0f)
            {
                return position;
            }

            position.x += Random.Range(-initialZombiePositionJitter, initialZombiePositionJitter);
            position.z += Random.Range(-initialZombiePositionJitter, initialZombiePositionJitter);
            return position;
        }
    }
}
