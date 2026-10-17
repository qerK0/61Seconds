using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A* over a room's floor cells (Width x Height plus extra cells), 8 neighbours, no corner cutting, with a
    /// blocked-cell set (debris, the street power line). Cell (x, y) covers grid [x, x+1] x [y, y+1]; its centre is
    /// (x + 0.5, y + 0.5). Paths are smoothed by skipping waypoints while the straight segment stays clear.
    /// Pure C# (Vector2Int / Vector2 only), unit-tested.
    /// </summary>
    public sealed class GridNav
    {
        private const float Diagonal = 1.41421356f;

        /// <summary>Half-width (cells) of the body checked along straight segments: no squeezing past a blocked corner.</summary>
        public const float BodyRadius = 0.2f;

        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        private readonly HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

        public int Width { get; }
        public int Height { get; }

        public GridNav(int width, int height, IEnumerable<Vector2Int> extraCells = null)
        {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++) floor.Add(new Vector2Int(x, y));
            }

            if (extraCells != null)
            {
                foreach (Vector2Int c in extraCells) floor.Add(c);
            }
        }

        /// <summary>Every floor cell (blocked or not).</summary>
        public IEnumerable<Vector2Int> Cells => floor;

        public int CellCount => floor.Count;

        public IReadOnlyCollection<Vector2Int> Blocked => blocked;

        /// <summary>The cell containing a grid point.</summary>
        public static Vector2Int CellOf(Vector2 grid) => new Vector2Int(Mathf.FloorToInt(grid.x), Mathf.FloorToInt(grid.y));

        public static Vector2 Center(Vector2Int cell) => new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        public bool OnFloor(Vector2Int cell) => floor.Contains(cell);
        public bool IsBlocked(Vector2Int cell) => blocked.Contains(cell);
        public bool IsWalkable(Vector2Int cell) => floor.Contains(cell) && !blocked.Contains(cell);

        public void SetBlocked(Vector2Int cell, bool isBlocked)
        {
            if (isBlocked) blocked.Add(cell);
            else blocked.Remove(cell);
        }

        public void SetBlocked(IEnumerable<Vector2Int> cells)
        {
            if (cells == null) return;
            foreach (Vector2Int c in cells) blocked.Add(c);
        }

        public void ClearBlocked() => blocked.Clear();

        /// <summary>A copy with the same floor and blocked cells (planners try cells on it).</summary>
        public GridNav Clone() => new GridNav(this);

        private GridNav(GridNav source)
        {
            Width = source.Width;
            Height = source.Height;
            foreach (Vector2Int c in source.floor) floor.Add(c);
            foreach (Vector2Int c in source.blocked) blocked.Add(c);
        }

        /// <summary>True if one step from a to its neighbour b is allowed (diagonals need both side cells free).</summary>
        public bool CanStep(Vector2Int a, Vector2Int b)
        {
            if (!IsWalkable(b)) return false;
            int dx = b.x - a.x;
            int dy = b.y - a.y;
            if (dx == 0 || dy == 0) return true;
            return IsWalkable(new Vector2Int(a.x + dx, a.y)) && IsWalkable(new Vector2Int(a.x, a.y + dy));
        }

        /// <summary>
        /// Cell path from start to goal (both included), or null if unreachable. The start cell may itself be
        /// blocked (debris fell where someone stands): leaving it is allowed.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            if (start == goal) return new List<Vector2Int> { start };
            if (!IsWalkable(goal)) return null;

            var open = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var cost = new Dictionary<Vector2Int, float> { [start] = 0f };
            var closed = new HashSet<Vector2Int>();
            while (open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < open.Count; i++)
                {
                    float score = cost[open[i]] + Heuristic(open[i], goal);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                Vector2Int current = open[bestIndex];
                open.RemoveAt(bestIndex);
                if (current == goal) return Rebuild(cameFrom, current);
                if (!closed.Add(current)) continue;

                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = current + d;
                    if (closed.Contains(n) || !CanStep(current, n)) continue;
                    float g = cost[current] + (d.x != 0 && d.y != 0 ? Diagonal : 1f);
                    if (cost.TryGetValue(n, out float known) && known <= g) continue;
                    cost[n] = g;
                    cameFrom[n] = current;
                    if (!open.Contains(n)) open.Add(n);
                }
            }

            return null;
        }

        /// <summary>Every cell reachable from start (start included, even if blocked).</summary>
        public HashSet<Vector2Int> ReachableFrom(Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int n = c + d;
                    if (seen.Contains(n) || !CanStep(c, n)) continue;
                    seen.Add(n);
                    queue.Enqueue(n);
                }
            }

            return seen;
        }

        /// <summary>True if b can be reached from a.</summary>
        public bool Connected(Vector2Int a, Vector2Int b) => a == b || ReachableFrom(a).Contains(b);

        /// <summary>
        /// The reachable walkable cell closest to goal (goal itself when it is free and reachable). Ties go to the
        /// cell nearer the start. Returns start if nothing else is reachable.
        /// </summary>
        public Vector2Int NearestReachable(Vector2Int start, Vector2Int goal)
        {
            HashSet<Vector2Int> reachable = ReachableFrom(start);
            if (reachable.Contains(goal) && IsWalkable(goal)) return goal;
            Vector2Int best = start;
            float bestDist = float.MaxValue;
            float bestFromStart = float.MaxValue;
            foreach (Vector2Int c in reachable)
            {
                if (!IsWalkable(c) && c != start) continue;
                float d = (Center(c) - Center(goal)).sqrMagnitude;
                float s = (Center(c) - Center(start)).sqrMagnitude;
                if (d < bestDist - 1e-4f || (Mathf.Abs(d - bestDist) <= 1e-4f && s < bestFromStart))
                {
                    best = c;
                    bestDist = d;
                    bestFromStart = s;
                }
            }

            return best;
        }

        /// <summary>The cell itself if walkable, else the nearest walkable floor cell (approach cells under debris).</summary>
        public Vector2Int NearestFree(Vector2Int cell)
        {
            if (IsWalkable(cell)) return cell;
            Vector2Int best = cell;
            float bestDist = float.MaxValue;
            foreach (Vector2Int c in floor)
            {
                if (blocked.Contains(c)) continue;
                float d = (Center(c) - Center(cell)).sqrMagnitude;
                if (d < bestDist)
                {
                    best = c;
                    bestDist = d;
                }
            }

            return best;
        }

        /// <summary>
        /// True if a body of BodyRadius can move straight from a to b (grid points) over walkable cells only.
        /// Samples the segment and the four corners of the body, so diagonal squeezes between two blocked corners fail.
        /// The cell under a (the start) is always allowed.
        /// </summary>
        public bool LineClear(Vector2 a, Vector2 b, float radius = BodyRadius)
        {
            Vector2Int startCell = CellOf(a);
            float length = (b - a).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(length / 0.1f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                if (!PointClear(p, startCell)) return false;
                if (!PointClear(p + new Vector2(radius, radius), startCell)) return false;
                if (!PointClear(p + new Vector2(radius, -radius), startCell)) return false;
                if (!PointClear(p + new Vector2(-radius, radius), startCell)) return false;
                if (!PointClear(p + new Vector2(-radius, -radius), startCell)) return false;
            }

            return true;
        }

        private bool PointClear(Vector2 p, Vector2Int startCell)
        {
            Vector2Int c = CellOf(p);
            return c == startCell || IsWalkable(c);
        }

        /// <summary>
        /// Grid-space waypoints from one grid point to another (start excluded, the end last). A blocked or
        /// unreachable target goes to the nearest reachable free cell's centre. Smoothed: a corner is cut whenever
        /// the straight segment is clear. Empty if already there.
        /// </summary>
        public List<Vector2> Plan(Vector2 from, Vector2 to)
        {
            var result = new List<Vector2>();
            Vector2Int startCell = CellOf(from);
            Vector2Int goalCell = CellOf(to);
            Vector2 end = to;
            if (!IsWalkable(goalCell) || !ReachableFrom(startCell).Contains(goalCell))
            {
                goalCell = NearestReachable(startCell, goalCell);
                end = Center(goalCell);
            }

            if (startCell == goalCell)
            {
                if ((end - from).sqrMagnitude > 1e-6f) result.Add(end);
                return result;
            }

            List<Vector2Int> cells = FindPath(startCell, goalCell);
            if (cells == null)
            {
                result.Add(end);
                return result;
            }

            var raw = new List<Vector2> { from };
            for (int i = 1; i < cells.Count - 1; i++) raw.Add(Center(cells[i]));
            raw.Add(end);

            // String pulling: from each anchor point jump to the farthest point still in straight sight.
            int anchor = 0;
            while (anchor < raw.Count - 1)
            {
                int far = anchor + 1;
                for (int j = raw.Count - 1; j > anchor + 1; j--)
                {
                    if (LineClear(raw[anchor], raw[j]))
                    {
                        far = j;
                        break;
                    }
                }

                result.Add(raw[far]);
                anchor = far;
            }

            return result;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Math.Abs(a.x - b.x);
            int dy = Math.Abs(a.y - b.y);
            return Math.Max(dx, dy) + (Diagonal - 1f) * Math.Min(dx, dy);
        }

        private static List<Vector2Int> Rebuild(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out Vector2Int previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
