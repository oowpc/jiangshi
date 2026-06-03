using System;
using System.Collections.Generic;
using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Pathfinding
{
    public sealed class FlowField : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int targetSearchRadius = 8;

        public static FlowField Instance { get; private set; }

        private Vector2[,] directions;
        private int[,] costs;
        private Queue<GridPosition> open;
        private int width, height;
        private float nextUpdate;
        [SerializeField] private Transform target;

        public void SetGridManager(GridManager gm) => gridManager = gm;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

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
            return TryGetDirection(worldPos, out var direction) ? direction : Vector3.zero;
        }

        public bool TryGetDirection(Vector3 worldPos, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (directions == null || gridManager == null) return false;

            var gp = gridManager.WorldToGrid(worldPos);
            if (gp.X < 0 || gp.X >= width || gp.Y < 0 || gp.Y >= height)
            {
                return TryGetBoundaryDirection(worldPos, gp, out direction);
            }

            var d = directions[gp.X, gp.Y];
            if (d.sqrMagnitude > 0.001f)
            {
                direction = new Vector3(d.x, 0f, d.y).normalized;
                return true;
            }

            return false;
        }

        private bool TryGetBoundaryDirection(Vector3 worldPos, GridPosition outsidePosition, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (width <= 0 || height <= 0) return false;

            var clamped = new GridPosition(
                Mathf.Clamp(outsidePosition.X, 0, width - 1),
                Mathf.Clamp(outsidePosition.Y, 0, height - 1));
            var targetPosition = gridManager.GridToWorld(clamped);
            var toMap = targetPosition - worldPos;
            toMap.y = 0f;

            if (toMap.sqrMagnitude > 0.001f)
            {
                direction = toMap.normalized;
                return true;
            }

            var d = directions[clamped.X, clamped.Y];
            if (d.sqrMagnitude <= 0.001f)
            {
                return false;
            }

            direction = new Vector3(d.x, 0f, d.y).normalized;
            return true;
        }

        private void Recalculate()
        {
            if (gridManager == null || target == null) return;

            width = gridManager.Width;
            height = gridManager.Height;
            EnsureBuffers(width, height);
            ResetBuffers();

            var goal = gridManager.WorldToGrid(target.position);
            if (goal.X < 0 || goal.X >= width || goal.Y < 0 || goal.Y >= height) return;

            if (!SeedGoalCells(goal))
            {
                return;
            }

            while (open.Count > 0)
            {
                var cur = open.Dequeue();
                var curCost = costs[cur.X, cur.Y];

                for (var i = 0; i < 4; i++)
                {
                    var nx = cur.X + DX[i];
                    var ny = cur.Y + DY[i];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    var cell = gridManager.GetCell(new GridPosition(nx, ny));
                    if (cell != null && !cell.IsWalkable) continue;

                    var newCost = curCost + 1;
                    if (newCost >= costs[nx, ny]) continue;

                    costs[nx, ny] = newCost;
                    open.Enqueue(new GridPosition(nx, ny));
                }
            }

            // Build direction field
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (costs[x, y] == int.MaxValue) continue;

                    var bestDir = Vector2.zero;
                    var bestCost = costs[x, y];

                    for (var i = 0; i < 4; i++)
                    {
                        var nx = x + DX[i];
                        var ny = y + DY[i];
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (costs[nx, ny] < bestCost)
                        {
                            bestCost = costs[nx, ny];
                            bestDir = new Vector2(DX[i], DY[i]);
                        }
                    }

                    directions[x, y] = bestDir;
                }
            }
        }

        private bool SeedGoalCells(GridPosition goal)
        {
            var goalCell = gridManager.GetCell(goal);
            if (goalCell != null && goalCell.IsWalkable)
            {
                SeedGoalCell(goal);
                return true;
            }

            var maxRadius = Mathf.Max(1, targetSearchRadius);
            for (var radius = 1; radius <= maxRadius; radius++)
            {
                var seeded = false;
                for (var x = goal.X - radius; x <= goal.X + radius; x++)
                {
                    for (var y = goal.Y - radius; y <= goal.Y + radius; y++)
                    {
                        if (x != goal.X - radius && x != goal.X + radius && y != goal.Y - radius && y != goal.Y + radius)
                        {
                            continue;
                        }

                        var candidate = new GridPosition(x, y);
                        var cell = gridManager.GetCell(candidate);
                        if (cell == null || !cell.IsWalkable)
                        {
                            continue;
                        }

                        SeedGoalCell(candidate);
                        seeded = true;
                    }
                }

                if (seeded)
                {
                    return true;
                }
            }

            return false;
        }

        private void SeedGoalCell(GridPosition goal)
        {
            costs[goal.X, goal.Y] = 0;
            open.Enqueue(goal);
        }

        private void EnsureBuffers(int nextWidth, int nextHeight)
        {
            if (directions != null && costs != null && directions.GetLength(0) == nextWidth && directions.GetLength(1) == nextHeight)
            {
                open ??= new Queue<GridPosition>(nextWidth * nextHeight);
                return;
            }

            directions = new Vector2[nextWidth, nextHeight];
            costs = new int[nextWidth, nextHeight];
            open = new Queue<GridPosition>(nextWidth * nextHeight);
        }

        private void ResetBuffers()
        {
            Array.Clear(directions, 0, directions.Length);
            open.Clear();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    costs[x, y] = int.MaxValue;
                }
            }
        }

        private static readonly int[] DX = { 1, -1, 0, 0 };
        private static readonly int[] DY = { 0, 0, 1, -1 };
    }
}
