using System.Collections.Generic;
using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Pathfinding
{
    public sealed class FlowField : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float updateInterval = 1f;

        private Vector2[,] directions;
        private int width, height;
        private float nextUpdate;
        [SerializeField] private Transform target;

        public void SetGridManager(GridManager gm) => gridManager = gm;

        public void SetTarget(Transform t)
        {
            target = t;
            Recalculate();
        }

        private void Start()
        {
            if (target != null) Recalculate();
        }

        private void Update()
        {
            if (target == null || gridManager == null) return;
            if (Time.time < nextUpdate) return;
            nextUpdate = Time.time + updateInterval;
            Recalculate();
        }

        public Vector3 GetDirection(Vector3 worldPos)
        {
            if (directions == null || gridManager == null) return Vector3.zero;
            var gp = gridManager.WorldToGrid(worldPos);
            if (gp.X < 0 || gp.X >= width || gp.Y < 0 || gp.Y >= height)
            {
                return GetFallbackDirection(worldPos);
            }

            var d = directions[gp.X, gp.Y];
            if (d.sqrMagnitude > 0.001f)
            {
                return new Vector3(d.x, 0f, d.y).normalized;
            }

            return GetFallbackDirection(worldPos);
        }

        private Vector3 GetFallbackDirection(Vector3 worldPos)
        {
            var fallbackTarget = target != null
                ? target.position
                : gridManager.GridToWorld(new GridPosition(width / 2, height / 2));

            var dir = fallbackTarget - worldPos;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;
        }

        private void Recalculate()
        {
            if (gridManager == null || target == null) return;

            width = gridManager.Width;
            height = gridManager.Height;
            directions = new Vector2[width, height];

            var cost = new int[width, height];
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    cost[x, y] = int.MaxValue;

            var goal = gridManager.WorldToGrid(target.position);
            if (goal.X < 0 || goal.X >= width || goal.Y < 0 || goal.Y >= height) return;

            cost[goal.X, goal.Y] = 0;
            var open = new Queue<GridPosition>();
            open.Enqueue(goal);

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                var curCost = cost[cur.X, cur.Y];

                for (var i = 0; i < 4; i++)
                {
                    var nx = cur.X + DX[i];
                    var ny = cur.Y + DY[i];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    var cell = gridManager.GetCell(new GridPosition(nx, ny));
                    if (cell != null && !cell.IsWalkable) continue;

                    var newCost = curCost + 1;
                    if (newCost >= cost[nx, ny]) continue;

                    cost[nx, ny] = newCost;
                    open.Enqueue(new GridPosition(nx, ny));
                }
            }

            // Build direction field
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (cost[x, y] == int.MaxValue) continue;

                    var bestDir = Vector2.zero;
                    var bestCost = cost[x, y];

                    for (var i = 0; i < 4; i++)
                    {
                        var nx = x + DX[i];
                        var ny = y + DY[i];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (cost[nx, ny] < bestCost)
                        {
                            bestCost = cost[nx, ny];
                            bestDir = new Vector2(DX[i], DY[i]);
                        }
                    }

                    directions[x, y] = bestDir;
                }
            }
        }

        private static readonly int[] DX = { 1, -1, 0, 0 };
        private static readonly int[] DY = { 0, 0, 1, -1 };
    }
}
