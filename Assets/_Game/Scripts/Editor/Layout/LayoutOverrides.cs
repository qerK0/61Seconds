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
                    foreach (PlacedObject o in entry.objects) ApplyOne(rooms, room, o);
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

        private static void ApplyOne(List<BuildingLayout.RoomSpec> rooms, BuildingLayout.RoomSpec room, PlacedObject o)
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
                    p.ObjScale = o.Scale1;
                    p.SortNudge = o.sortNudge;
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
                    i.ObjScale = o.Scale1;
                    i.SortNudge = o.sortNudge;
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
                    c.ObjScale = o.Scale1;
                    c.SortNudge = o.sortNudge;
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
                case LayoutKind.Door:
                {
                    // Doors are never removed or added here: only their wall edge, flip and art change.
                    if (o.removed || string.IsNullOrEmpty(o.doorEdge) || !BuildingLayout.WallEdges(room).Contains(o.doorEdge)) return;
                    BuildingLayout.DoorSpec d = room.Doors.Find(x => x.Id == o.id);
                    if (d != null)
                    {
                        BuildingLayout.MoveDoor(rooms, room, d, o.doorEdge);
                        d.FlipX = o.flipX;
                        if (System.Enum.TryParse(o.doorArt, out BuildingLayout.DoorArtKind art)) d.Art = art;
                        return;
                    }

                    BuildingLayout.HotspotSpec h = room.Hotspots.Find(x => !string.IsNullOrEmpty(x.Edge) && BuildingLayout.HotspotLayoutId(x) == o.id);
                    if (h == null) return;
                    BuildingLayout.MoveHotspot(room, h, o.doorEdge);
                    h.FlipX = o.flipX;
                    if (!string.IsNullOrEmpty(o.doorArt)) h.Art = o.doorArt;
                    return;
                }
            }
        }

        /// <summary>
        /// Cells blocked by solid furniture (PropFootprint: sprite-base footprint × saved scale, required cells
        /// carved out, shrunk where it would cut a path), with the blocking object's id.
        /// </summary>
        public static Dictionary<Vector2Int, string> BlockerCells(BuildingLayout.RoomSpec room) => PropFootprint.Resolve(room);

        /// <summary>Every prop the resolver had to shrink so a required path stays open (for the handoff / console).</summary>
        public static List<string> ShrinkNotes(List<BuildingLayout.RoomSpec> rooms)
        {
            var notes = new List<string>();
            foreach (BuildingLayout.RoomSpec room in rooms) PropFootprint.Resolve(room, notes);
            return notes;
        }

        /// <summary>Door problems: an edge outside the room's walls, or two doors in one wall gap.</summary>
        public static List<string> CheckDoors(BuildingLayout.RoomSpec room)
        {
            var problems = new List<string>();
            List<string> walls = BuildingLayout.WallEdges(room);
            var used = new Dictionary<string, string>();
            var doors = new List<KeyValuePair<string, string>>();
            foreach (BuildingLayout.DoorSpec d in room.Doors) doors.Add(new KeyValuePair<string, string>(d.Edge, d.Id));
            foreach (BuildingLayout.HotspotSpec h in room.Hotspots)
            {
                if (!string.IsNullOrEmpty(h.Edge)) doors.Add(new KeyValuePair<string, string>(h.Edge, BuildingLayout.HotspotLayoutId(h)));
            }

            foreach (KeyValuePair<string, string> d in doors)
            {
                if (BuildingLayout.IsWallEdge(d.Key) && !walls.Contains(d.Key)) problems.Add($"room '{room.Id}': '{d.Value}' sits on edge {d.Key}, which is not one of its walls.");
                if (used.TryGetValue(d.Key, out string other)) problems.Add($"room '{room.Id}': '{d.Value}' and '{other}' share the wall gap {d.Key}.");
                else used[d.Key] = d.Value;
            }

            return problems;
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
        /// Problems in one room after the overrides: doors, solid furniture on a required cell, or cutting a required
        /// cell / container / pickup off from the player spawn. Messages name the room and the object id.
        /// </summary>
        public static List<string> Check(BuildingLayout.RoomSpec room)
        {
            List<string> problems = CheckDoors(room);
            problems.AddRange(PropFootprint.Problems(room, BlockerCells(room), PropFootprint.HardCells(room)));
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
            ApplyTransform(tag, p.ObjScale, p.SortNudge);
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
            ApplyTransform(tag, i.ObjScale, i.SortNudge);
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
            ApplyTransform(tag, c.ObjScale, c.SortNudge);
        }

        public static void Tag(GameObject go, BuildingLayout.LightSpec l)
        {
            LayoutObject tag = Ensure(go, l.Name, LayoutKind.Light);
            tag.Lift = l.Lift;
            tag.Radius = l.Radius;
            tag.LightColor = l.Color;
        }

        public static void Tag(GameObject go, BuildingLayout.DoorSpec d)
        {
            LayoutObject tag = Ensure(go, d.Id, LayoutKind.Door);
            tag.DoorEdge = d.Edge;
            tag.DoorArt = d.Art.ToString();
            tag.FlipX = d.FlipX;
            tag.DoorTarget = d.TargetRoom;
            tag.DoorTwoWay = d.TwoWay;
        }

        /// <summary>A wall hotspot (the F2 stairwell door) is laid out like a door; it plays its choice instead.</summary>
        public static void Tag(GameObject go, BuildingLayout.HotspotSpec h)
        {
            LayoutObject tag = Ensure(go, BuildingLayout.HotspotLayoutId(h), LayoutKind.Door);
            tag.DoorEdge = h.Edge;
            tag.DoorArt = h.Art;
            tag.FlipX = h.FlipX;
            tag.DoorTarget = h.ChoiceKey;
            tag.DoorTwoWay = false;
        }

        /// <summary>
        /// Applies the uniform layout scale (children named "Approach" keep their world position, so the player
        /// still stands in front) and the sorting nudge (iso sorter offset, or the renderers' order without one).
        /// </summary>
        private static void ApplyTransform(LayoutObject tag, float scale, int nudge)
        {
            Transform t = tag.transform;
            tag.BaseScale = t.localScale;
            tag.ObjectScale = scale > 0.01f ? scale : 1f;
            tag.SortNudge = nudge;
            if (!Mathf.Approximately(tag.ObjectScale, 1f))
            {
                Transform approach = t.Find("Approach");
                Vector3 stand = approach != null ? approach.position : Vector3.zero;
                t.localScale = new Vector3(tag.BaseScale.x * tag.ObjectScale, tag.BaseScale.y * tag.ObjectScale, 1f);
                if (approach != null) approach.position = stand;
            }

            if (nudge == 0) return;
            SixtyOne.Iso.IsoSorter sorter = t.GetComponent<SixtyOne.Iso.IsoSorter>();
            if (sorter != null)
            {
                var so = new SerializedObject(sorter);
                so.FindProperty("offset").intValue += nudge;
                so.ApplyModifiedPropertiesWithoutUndo();
                sorter.Apply();
            }
            else
            {
                foreach (SpriteRenderer r in t.GetComponents<SpriteRenderer>()) r.sortingOrder += nudge;
            }
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
                tint = tag.Tint,
                objectScale = ScaleOf(tag),
                sortNudge = tag.SortNudge,
                doorEdge = tag.DoorEdge,
                doorArt = tag.DoorArt
            };
        }

        /// <summary>The uniform scale of a tagged object now (Scene scale tool or slider): localScale / the built scale.</summary>
        public static float ScaleOf(LayoutObject tag)
        {
            if (tag.Kind == LayoutKind.Door || tag.Kind == LayoutKind.Light || Mathf.Abs(tag.BaseScale.x) < 1e-4f) return 1f;
            return Round(tag.transform.localScale.x / tag.BaseScale.x);
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
            foreach (BuildingLayout.DoorSpec d in room.Doors)
            {
                Vector2 c = BuildingLayout.EdgeCenter(room, d.Edge);
                yield return Placed(c.x, c.y, go => Tag(go, d));
            }

            foreach (BuildingLayout.HotspotSpec h in room.Hotspots)
            {
                if (!string.IsNullOrEmpty(h.Edge)) yield return Placed(h.Gx, h.Gy, go => Tag(go, h));
            }
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
            // A door is its wall edge (the dragged position only picks the edge), flip and art.
            if (a.kind == LayoutKind.Door) return a.doorEdge == b.doorEdge && a.flipX == b.flipX && (a.doorArt ?? "") == (b.doorArt ?? "");
            if (!Near(a.Scale1, b.Scale1) || a.sortNudge != b.sortNudge) return false;
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
