using System.Collections.Generic;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Which props are solid and which nav cells they block. A solid prop blocks the cells under its BASE: the
    /// opaque width of the bottom half of its sprite (× its saved scale) is the side of a square footprint whose
    /// front corner is the sprite's bottom-centre pivot (floor decals are centre-pivot). Tall art (wardrobe,
    /// bookshelf) only blocks its base, never its full height. Wall fixtures, ceiling fixtures and signs are
    /// never solid. Resolve() then keeps every room playable: required cells (spawns, anchors, doors, hotspots,
    /// ropes, NPCs) are carved out, and a prop that still cuts something off shrinks to its centre cell (listed).
    /// </summary>
    public static class PropFootprint
    {
        /// <summary>Never block: wall-mounted, ceiling-hung, signs, façade dressing (street cars are static blocks).</summary>
        private static readonly HashSet<string> NonSolid = new HashSet<string>
        {
            "WallLamp", "Valve", "FuseBox", "Sign", "CeilingLamp", "CorridorLamp", "CeilingDebris", "CeilingPlaster",
            "Window", "CorridorWindow", "DoorLintel", "Cables", "HangingCables", "Shaft", "Kerb", "Facade", "OurBuilding",
            "BuildingDoor", "FallenPole", "Elevator", "BalconyRail", "StairEdge", "Rug", "Car", "Debris",
            // The TV sits on its unit (the unit blocks the cell).
            "Tv"
        };

        /// <summary>Base widths are measured on this fraction of the opaque rows, from the bottom.</summary>
        private const float BaseBand = 0.5f;

        private static readonly Dictionary<Sprite, float> widths = new Dictionary<Sprite, float>();

        /// <summary>True if a prop of this name blocks its nav cells.</summary>
        public static bool IsSolid(string propName) => !string.IsNullOrEmpty(propName) && !NonSolid.Contains(propName);

        /// <summary>One solid object of a room with its resolved blocked cells.</summary>
        public sealed class Blocker
        {
            public string Id;
            public Vector2Int Centre;
            public List<Vector2Int> Cells = new List<Vector2Int>();
        }

        /// <summary>Opaque base width (room-local units, scale 1) of a sprite: the widest bottom-band row run.</summary>
        public static float BaseWidth(Sprite sprite)
        {
            if (sprite == null) return 0f;
            if (widths.TryGetValue(sprite, out float cached)) return cached;
            float width = sprite.bounds.size.x;
            string path = AssetDatabase.GetAssetPath(sprite.texture);
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(System.IO.File.ReadAllBytes(path)))
                {
                    // The import may be downscaled: map the sprite rect onto the source pixels.
                    float sx = tex.width / (float)sprite.texture.width, sy = tex.height / (float)sprite.texture.height;
                    Rect r = sprite.rect;
                    int x0 = Mathf.FloorToInt(r.x * sx), x1 = Mathf.CeilToInt(r.xMax * sx), y0 = Mathf.FloorToInt(r.y * sy), y1 = Mathf.CeilToInt(r.yMax * sy);
                    int low = -1, high = -1;
                    for (int y = y0; y < y1 && low < 0; y++) if (RowExtent(tex, y, x0, x1, out _, out _)) low = y;
                    for (int y = y1 - 1; y >= y0 && high < 0; y--) if (RowExtent(tex, y, x0, x1, out _, out _)) high = y;
                    if (low >= 0)
                    {
                        int top = low + Mathf.Max(1, Mathf.RoundToInt((high - low + 1) * BaseBand));
                        int min = int.MaxValue, max = int.MinValue;
                        for (int y = low; y < top; y++)
                        {
                            if (!RowExtent(tex, y, x0, x1, out int a, out int b)) continue;
                            min = Mathf.Min(min, a);
                            max = Mathf.Max(max, b);
                        }

                        if (max >= min) width = (max - min + 1) / sx / sprite.pixelsPerUnit;
                    }
                }

                Object.DestroyImmediate(tex);
            }

            widths[sprite] = width;
            return width;
        }

        private static bool RowExtent(Texture2D tex, int y, int x0, int x1, out int min, out int max)
        {
            min = -1;
            max = -1;
            for (int x = x0; x < x1; x++)
            {
                if (tex.GetPixel(x, y).a < 0.5f) continue;
                if (min < 0) min = x;
                max = x;
            }

            return min >= 0;
        }

        private static PropArtSet Set => SceneKit.Props != null ? SceneKit.Props : AssetDatabase.LoadAssetAtPath<PropArtSet>(PropArt.SetPath);

        /// <summary>Base width of a prop / container art name; unmapped art uses its block width.</summary>
        public static float ArtWidth(string artName, float blockWidth)
        {
            Sprite s = Set != null && !string.IsNullOrEmpty(artName) ? Set.Find(artName) : null;
            return s != null ? BaseWidth(s) : blockWidth;
        }

        /// <summary>
        /// Cells under a square base of side `side` (grid cells): front corner at the pivot for bottom-pivot art,
        /// centred on it for floor decals. The pivot's own cell (clamped into the room) is always included.
        /// </summary>
        public static List<Vector2Int> Cells(BuildingLayout.RoomSpec room, float gx, float gy, float side, bool centrePivot, out Vector2Int centre)
        {
            float half = side * 0.5f;
            float cx = centrePivot ? gx : gx + half, cy = centrePivot ? gy : gy + half;
            GridNav nav = BuildingLayout.NavFor(room);
            centre = Clamp(nav, GridNav.CellOf(new Vector2(cx, cy)));
            var cells = new List<Vector2Int>();
            foreach (Vector2Int c in nav.Cells)
            {
                if (Mathf.Abs(c.x + 0.5f - cx) < half && Mathf.Abs(c.y + 0.5f - cy) < half) cells.Add(c);
            }

            if (!cells.Contains(centre)) cells.Add(centre);
            return cells;
        }

        private static Vector2Int Clamp(GridNav nav, Vector2Int cell) => nav.OnFloor(cell) ? cell : nav.NearestFloor(cell);

        /// <summary>Every solid object of a room with its raw footprint (no carving).</summary>
        public static List<Blocker> Raw(BuildingLayout.RoomSpec room)
        {
            var list = new List<Blocker>();
            foreach (BuildingLayout.PropSpec p in room.Props)
            {
                // A Layout Editor "blocks nav" tick still makes an unknown prop solid; named fixtures never are.
                bool solid = IsSolid(p.Name) || (p.BlocksNav && !NonSolid.Contains(p.Name ?? ""));
                if (!solid) continue;
                float side = ArtWidth(p.Name, p.Scale.x) * p.ObjScale;
                var b = new Blocker { Id = p.Id };
                b.Cells = Cells(room, p.Gx, p.Gy, side, PropArt.IsFloorDecal(p.Name), out b.Centre);
                list.Add(b);
            }

            foreach (BuildingLayout.ContainerSpec c in room.Containers)
            {
                float side = ArtWidth(c.Art, c.Scale.x * 0.5f) * c.ObjScale;
                var b = new Blocker { Id = c.Id };
                b.Cells = Cells(room, c.Gx, c.Gy, side, false, out b.Centre);
                list.Add(b);
            }

            return list;
        }

        /// <summary>Cells that must stay walkable: spawns, anchors, door approaches, hotspot approaches, ropes, NPCs.</summary>
        public static HashSet<Vector2Int> HardCells(BuildingLayout.RoomSpec room)
        {
            var points = new List<Vector2> { room.PlayerSpawn, room.WifeSpawn, room.ChildSpawn };
            foreach (BuildingLayout.AnchorSpec a in room.Anchors) points.Add(new Vector2(a.Gx, a.Gy));
            foreach (BuildingLayout.DoorSpec d in room.Doors) points.Add(BuildingLayout.EdgeInside(room, d.Edge));
            foreach (BuildingLayout.HotspotSpec h in room.Hotspots)
            {
                if (!string.IsNullOrEmpty(h.ChoiceKey)) points.Add(BuildingLayout.HotspotApproach(room, h));
                if (h.Person) points.Add(new Vector2(h.Gx, h.Gy));
            }

            foreach (BuildingLayout.RopeSpec r in room.Ropes) points.Add(new Vector2(r.Gx, r.Gy));
            GridNav nav = BuildingLayout.NavFor(room);
            var cells = new HashSet<Vector2Int>();
            foreach (Vector2 p in points) cells.Add(Clamp(nav, GridNav.CellOf(p)));
            return cells;
        }

        /// <summary>Cells that need a reachable cell on or next to them: container approaches and pickups.</summary>
        public static List<KeyValuePair<Vector2Int, string>> SoftCells(BuildingLayout.RoomSpec room)
        {
            GridNav nav = BuildingLayout.NavFor(room);
            var cells = new List<KeyValuePair<Vector2Int, string>>();
            foreach (BuildingLayout.ContainerSpec c in room.Containers) cells.Add(new KeyValuePair<Vector2Int, string>(Clamp(nav, GridNav.CellOf(BuildingLayout.ContainerApproach(room, c))), "container " + c.Id));
            foreach (BuildingLayout.ItemSpec i in room.Items) cells.Add(new KeyValuePair<Vector2Int, string>(Clamp(nav, GridNav.CellOf(new Vector2(i.Gx, i.Gy))), "item " + i.Id));
            return cells;
        }

        /// <summary>
        /// The resolved blocked cells of a room (cell → object id). Required cells are carved out; while something
        /// is cut off, the biggest footprint shrinks to its centre cell (then loses blocking altogether). Notes name
        /// every shrink so the handoff can list them.
        /// </summary>
        public static Dictionary<Vector2Int, string> Resolve(BuildingLayout.RoomSpec room, List<string> notes = null)
        {
            HashSet<Vector2Int> hard = HardCells(room);
            List<Blocker> blockers = Raw(room);
            foreach (Blocker b in blockers) b.Cells.RemoveAll(hard.Contains);

            var shrunk = new HashSet<Blocker>();
            while (true)
            {
                Dictionary<Vector2Int, string> map = Map(blockers);
                if (Playable(room, map, hard)) return FillPockets(room, map, hard);
                Blocker worst = null;
                foreach (Blocker b in blockers)
                {
                    if (b.Cells.Count == 0) continue;
                    if (worst == null || b.Cells.Count > worst.Cells.Count) worst = b;
                }

                if (worst == null) return map;
                bool single = worst.Cells.Count == 1 && worst.Cells[0] == worst.Centre;
                worst.Cells.Clear();
                if (!single && !hard.Contains(worst.Centre)) worst.Cells.Add(worst.Centre);
                if (shrunk.Add(worst)) notes?.Add($"{room.Id}: '{worst.Id}' {(worst.Cells.Count > 0 ? "shrunk to its centre cell " + worst.Centre : "no longer blocks")} (it cut off a required path).");
            }
        }

        private static Dictionary<Vector2Int, string> Map(List<Blocker> blockers)
        {
            var map = new Dictionary<Vector2Int, string>();
            foreach (Blocker b in blockers) foreach (Vector2Int c in b.Cells) map[c] = b.Id;
            return map;
        }

        /// <summary>Hard cells connected to the player spawn; every soft cell has a reachable cell on or next to it.</summary>
        public static bool Playable(BuildingLayout.RoomSpec room, Dictionary<Vector2Int, string> map, HashSet<Vector2Int> hard)
        {
            return Problems(room, map, hard).Count == 0;
        }

        /// <summary>Readable playability problems of a resolved room (empty = fully connected).</summary>
        public static List<string> Problems(BuildingLayout.RoomSpec room, Dictionary<Vector2Int, string> map, HashSet<Vector2Int> hard)
        {
            var problems = new List<string>();
            GridNav before = BuildingLayout.NavFor(room);
            foreach (Vector2Int c in map.Keys) before.SetBlocked(c, false);
            GridNav after = before.Clone();
            foreach (Vector2Int c in map.Keys) after.SetBlocked(c, true);
            Vector2Int spawn = Clamp(before, GridNav.CellOf(room.PlayerSpawn));
            HashSet<Vector2Int> was = before.ReachableFrom(spawn);
            HashSet<Vector2Int> reach = after.ReachableFrom(spawn);
            foreach (Vector2Int c in hard)
            {
                if (map.ContainsKey(c)) problems.Add($"room '{room.Id}': '{map[c]}' stands on the required cell {c}.");
                else if (was.Contains(c) && !reach.Contains(c)) problems.Add($"room '{room.Id}': furniture cuts off the required cell {c}.");
            }

            foreach (KeyValuePair<Vector2Int, string> s in SoftCells(room))
            {
                bool ok = false, couldBefore = false;
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int n = s.Key + new Vector2Int(dx, dy);
                        couldBefore |= was.Contains(n);
                        ok |= reach.Contains(n) && after.IsWalkable(n);
                    }
                }

                if (couldBefore && !ok) problems.Add($"room '{room.Id}': {s.Value} has no reachable cell next to it.");
            }

            return problems;
        }

        /// <summary>Free cells walled in by furniture are blocked too (nobody can stand there), so rooms stay fully connected.</summary>
        private static Dictionary<Vector2Int, string> FillPockets(BuildingLayout.RoomSpec room, Dictionary<Vector2Int, string> map, HashSet<Vector2Int> hard)
        {
            GridNav before = BuildingLayout.NavFor(room);
            foreach (Vector2Int c in map.Keys) before.SetBlocked(c, false);
            Vector2Int spawn = Clamp(before, GridNav.CellOf(room.PlayerSpawn));
            HashSet<Vector2Int> was = before.ReachableFrom(spawn);
            GridNav after = before.Clone();
            foreach (Vector2Int c in map.Keys) after.SetBlocked(c, true);
            HashSet<Vector2Int> reach = after.ReachableFrom(spawn);
            foreach (Vector2Int c in was)
            {
                if (after.IsWalkable(c) && !reach.Contains(c) && !hard.Contains(c)) map[c] = "pocket";
            }

            return map;
        }
    }
}
