using System;
using System.Collections.Generic;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// Picks which floor cells of a room hold debris after a number of quakes. Pure C#, unit-tested.
    /// - Seeded per (room, quake, checkpoint seed): the same checkpoint always gives the same layout.
    /// - Density per quake count from LevelConfig (+ a bonus per floor below the top), of the free cells.
    /// - Debris NEVER blocks progress: a cell is only kept if every required cell (door approaches, spawns,
    ///   anchors, pickup / container / NPC approaches, rope entries) can still reach the others. Required cells are
    ///   never debris. Bounded attempts per quake.
    /// - Debris only grows: quake n keeps every cell of quake n-1 and adds new ones.
    /// </summary>
    public static class DebrisPlanner
    {
        /// <summary>Attempts per wanted cell before a quake gives up (plus a small constant).</summary>
        public const int AttemptsPerCell = 6;

        /// <summary>Stable seed for (room, quake, checkpoint seed): FNV-1a, never string.GetHashCode.</summary>
        public static int Seed(string roomId, int quakeCount, int checkpointSeed)
        {
            return Hash(roomId, quakeCount.ToString(), checkpointSeed);
        }

        /// <summary>Stable 32-bit hash of a key, a sub-key and a seed.</summary>
        public static int Hash(string key, string subKey, int seed)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in key ?? string.Empty) h = (h ^ c) * 16777619u;
                h = (h ^ '|') * 16777619u;
                foreach (char c in subKey ?? string.Empty) h = (h ^ c) * 16777619u;
                h = (h ^ (uint)seed) * 16777619u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (int)(h & 0x7fffffff);
            }
        }

        /// <summary>Debris share of the free cells after quakeCount quakes, depth floors below the top (0 before any quake).</summary>
        public static float Density(IReadOnlyList<float> perQuake, int quakeCount, float depthBonus, int depth)
        {
            if (quakeCount <= 0 || perQuake == null || perQuake.Count == 0) return 0f;
            float baseDensity = perQuake[Math.Min(quakeCount, perQuake.Count - 1)];
            return Mathf.Clamp01(baseDensity + Math.Max(0, depth) * depthBonus);
        }

        /// <summary>Debris cells for the room after quakeCount quakes (a superset of the result for quakeCount - 1).</summary>
        public static HashSet<Vector2Int> Plan(GridNav nav, IEnumerable<Vector2Int> required, string roomId, int quakeCount, int checkpointSeed,
            IReadOnlyList<float> perQuake, float depthBonus, int depth)
        {
            var placed = new HashSet<Vector2Int>();
            if (nav == null || quakeCount <= 0) return placed;

            GridNav work = nav.Clone();
            var requiredSet = new HashSet<Vector2Int>();
            if (required != null)
            {
                foreach (Vector2Int r in required) requiredSet.Add(nav.NearestFloor(r));
            }

            List<List<Vector2Int>> groups = ConnectedGroups(work, requiredSet);

            // Candidates in a fixed order (x, then y) so the seeded picks never depend on hash-set order.
            var candidates = new List<Vector2Int>();
            foreach (Vector2Int c in nav.Cells)
            {
                if (nav.IsWalkable(c) && !requiredSet.Contains(c)) candidates.Add(c);
            }

            candidates.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            for (int quake = 1; quake <= quakeCount; quake++)
            {
                int wanted = Mathf.RoundToInt(Density(perQuake, quake, depthBonus, depth) * candidates.Count);
                if (wanted <= placed.Count) continue;

                var pool = new List<Vector2Int>();
                foreach (Vector2Int c in candidates) if (!placed.Contains(c)) pool.Add(c);
                var rng = new System.Random(Seed(roomId, quake, checkpointSeed));
                int attempts = (wanted - placed.Count) * AttemptsPerCell + 10;
                while (placed.Count < wanted && attempts-- > 0 && pool.Count > 0)
                {
                    int index = rng.Next(pool.Count);
                    Vector2Int cell = pool[index];
                    pool[index] = pool[pool.Count - 1];
                    pool.RemoveAt(pool.Count - 1);

                    work.SetBlocked(cell, true);
                    if (StillConnected(work, groups)) placed.Add(cell);
                    else work.SetBlocked(cell, false);
                }
            }

            return placed;
        }

        /// <summary>
        /// Extra rubble (street façade drops) that may block on top of the planned debris: each extra cell is kept
        /// only if the required cells stay connected, so even scripted rubble never blocks progress.
        /// </summary>
        public static List<Vector2Int> SafeExtras(GridNav nav, IEnumerable<Vector2Int> required, IEnumerable<Vector2Int> planned, IEnumerable<Vector2Int> extras)
        {
            var accepted = new List<Vector2Int>();
            if (nav == null || extras == null) return accepted;
            var requiredSet = new HashSet<Vector2Int>();
            if (required != null) foreach (Vector2Int r in required) requiredSet.Add(nav.NearestFloor(r));
            List<List<Vector2Int>> groups = ConnectedGroups(nav, requiredSet);
            GridNav work = nav.Clone();
            if (planned != null) work.SetBlocked(planned);

            var ordered = new List<Vector2Int>(extras);
            ordered.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            foreach (Vector2Int cell in ordered)
            {
                if (requiredSet.Contains(cell) || !work.IsWalkable(cell)) continue;
                work.SetBlocked(cell, true);
                if (StillConnected(work, groups)) accepted.Add(cell);
                else work.SetBlocked(cell, false);
            }

            return accepted;
        }

        /// <summary>True if every group of required cells is still mutually reachable.</summary>
        public static bool StillConnected(GridNav nav, List<List<Vector2Int>> groups)
        {
            foreach (List<Vector2Int> group in groups)
            {
                if (group.Count < 2) continue;
                HashSet<Vector2Int> reach = nav.ReachableFrom(group[0]);
                for (int i = 1; i < group.Count; i++)
                {
                    if (!reach.Contains(group[i])) return false;
                }
            }

            return true;
        }

        /// <summary>Required cells grouped by what reaches what before any debris (normally one group).</summary>
        public static List<List<Vector2Int>> ConnectedGroups(GridNav nav, IEnumerable<Vector2Int> required)
        {
            var groups = new List<List<Vector2Int>>();
            var assigned = new HashSet<Vector2Int>();
            var ordered = new List<Vector2Int>(required);
            ordered.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            foreach (Vector2Int r in ordered)
            {
                if (assigned.Contains(r)) continue;
                HashSet<Vector2Int> reach = nav.ReachableFrom(r);
                var group = new List<Vector2Int>();
                foreach (Vector2Int other in ordered)
                {
                    if (!assigned.Contains(other) && reach.Contains(other))
                    {
                        group.Add(other);
                        assigned.Add(other);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }
    }

    /// <summary>
    /// Which wall segments are cracked. Each segment gets a fixed seeded value (room, segment, checkpoint seed, no
    /// quake count), and is cracked once the chance for the current quake count passes it, so cracks only increase.
    /// Pure C#, unit-tested.
    /// </summary>
    public static class CrackRules
    {
        /// <summary>Crack chance after quakeCount quakes, depth floors below the top (0 before any quake).</summary>
        public static float Chance(IReadOnlyList<float> perQuake, int quakeCount, float depthBonus, int depth)
        {
            if (quakeCount <= 0 || perQuake == null || perQuake.Count == 0) return 0f;
            float baseChance = perQuake[Math.Min(quakeCount, perQuake.Count - 1)];
            return Mathf.Clamp01(baseChance + Math.Max(0, depth) * depthBonus);
        }

        /// <summary>Fixed value in [0, 1) for one segment of one room.</summary>
        public static float SegmentRoll(string roomId, string segmentKey, int checkpointSeed)
        {
            return (DebrisPlanner.Hash(roomId, "crack:" + segmentKey, checkpointSeed) % 10000) / 10000f;
        }

        public static bool IsCracked(string roomId, string segmentKey, int checkpointSeed, float chance)
        {
            return chance > 0f && SegmentRoll(roomId, segmentKey, checkpointSeed) < chance;
        }
    }
}
