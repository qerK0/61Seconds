using System.Collections.Generic;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;
using PlacedObject = SixtyOne.Editor.RoomLayoutOverrides.PlacedObject;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Applies RoomLayoutOverrides to the BuildingLayout specs (move / flip / art / data / remove / add), checks the
    /// result like DebrisPlanner does (no required cell blocked, everything still reachable), and turns a room's
    /// placed objects back into the minimal list of edits against the code layout.
    /// </summary>
    public static class LayoutOverrides
    {
        public const string AssetPath = BuildingLevelsGenerator.DataRoot + "/Layout/RoomLayoutOverrides.asset";

        private const float Epsilon = 0.01f;

        /// <summary>The overrides asset, or null when nothing was saved yet.</summary>
        public static RoomLayoutOverrides Load() => AssetDatabase.LoadAssetAtPath<RoomLayoutOverrides>(AssetPath);

        /// <summary>The overrides asset, created on first use.</summary>
        public static RoomLayoutOverrides LoadOrCreate()
        {
            RoomLayoutOverrides asset = Load();
            if (asset != null) return asset;
            AssetUtil.EnsureFolder(BuildingLevelsGenerator.DataRoot + "/Layout");
            asset = ScriptableObject.CreateInstance<RoomLayoutOverrides>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>BuildingLayout rooms with the saved overrides applied (what Build Test Scene builds).</summary>
        public static List<BuildingLayout.RoomSpec> Rooms()
        {
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            Apply(rooms, Load());
            return rooms;
        }

        // ---------- apply ----------

        /// <summary>Applies every room entry to its room spec; unknown room ids are ignored (reported by Check).</summary>
        public static void Apply(List<BuildingLayout.RoomSpec> rooms, RoomLayoutOverrides overrides)
        {
            if (overrides != null)
            {
                foreach (RoomLayoutOverrides.RoomEntry entry in overrides.Rooms)
                {
                    BuildingLayout.RoomSpec room = rooms.Find(r => r.Id == entry.roomId);
                    if (room == null) continue;
                    foreach (PlacedObject o in entry.objects) ApplyOne(room, o);
                }
            }

            // Code props can block too (the Flat 10 floor hole), so every room gets its blockers.
            foreach (BuildingLayout.RoomSpec room in rooms)
            {
                foreach (Vector2Int cell in BlockerCells(room).Keys)
                {
                    if (!room.Blocked.Contains(cell)) room.Blocked.Add(cell);
                }
            }
        }

        private static void ApplyOne(BuildingLayout.RoomSpec room, PlacedObject o)
        {
            if (o == null || string.IsNullOrEmpty(o.id)) return;
            switch (o.kind)
            {
                case LayoutKind.Prop:
                {
                    BuildingLayout.PropSpec p = room.Props.Find(x => x.Id == o.id);
                    if (o.removed) { if (p != null) room.Props.Remove(p); return; }
                    if (p == null) room.Props.Add(p = new BuildingLayout.PropSpec { Id = o.id });
                    p.Name = string.IsNullOrEmpty(o.propName) ? p.Name ?? o.id : o.propName;
                    p.Gx = o.gx;
                    p.Gy = o.gy;
                    p.Lift = o.lift;
                    p.FlipX = o.flipX;
                    p.BlocksNav = o.blocksNav;
                    p.PropId = o.hazardProp ? (string.IsNullOrEmpty(o.hazardPropId) ? o.id : o.hazardPropId) : null;
                    p.Topple = o.topple;
                    p.Drop = o.drop;
                    p.Scale = o.scale;
                    p.Color = o.tint;
                    return;
                }
                case LayoutKind.Pickup:
                case LayoutKind.HiddenItem:
                {
                    BuildingLayout.ItemSpec i = room.Items.Find(x => x.Id == o.id);
                    if (o.removed) { if (i != null) room.Items.Remove(i); return; }
                    if (i == null) room.Items.Add(i = new BuildingLayout.ItemSpec { Id = o.id });
                    i.ItemId = o.itemId;
                    i.Gx = o.gx;
                    i.Gy = o.gy;
                    i.Cost = o.cost;
                    i.Borrowed = o.borrowed;
                    i.Surface = o.surface;
                    i.PrepOnly = o.prepOnly;
                    i.Hidden = o.kind == LayoutKind.HiddenItem;
                    i.FlipX = o.flipX;
                    i.Color = o.tint;
                    return;
                }
                case LayoutKind.Container:
                {
                    BuildingLayout.ContainerSpec c = room.Containers.Find(x => x.Id == o.id);
                    if (o.removed) { if (c != null) room.Containers.Remove(c); return; }
                    if (c == null)
                    {
                        c = BuildingLayout.ContainerPreset(o.id, string.IsNullOrEmpty(o.containerType) ? "drawer" : o.containerType);
                        room.Containers.Add(c);
                    }
                    else if (!string.IsNullOrEmpty(o.containerType) && c.NameKey != "container." + o.containerType)
                    {
                        BuildingLayout.ContainerSpec preset = BuildingLayout.ContainerPreset(o.id, o.containerType);
                        c.NameKey = preset.NameKey;
                        c.Scale = preset.Scale;
                    }

                    if (!string.IsNullOrEmpty(o.propName)) c.Art = o.propName;
                    c.Gx = o.gx;
                    c.Gy = o.gy;
                    c.Items = o.contents.ToArray();
                    c.Hidden = o.hiddenContents.ToArray();
                    c.Seconds = o.searchSeconds;
                    c.Cost = o.buildingCost;
                    c.Borrowed = o.borrowed;
                    c.PrepOnly = o.prepOnly;
                    c.FlipX = o.flipX;
                    c.BlocksNav = o.blocksNav;
                    return;
                }
                case LayoutKind.Light:
                {
                    BuildingLayout.LightSpec l = room.Lights.Find(x => x.Name == o.id);
                    if (o.removed) { if (l != null) room.Lights.Remove(l); return; }
                    if (l == null) room.Lights.Add(l = new BuildingLayout.LightSpec { Name = o.id });
                    l.Gx = o.gx;
                    l.Gy = o.gy;
                    l.Lift = o.lift;
                    l.Radius = o.radius;
                    l.Color = o.lightColor;
                    return;
                }
            }
        }

        /// <summary>Cells blocked by furniture that blocks nav, with the blocking object's id.</summary>
        public static Dictionary<Vector2Int, string> BlockerCells(BuildingLayout.RoomSpec room)
        {
            var cells = new Dictionary<Vector2Int, string>();
            foreach (BuildingLayout.PropSpec p in room.Props)
            {
                if (p.BlocksNav) cells[GridNav.CellOf(new Vector2(p.Gx, p.Gy))] = p.Id;
            }

            foreach (BuildingLayout.ContainerSpec c in room.Containers)
            {
                if (c.BlocksNav) cells[GridNav.CellOf(new Vector2(c.Gx, c.Gy))] = c.Id;
            }

            return cells;
        }

        // ---------- checks ----------

        /// <summary>Required points (cells debris may never cover), each with a readable label.</summary>
        public static List<KeyValuePair<Vector2Int, string>> RequiredPoints(BuildingLayout.RoomSpec room)
        {
            var points = new List<KeyValuePair<Vector2, string>>
            {
                new KeyValuePair<Vector2, string>(room.PlayerSpawn, "player spawn"),
                new KeyValuePair<Vector2, string>(room.WifeSpawn, "wife spawn"),
                new KeyValuePair<Vector2, string>(room.ChildSpawn, "child spawn")
            };
            foreach (BuildingLayout.AnchorSpec a in room.Anchors) points.Add(new KeyValuePair<Vector2, string>(new Vector2(a.Gx, a.Gy), "anchor " + a.Id));
            foreach (BuildingLayout.DoorSpec d in room.Doors) points.Add(new KeyValuePair<Vector2, string>(BuildingLayout.EdgeInside(room, d.Edge), "door approach → " + d.TargetRoom));
            foreach (BuildingLayout.HotspotSpec h in room.Hotspots)
            {
                if (!string.IsNullOrEmpty(h.ChoiceKey)) points.Add(new KeyValuePair<Vector2, string>(BuildingLayout.HotspotApproach(room, h), "hotspot " + h.Name));
            }

            foreach (BuildingLayout.ItemSpec i in room.Items) points.Add(new KeyValuePair<Vector2, string>(new Vector2(i.Gx, i.Gy), "item " + i.Id));
            foreach (BuildingLayout.RopeSpec r in room.Ropes) points.Add(new KeyValuePair<Vector2, string>(new Vector2(r.Gx, r.Gy), "rope " + r.Id));
            foreach (BuildingLayout.ContainerSpec c in room.Containers) points.Add(new KeyValuePair<Vector2, string>(BuildingLayout.ContainerApproach(room, c), "container approach " + c.Id));
            var cells = new List<KeyValuePair<Vector2Int, string>>();
            foreach (KeyValuePair<Vector2, string> p in points) cells.Add(new KeyValuePair<Vector2Int, string>(GridNav.CellOf(p.Key), p.Value));
            return cells;
        }

        /// <summary>
        /// Problems in one room after the overrides: hand-placed furniture on a required cell, or cutting a required
        /// cell off from the others (same rules as DebrisPlanner). Messages name the room and the object id.
        /// </summary>
        public static List<string> Check(BuildingLayout.RoomSpec room)
        {
            var problems = new List<string>();
            Dictionary<Vector2Int, string> blockers = BlockerCells(room);
            if (blockers.Count == 0) return problems;

            List<KeyValuePair<Vector2Int, string>> required = RequiredPoints(room);
            foreach (KeyValuePair<Vector2Int, string> r in required)
            {
                // A container standing on its own approach cell is fine: the player uses the nearest free neighbour.
                if (blockers.TryGetValue(r.Key, out string id) && r.Value != "container approach " + id)
                {
                    problems.Add($"room '{room.Id}': '{id}' blocks the {r.Value} cell {r.Key}.");
                }
            }

            // Reachability: compare against the room without the hand-placed blockers.
            GridNav before = BuildingLayout.NavFor(room);
            foreach (Vector2Int cell in blockers.Keys) before.SetBlocked(cell, false);
            GridNav after = BuildingLayout.NavFor(room);
            var cells = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, string> r in required)
            {
                if (!blockers.ContainsKey(r.Key)) cells.Add(r.Key);
                else if (blockers[r.Key] != null && r.Value == "container approach " + blockers[r.Key])
                {
                    // Its own approach: some free neighbour of it must stay reachable instead.
                    foreach (Vector2Int n in new[] { r.Key + Vector2Int.left, r.Key + Vector2Int.down, r.Key + Vector2Int.right, r.Key + Vector2Int.up })
                    {
                        if (after.IsWalkable(n))
                        {
                            cells.Add(n);
                            break;
                        }
                    }
                }
            }

            string ids = string.Join(", ", new List<string>(blockers.Values));
            foreach (List<Vector2Int> group in DebrisPlanner.ConnectedGroups(before, cells))
            {
                if (group.Count < 2) continue;
                HashSet<Vector2Int> reach = after.ReachableFrom(group[0]);
                foreach (Vector2Int cell in group)
                {
                    if (reach.Contains(cell)) continue;
                    string label = required.Find(r => r.Key == cell).Value;
                    problems.Add($"room '{room.Id}': hand-placed furniture ({ids}) cuts off the {label} cell {cell}.");
                }
            }

            return problems;
        }

        /// <summary>Every override problem in the building (unknown rooms, missing items, blocked / cut-off cells).</summary>
        public static List<string> CheckAll()
        {
            var problems = new List<string>();
            RoomLayoutOverrides overrides = Load();
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            var itemIds = new HashSet<string>(BuildingLevelsGenerator.ItemIds);
            foreach (RoomLayoutOverrides.RoomEntry entry in overrides != null ? overrides.Rooms : new List<RoomLayoutOverrides.RoomEntry>())
            {
                if (rooms.Find(r => r.Id == entry.roomId) == null) problems.Add($"room '{entry.roomId}': no such room in BuildingLayout.");
                foreach (PlacedObject o in entry.objects)
                {
                    if (o.removed) continue;
                    if ((o.kind == LayoutKind.Pickup || o.kind == LayoutKind.HiddenItem) && !itemIds.Contains(o.itemId))
                    {
                        problems.Add($"room '{entry.roomId}': '{o.id}' grants unknown item '{o.itemId}'.");
                    }

                    if (o.kind == LayoutKind.Container)
                    {
                        foreach (string id in o.contents)
                        {
                            if (!itemIds.Contains(id)) problems.Add($"room '{entry.roomId}': container '{o.id}' holds unknown item '{id}'.");
                        }
                    }
                }
            }

            Apply(rooms, overrides);
            foreach (BuildingLayout.RoomSpec room in rooms) problems.AddRange(Check(room));
            return problems;
        }

        // ---------- tagging (builder) and saving (editor window) ----------

        public static void Tag(GameObject go, BuildingLayout.PropSpec p, float effectiveLift)
        {
            LayoutObject tag = Ensure(go, p.Id, LayoutKind.Prop);
            tag.Lift = effectiveLift;
            tag.FlipX = p.FlipX;
            tag.PropName = p.Name;
            tag.BlocksNav = p.BlocksNav;
            tag.HazardProp = !string.IsNullOrEmpty(p.PropId);
            tag.HazardPropId = p.PropId;
            tag.Topple = p.Topple;
            tag.Drop = p.Drop;
            tag.Scale = p.Scale;
            tag.Tint = p.Color;
        }

        public static void Tag(GameObject go, BuildingLayout.ItemSpec i)
        {
            LayoutObject tag = Ensure(go, i.Id, i.Hidden ? LayoutKind.HiddenItem : LayoutKind.Pickup);
            tag.ItemId = i.ItemId;
            tag.Cost = i.Cost;
            tag.Borrowed = i.Borrowed;
            tag.Surface = i.Surface;
            tag.PrepOnly = i.PrepOnly;
            tag.FlipX = i.FlipX;
            tag.Tint = i.Color;
        }

        public static void Tag(GameObject go, BuildingLayout.ContainerSpec c)
        {
            LayoutObject tag = Ensure(go, c.Id, LayoutKind.Container);
            tag.ContainerType = c.NameKey != null && c.NameKey.StartsWith("container.") ? c.NameKey.Substring("container.".Length) : c.NameKey;
            tag.PropName = c.Art;
            tag.Contents = new List<string>(c.Items);
            tag.HiddenContents = new List<string>(c.Hidden);
            tag.SearchSeconds = c.Seconds;
            tag.BuildingCost = c.Cost;
            tag.Borrowed = c.Borrowed;
            tag.PrepOnly = c.PrepOnly;
            tag.FlipX = c.FlipX;
            tag.BlocksNav = c.BlocksNav;
        }

        public static void Tag(GameObject go, BuildingLayout.LightSpec l)
        {
            LayoutObject tag = Ensure(go, l.Name, LayoutKind.Light);
            tag.Lift = l.Lift;
            tag.Radius = l.Radius;
            tag.LightColor = l.Color;
        }

        private static LayoutObject Ensure(GameObject go, string id, LayoutKind kind)
        {
            LayoutObject tag = go.GetComponent<LayoutObject>();
            if (tag == null) tag = go.AddComponent<LayoutObject>();
            tag.Id = id;
            tag.Kind = kind;
            tag.FromCode = true;
            tag.Proxy = false;
            return tag;
        }

        /// <summary>The placed object a scene tag describes (grid position from its room-local position).</summary>
        public static PlacedObject FromTag(LayoutObject tag)
        {
            Vector3 local = tag.transform.localPosition - new Vector3(0f, tag.Lift, 0f);
            Vector2 grid = SixtyOne.Iso.IsoMath.WorldToGrid(local);
            return new PlacedObject
            {
                id = tag.Id,
                kind = tag.Kind,
                gx = Round(grid.x),
                gy = Round(grid.y),
                lift = tag.Lift,
                flipX = tag.FlipX,
                propName = tag.PropName,
                blocksNav = tag.BlocksNav,
                itemId = tag.ItemId,
                cost = tag.Cost,
                borrowed = tag.Borrowed,
                surface = tag.Surface,
                prepOnly = tag.PrepOnly,
                containerType = tag.ContainerType,
                contents = new List<string>(tag.Contents),
                hiddenContents = new List<string>(tag.HiddenContents),
                searchSeconds = tag.SearchSeconds,
                buildingCost = tag.BuildingCost,
                hazardProp = tag.HazardProp,
                hazardPropId = tag.HazardPropId,
                topple = tag.Topple,
                drop = tag.Drop,
                radius = tag.Radius,
                lightColor = tag.LightColor,
                scale = tag.Scale,
                tint = tag.Tint
            };
        }

        private static float Round(float v) => Mathf.Round(v * 100f) / 100f;

        /// <summary>
        /// The edits that turn the code room into the placed objects: changed code objects, added objects and
        /// removed code objects. Unchanged code objects are left out, so later code changes still reach them.
        /// </summary>
        public static List<PlacedObject> Diff(BuildingLayout.RoomSpec codeRoom, IEnumerable<PlacedObject> placed)
        {
            var result = new List<PlacedObject>();
            var seen = new HashSet<string>();
            foreach (PlacedObject o in placed)
            {
                seen.Add(Key(o.kind, o.id));
                PlacedObject code = CodeObject(codeRoom, o.kind, o.id);
                if (code == null || !Same(code, o)) result.Add(o);
            }

            foreach (PlacedObject code in CodeObjects(codeRoom))
            {
                if (seen.Contains(Key(code.kind, code.id))) continue;
                result.Add(new PlacedObject { id = code.id, kind = code.kind, removed = true });
            }

            result.Sort((a, b) => a.kind != b.kind ? a.kind.CompareTo(b.kind) : string.CompareOrdinal(a.id, b.id));
            return result;
        }

        private static string Key(LayoutKind kind, string id) => (kind == LayoutKind.HiddenItem ? LayoutKind.Pickup : kind) + "/" + id;

        private static PlacedObject CodeObject(BuildingLayout.RoomSpec room, LayoutKind kind, string id)
        {
            foreach (PlacedObject o in CodeObjects(room))
            {
                if (Key(o.kind, o.id) == Key(kind, id)) return o;
            }

            return null;
        }

        /// <summary>The code objects of a room in placed-object form (lift as the builder applies it).</summary>
        public static IEnumerable<PlacedObject> CodeObjects(BuildingLayout.RoomSpec room)
        {
            foreach (BuildingLayout.PropSpec p in room.Props)
            {
                var go = new GameObject();
                try
                {
                    Tag(go, p, SceneKit.EffectiveLift(p.Name, p.Lift));
                    go.transform.localPosition = SixtyOne.Iso.IsoMath.GridToWorld(p.Gx, p.Gy) + new Vector3(0f, go.GetComponent<LayoutObject>().Lift, 0f);
                    yield return FromTag(go.GetComponent<LayoutObject>());
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }

            foreach (BuildingLayout.ItemSpec i in room.Items) yield return Placed(i.Gx, i.Gy, go => Tag(go, i));
            foreach (BuildingLayout.ContainerSpec c in room.Containers) yield return Placed(c.Gx, c.Gy, go => Tag(go, c));
            foreach (BuildingLayout.LightSpec l in room.Lights) yield return Placed(l.Gx, l.Gy, go => Tag(go, l), l.Lift);
        }

        private static PlacedObject Placed(float gx, float gy, System.Action<GameObject> tag, float lift = 0f)
        {
            var go = new GameObject();
            try
            {
                tag(go);
                go.transform.localPosition = SixtyOne.Iso.IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
                return FromTag(go.GetComponent<LayoutObject>());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static bool Same(PlacedObject a, PlacedObject b)
        {
            if (a.kind != b.kind) return false;
            if (!Near(a.gx, b.gx) || !Near(a.gy, b.gy) || !Near(a.lift, b.lift) || a.flipX != b.flipX || a.blocksNav != b.blocksNav) return false;
            switch (a.kind)
            {
                case LayoutKind.Prop:
                    return a.propName == b.propName && a.hazardProp == b.hazardProp && (a.hazardPropId ?? "") == (b.hazardPropId ?? "")
                        && Near(a.topple, b.topple) && Near(a.drop, b.drop);
                case LayoutKind.Pickup:
                case LayoutKind.HiddenItem:
                    return a.itemId == b.itemId && Near(a.cost, b.cost) && a.borrowed == b.borrowed && a.surface == b.surface && a.prepOnly == b.prepOnly;
                case LayoutKind.Container:
                    return a.containerType == b.containerType && (a.propName ?? "") == (b.propName ?? "") && SameList(a.contents, b.contents)
                        && SameList(a.hiddenContents, b.hiddenContents) && Near(a.searchSeconds, b.searchSeconds) && Near(a.buildingCost, b.buildingCost)
                        && a.borrowed == b.borrowed && a.prepOnly == b.prepOnly;
                case LayoutKind.Light:
                    return Near(a.radius, b.radius) && a.lightColor == b.lightColor;
            }

            return true;
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < Epsilon;

        private static bool SameList(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
    }
}
