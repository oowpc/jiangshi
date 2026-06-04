using System.Collections.Generic;
using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Pathfinding
{
    public static class PathPlanner
    {
        private static readonly int[] DX = { 1, -1, 0, 0 };
        private static readonly int[] DY = { 0, 0, 1, -1 };

        public static List<Vector3> FindPath(GridManager grid, Vector3 startWorld, Vector3 endWorld, int maxSteps = 4000)
        {
            var start = grid.WorldToGrid(startWorld);
            var end = grid.WorldToGrid(endWorld);

            start = ClampToWalkable(grid, start);
            end = ClampToWalkable(grid, end);

            if (start.Equals(end))
            {
                return new List<Vector3> { grid.GridToWorld(end) };
            }

            var visited = new HashSet<ulong>();
            var cameFrom = new Dictionary<ulong, GridPosition>();
            var frontier = new Queue<GridPosition>();
            frontier.Enqueue(start);
            visited.Add(Pack(start));

            var steps = 0;
            while (frontier.Count > 0 && steps < maxSteps)
            {
                var current = frontier.Dequeue();
                steps++;

                for (var i = 0; i < 4; i++)
                {
                    var nx = current.X + DX[i];
                    var ny = current.Y + DY[i];
                    if (nx < 0 || nx >= grid.Width || ny < 0 || ny >= grid.Height) continue;

                    var next = new GridPosition(nx, ny);
                    var key = Pack(next);
                    if (visited.Contains(key)) continue;

                    var cell = grid.GetCell(next);
                    if (cell == null || !cell.IsWalkable) continue;

                    visited.Add(key);
                    cameFrom[key] = current;
                    frontier.Enqueue(next);

                    if (next.Equals(end))
                    {
                        return ReconstructPath(grid, cameFrom, start, end);
                    }
                }
            }

            return null;
        }

        private static List<Vector3> ReconstructPath(GridManager grid, Dictionary<ulong, GridPosition> cameFrom, GridPosition start, GridPosition end)
        {
            var path = new List<Vector3>();
            var current = end;

            while (!current.Equals(start))
            {
                path.Add(grid.GridToWorld(current));
                current = cameFrom[Pack(current)];
            }

            path.Add(grid.GridToWorld(start));
            path.Reverse();

            if (path.Count > 1)
            {
                path[path.Count - 1] = grid.GridToWorld(end);
            }

            return SimplifyPath(path);
        }

        private static List<Vector3> SimplifyPath(List<Vector3> path)
        {
            if (path.Count <= 2) return path;

            var simplified = new List<Vector3> { path[0] };
            var lastDir = (path[1] - path[0]).normalized;

            for (var i = 2; i < path.Count; i++)
            {
                var dir = (path[i] - simplified[simplified.Count - 1]).normalized;
                if (Vector3.Dot(dir, lastDir) < 0.999f)
                {
                    simplified.Add(path[i - 1]);
                    lastDir = (path[i] - path[i - 1]).normalized;
                }
            }

            simplified.Add(path[path.Count - 1]);
            return simplified;
        }

        private static GridPosition ClampToWalkable(GridManager grid, GridPosition pos)
        {
            if (pos.X < 0 || pos.X >= grid.Width || pos.Y < 0 || pos.Y >= grid.Height)
            {
                return new GridPosition(
                    Mathf.Clamp(pos.X, 0, grid.Width - 1),
                    Mathf.Clamp(pos.Y, 0, grid.Height - 1));
            }

            if (IsWalkable(grid, pos)) return pos;

            for (var radius = 1; radius <= 8; radius++)
            {
                for (var x = pos.X - radius; x <= pos.X + radius; x++)
                {
                    for (var y = pos.Y - radius; y <= pos.Y + radius; y++)
                    {
                        if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height) continue;
                        var candidate = new GridPosition(x, y);
                        if (IsWalkable(grid, candidate)) return candidate;
                    }
                }
            }

            return pos;
        }

        private static bool IsWalkable(GridManager grid, GridPosition pos)
        {
            var cell = grid.GetCell(pos);
            return cell != null && cell.IsWalkable;
        }

        private static ulong Pack(GridPosition p)
        {
            return ((ulong)(uint)p.X << 32) | (uint)p.Y;
        }
    }
}
