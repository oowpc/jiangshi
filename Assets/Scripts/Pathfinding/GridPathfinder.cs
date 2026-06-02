using System.Collections.Generic;
using Jiangshi.Grid;
using UnityEngine;

namespace Jiangshi.Pathfinding
{
    public static class GridPathfinder
    {
        private static readonly GridPosition[] Dirs = {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        public static List<Vector3> FindPath(GridManager grid, Vector3 from, Vector3 to, int maxSteps = 200)
        {
            if (grid == null) return null;

            var start = grid.WorldToGrid(from);
            var end = grid.WorldToGrid(to);

            if (start.X == end.X && start.Y == end.Y) return null;

            var open = new SortedList<float, GridPosition>(new DuplicateKeyComparer());
            var cameFrom = new Dictionary<long, long>();
            var gScore = new Dictionary<long, float>();

            var startKey = Key(start);
            gScore[startKey] = 0;
            open.Add(Heuristic(start, end), start);

            while (open.Count > 0 && gScore.Count < maxSteps)
            {
                var current = open.Values[0];
                open.RemoveAt(0);
                var currentKey = Key(current);

                if (current.X == end.X && current.Y == end.Y)
                    return ReconstructPath(grid, cameFrom, currentKey, startKey);

                foreach (var dir in Dirs)
                {
                    var neighbor = new GridPosition(current.X + dir.X, current.Y + dir.Y);
                    var cell = grid.GetCell(neighbor);

                    if (cell == null || !cell.IsWalkable) continue;

                    var neighborKey = Key(neighbor);
                    var tentativeG = gScore[currentKey] + 1f;

                    if (gScore.TryGetValue(neighborKey, out var existing) && tentativeG >= existing)
                        continue;

                    gScore[neighborKey] = tentativeG;
                    cameFrom[neighborKey] = currentKey;
                    open.Add(tentativeG + Heuristic(neighbor, end), neighbor);
                }
            }

            return null; // No path found
        }

        private static List<Vector3> ReconstructPath(GridManager grid, Dictionary<long, long> cameFrom, long currentKey, long startKey)
        {
            var path = new List<Vector3>();
            while (currentKey != startKey)
            {
                var pos = FromKey(currentKey);
                path.Add(grid.GridToWorld(pos));
                if (!cameFrom.TryGetValue(currentKey, out currentKey)) break;
            }
            path.Reverse();
            return path;
        }

        private static float Heuristic(GridPosition a, GridPosition b)
        {
            return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
        }

        private static long Key(GridPosition p) => (long)p.X * 100000 + p.Y;
        private static GridPosition FromKey(long k) => new((int)(k / 100000), (int)(k % 100000));

        private class DuplicateKeyComparer : IComparer<float>
        {
            public int Compare(float x, float y)
            {
                var c = x.CompareTo(y);
                return c == 0 ? 1 : c; // Allow duplicate keys
            }
        }
    }
}
