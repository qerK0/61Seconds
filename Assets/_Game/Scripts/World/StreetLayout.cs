using System;
using System.Collections.Generic;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// The street finale's grid (pure, shared by the scene builder, StreetQuakeDirector and tests): 20 cells long
    /// (grid x) by 6 deep (grid y), our building's door at the right end, the "Acil Toplanma Alanı" sign at the far
    /// left, façades along the back edge (y = Height). A downed power line crosses the middle (x = LineX) with one
    /// gap; its cut end sparks next to the façades. Parked cars block a few cells.
    /// </summary>
    public static class StreetLayout
    {
        public const int Width = 20;
        public const int Height = 6;

        /// <summary>Column of the downed power line, and the one gap in it.</summary>
        public const int LineX = 10;
        public const int GapY = 2;

        /// <summary>Within this many cells of the building door when the timer hits 0 = crushed by the collapse.</summary>
        public const int CollapseRadius = 5;

        /// <summary>Where the family comes out of the building (right end) and the player spawns.</summary>
        public static readonly Vector2Int DoorCell = new Vector2Int(19, 3);

        /// <summary>The assembly-area sign (exit) at the far left, and where the player stands to use it.</summary>
        public static readonly Vector2Int SignCell = new Vector2Int(0, 3);

        /// <summary>Cells of the downed line (blocked).</summary>
        public static readonly Vector2Int[] PowerLine =
        {
            new Vector2Int(LineX, 0), new Vector2Int(LineX, 1), new Vector2Int(LineX, 3), new Vector2Int(LineX, 4), new Vector2Int(LineX, 5)
        };

        /// <summary>The sparking cut end lying next to the façades (walkable, deadly).</summary>
        public static readonly Vector2Int[] SparkCells = { new Vector2Int(LineX - 1, 5), new Vector2Int(LineX + 1, 5) };

        /// <summary>Parked cars (two cells each, blocked).</summary>
        public static readonly Vector2Int[] CarCells =
        {
            new Vector2Int(15, 1), new Vector2Int(16, 1),
            new Vector2Int(6, 4), new Vector2Int(7, 4),
            new Vector2Int(3, 0), new Vector2Int(4, 0)
        };

        /// <summary>Every statically blocked cell (line + cars).</summary>
        public static IEnumerable<Vector2Int> StaticBlocked()
        {
            foreach (Vector2Int c in PowerLine) yield return c;
            foreach (Vector2Int c in CarCells) yield return c;
        }

        /// <summary>The street grid with its static blocks.</summary>
        public static GridNav Nav()
        {
            var nav = new GridNav(Width, Height);
            nav.SetBlocked(StaticBlocked());
            return nav;
        }

        public static bool IsSpark(Vector2Int cell) => Array.IndexOf(SparkCells, cell) >= 0;

        /// <summary>
        /// Safe at the quake's peak: the open middle of the street, not within 1 cell of the façades (the two rows
        /// along the back edge) or of the power line.
        /// </summary>
        public static bool InOpen(Vector2Int cell)
        {
            bool onStreet = cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
            return onStreet && cell.y <= Height - 3 && Math.Abs(cell.x - LineX) > 1;
        }

        /// <summary>The building collapses behind them: deadly only within CollapseRadius cells of its door.</summary>
        public static bool CollapseKills(Vector2Int playerCell)
        {
            return Math.Max(Math.Abs(playerCell.x - DoorCell.x), Math.Abs(playerCell.y - DoorCell.y)) <= CollapseRadius;
        }

        /// <summary>The 4 cells of a façade drop whose lower-left cell is origin.</summary>
        public static IEnumerable<Vector2Int> DropCells(Vector2Int origin)
        {
            yield return origin;
            yield return origin + new Vector2Int(1, 0);
            yield return origin + new Vector2Int(0, 1);
            yield return origin + new Vector2Int(1, 1);
        }

        /// <summary>
        /// Seeded façade drops: 2x2 blocks in the two rows along the façades, away from the door, the sign, the
        /// line and the sparks; a block is only taken if the door can still reach the sign with it (and the drops
        /// before it) blocked, so a drop never lands on the only path cell. keepClear adds cells that must stay free.
        /// </summary>
        public static List<Vector2Int> PlanFacadeDrops(GridNav nav, int seed, int count, IEnumerable<Vector2Int> keepClear = null)
        {
            var clear = new HashSet<Vector2Int> { DoorCell, SignCell };
            if (keepClear != null) foreach (Vector2Int c in keepClear) clear.Add(c);

            var candidates = new List<Vector2Int>();
            for (int x = 1; x <= Width - 4; x++)
            {
                var origin = new Vector2Int(x, Height - 2);
                bool ok = Math.Abs(x - LineX) > 2 && Math.Abs(x + 1 - LineX) > 2;
                foreach (Vector2Int c in DropCells(origin))
                {
                    if (!nav.OnFloor(c) || clear.Contains(c)) ok = false;
                }

                if (ok) candidates.Add(origin);
            }

            var rng = new System.Random(seed);
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                Vector2Int tmp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = tmp;
            }

            GridNav work = nav.Clone();
            var picked = new List<Vector2Int>();
            var used = new HashSet<Vector2Int>();
            foreach (Vector2Int origin in candidates)
            {
                if (picked.Count >= count) break;
                bool overlaps = false;
                foreach (Vector2Int c in DropCells(origin)) if (used.Contains(c) || used.Contains(c + Vector2Int.left) || used.Contains(c + Vector2Int.right)) overlaps = true;
                if (overlaps) continue;

                var added = new List<Vector2Int>();
                foreach (Vector2Int c in DropCells(origin))
                {
                    if (work.IsBlocked(c)) continue;
                    work.SetBlocked(c, true);
                    added.Add(c);
                }

                if (work.Connected(DoorCell, SignCell))
                {
                    picked.Add(origin);
                    foreach (Vector2Int c in DropCells(origin)) used.Add(c);
                }
                else
                {
                    foreach (Vector2Int c in added) work.SetBlocked(c, false);
                }
            }

            return picked;
        }
    }
}
