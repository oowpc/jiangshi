using UnityEngine;

namespace Jiangshi.Grid
{
    public sealed class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private int width = 128;
        [SerializeField] private int height = 128;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 origin;
        [SerializeField] private bool drawDebugGrid = true;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color occupiedCellColor = new Color(1f, 0.2f, 0.1f, 0.22f);
        [SerializeField] private float debugGridYOffset = 0.03f;

        private Cell[,] cells;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            Instance = this;
            BuildGrid();
        }

        public void BuildGrid()
        {
            cells = new Cell[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    cells[x, y] = new Cell(new GridPosition(x, y));
                }
            }
        }

        public bool IsInside(GridPosition position)
        {
            return position.X >= 0 && position.X < width && position.Y >= 0 && position.Y < height;
        }

        public Cell GetCell(GridPosition position)
        {
            return IsInside(position) ? cells[position.X, position.Y] : null;
        }

        public GridPosition WorldToGrid(Vector3 worldPosition)
        {
            var local = worldPosition - origin;
            return new GridPosition(
                Mathf.FloorToInt(local.x / cellSize),
                Mathf.FloorToInt(local.z / cellSize));
        }

        public GridPosition WorldToGridOrigin(Vector3 worldPosition, Vector2Int size)
        {
            var local = worldPosition - origin;
            return new GridPosition(
                Mathf.FloorToInt(local.x / cellSize - size.x * 0.5f),
                Mathf.FloorToInt(local.z / cellSize - size.y * 0.5f));
        }

        public Vector3 GridToWorld(GridPosition position)
        {
            return origin + new Vector3(
                (position.X + 0.5f) * cellSize,
                0f,
                (position.Y + 0.5f) * cellSize);
        }

        public Vector3 GridToWorld(GridPosition position, Vector2Int size)
        {
            return origin + new Vector3(
                (position.X + size.x * 0.5f) * cellSize,
                0f,
                (position.Y + size.y * 0.5f) * cellSize);
        }

        public bool IsWalkableAt(Vector3 worldPosition)
        {
            var cell = GetCell(WorldToGrid(worldPosition));
            return cell != null && cell.IsWalkable;
        }

        public bool CanOccupy(GridPosition originPosition, Vector2Int size)
        {
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var cell = GetCell(new GridPosition(originPosition.X + x, originPosition.Y + y));
                    if (cell == null || !cell.IsBuildable || cell.IsOccupied)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void SetOccupied(GridPosition originPosition, Vector2Int size, bool occupied, bool walkable)
        {
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var cell = GetCell(new GridPosition(originPosition.X + x, originPosition.Y + y));
                    if (cell == null)
                    {
                        continue;
                    }

                    cell.IsOccupied = occupied;
                    cell.IsWalkable = walkable;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawDebugGrid || cellSize <= 0f)
            {
                return;
            }

            DrawGridLines();
            DrawOccupiedCells();
        }

        private void DrawGridLines()
        {
            Gizmos.color = gridLineColor;

            var y = origin.y + debugGridYOffset;
            var maxX = origin.x + width * cellSize;
            var maxZ = origin.z + height * cellSize;

            for (var x = 0; x <= width; x++)
            {
                var worldX = origin.x + x * cellSize;
                Gizmos.DrawLine(new Vector3(worldX, y, origin.z), new Vector3(worldX, y, maxZ));
            }

            for (var row = 0; row <= height; row++)
            {
                var worldZ = origin.z + row * cellSize;
                Gizmos.DrawLine(new Vector3(origin.x, y, worldZ), new Vector3(maxX, y, worldZ));
            }
        }

        private void DrawOccupiedCells()
        {
            if (cells == null)
            {
                return;
            }

            Gizmos.color = occupiedCellColor;
            var size = new Vector3(cellSize * 0.9f, 0.02f, cellSize * 0.9f);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var cell = cells[x, y];
                    if (cell != null && cell.IsOccupied)
                    {
                        var center = GridToWorld(cell.Position);
                        center.y = origin.y + debugGridYOffset;
                        Gizmos.DrawCube(center, size);
                    }
                }
            }
        }
    }
}
