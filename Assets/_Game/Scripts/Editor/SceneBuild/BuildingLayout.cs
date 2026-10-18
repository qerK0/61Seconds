using System.Collections.Generic;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Data tables for every room of the test building (positions in iso grid cells). Flats are several rooms
    /// (entrance hall + living room + kitchen/bedroom) that share one zone = one node; corridors are single rooms
    /// with one door per flat plus the stairs. Doors sit on wall gaps (edge codes, see <see cref="EdgeCenter"/>).
    /// </summary>
    public static class BuildingLayout
    {
        public sealed class AnchorSpec
        {
            public string Id;
            public float Gx;
            public float Gy;
            public AnchorTag Tag;
            public int Capacity = 1;
            public string HazardId;
            public string LabelKey;
            public string[] Adjacent = new string[0];
        }

        public sealed class PropSpec
        {
            /// <summary>Stable id inside the room (Name, or Name#2, #3 for repeats); set by Rooms(). Layout overrides key on it.</summary>
            public string Id;
            public string PropId;
            public string Name;
            public bool FlipX;
            /// <summary>Blocks its nav cell (hand-placed furniture; code props never did).</summary>
            public bool BlocksNav;
            public float Gx;
            public float Gy;
            public float Lift;
            public Vector2 Scale;
            public Color Color;
            public float Topple = -80f;
            /// <summary>Uniform scale on top of the built size (Layout Editor; 1 = as built).</summary>
            public float ObjScale = 1f;
            /// <summary>Sorting nudge added to the iso order (+ = in front).</summary>
            public int SortNudge;
            public float Drop = 0.8f;
        }

        public sealed class ItemSpec
        {
            public string Id;
            public string ItemId;
            public float Gx;
            public float Gy;
            public float Cost = 1f;
            public bool Surface = true;
            public bool Borrowed;
            public bool Hidden;
            /// <summary>Our own flat: only usable during the pre-quake prep.</summary>
            public bool PrepOnly;
            public bool FlipX;
            public Color Color = Color.white;
            /// <summary>Uniform scale on top of the built size (Layout Editor; 1 = as built).</summary>
            public float ObjScale = 1f;
            /// <summary>Sorting nudge added to the iso order (+ = in front).</summary>
            public int SortNudge;
        }

        /// <summary>Furniture you search: a LootContainer with its contents (item ids).</summary>
        public sealed class ContainerSpec
        {
            public string Id;
            /// <summary>container.wardrobe / cabinet / kitchen_cabinet / bookshelf / drawer / toolbox / nightstand.</summary>
            public string NameKey;
            /// <summary>Prop art name (PropArtSet): Wardrobe, NeighbourWardrobe, Bookshelf, Cabinet; others are tinted blocks.</summary>
            public string Art;
            public float Gx;
            public float Gy;
            public string[] Items = new string[0];
            public string[] Hidden = new string[0];
            public float Cost = 3f;
            public float Seconds = 1.5f;
            public bool Borrowed;
            public bool PrepOnly;
            public bool FlipX;
            public bool BlocksNav;
            public Vector2 Scale = new Vector2(0.7f, 0.55f);
            public Color Color = Color.white;
            /// <summary>Uniform scale on top of the built size (Layout Editor; 1 = as built).</summary>
            public float ObjScale = 1f;
            /// <summary>Sorting nudge added to the iso order (+ = in front).</summary>
            public int SortNudge;
        }

        public sealed class LightSpec
        {
            public string Name;
            public float Gx;
            public float Gy;
            public float Lift;
            public Color Color;
            public float Radius;
        }

        /// <summary>Which RoomTileSet a room uses.</summary>
        public enum RoomArt { Home, Corridor, DamagedCorridor, Street }

        /// <summary>A window/balcony where the bedsheet rope lowers the family to another node.</summary>
        public sealed class RopeSpec
        {
            public string Id;
            public float Gx;
            public float Gy;
            public string TargetNodeId;
        }

        /// <summary>Art in a door's wall gap: a door, an elevator, a stairwell door (door + sign) or stairs (steps down + sign).</summary>
        public enum DoorArtKind { Door, Elevator, Stairwell, Stairs }

        /// <summary>Decorative stairs in a back-wall gap (not clickable, no sign); broken = dark, with debris on top.</summary>
        public sealed class DecorStairsSpec
        {
            public string Edge;
            public bool Broken;
        }

        /// <summary>A door on an edge of this room leading to another room (arrival = just inside it there).</summary>
        public sealed class DoorSpec
        {
            /// <summary>Edge code: L/R = back-left/back-right wall gap, F/G = front-right/front-left floor exit.</summary>
            public string Edge;
            public string TargetRoom;
            /// <summary>Grid point in the target room where the player appears.</summary>
            public Vector2 Arrival;
            /// <summary>Two-way door: the target room has a door back, and the arrival follows that door when it moves.</summary>
            public bool TwoWay;
            /// <summary>Door, elevator, stairwell door or stairs (stairwell and stairs carry a "MERDİVEN ↓" sign).</summary>
            public DoorArtKind Art;
            /// <summary>Mirrors the art (Layout Editor).</summary>
            public bool FlipX;
            /// <summary>Stable id inside the room (layout overrides key on it): "door_" + target room.</summary>
            public string Id => "door_" + TargetRoom;
        }

        /// <summary>A person or object that plays a node choice (by label key).</summary>
        public sealed class HotspotSpec
        {
            public string Name;
            public string ChoiceKey;
            public float Gx;
            public float Gy;
            public bool Person;
            public Vector2 Scale = Vector2.one;
            public Color Color = Color.white;
            /// <summary>Disappears once its choice is gone (the neighbour left).</summary>
            public bool HideWhenGone;
            /// <summary>NPC idle id (CharacterArt.NpcIdles: sevim, cemil, robber, dog): loops its frames when delivered, else a dark silhouette at its height.</summary>
            public string Idle;
            /// <summary>Grid point the NPC looks toward (flips its idle art); null = as drawn.</summary>
            public Vector2? Face;
            /// <summary>Prop art name (PropArtSet) for objects, e.g. "Valve"; null = tinted block.</summary>
            public string Art;
            /// <summary>More choice keys the same object plays in other nodes (prep / post-quake valve).</summary>
            public string[] AlsoKeys = new string[0];
            /// <summary>Wall-gap edge code for a stairwell-door hotspot (Art = "Stairwell"): sits in the gap, approached from inside.</summary>
            public string Edge;
            /// <summary>Mirrors the art (Layout Editor).</summary>
            public bool FlipX;
        }

        public sealed class RoomSpec
        {
            public string Id;
            /// <summary>Entry room id of the flat this room belongs to (null = own zone).</summary>
            public string Zone;
            public int Width = 4;
            public int Height = 4;
            public Color Floor;
            public RoomArt Art = RoomArt.Home;
            public bool Walls = true;
            /// <summary>Back-wall segments left open (exits): "L3" = back-left edge, segment x=3; "R1" = back-right edge, segment y=1.</summary>
            public List<string> Gaps = new List<string>();
            /// <summary>Back-wall segments using the window variant (same codes as Gaps).</summary>
            public List<string> Windows = new List<string>();
            public Vector2 PlayerSpawn;
            public Vector2 WifeSpawn;
            public Vector2 ChildSpawn;
            public List<AnchorSpec> Anchors = new List<AnchorSpec>();
            public List<PropSpec> Props = new List<PropSpec>();
            public List<ItemSpec> Items = new List<ItemSpec>();
            public List<ContainerSpec> Containers = new List<ContainerSpec>();
            public List<LightSpec> Lights = new List<LightSpec>();
            public List<Vector2> ExtraTiles = new List<Vector2>();
            public List<RopeSpec> Ropes = new List<RopeSpec>();
            public List<DoorSpec> Doors = new List<DoorSpec>();
            public List<HotspotSpec> Hotspots = new List<HotspotSpec>();
            /// <summary>Decorative stairs in back-wall gaps (the F3 arrival, the F3 broken stairwell).</summary>
            public List<DecorStairsSpec> DecorStairs = new List<DecorStairsSpec>();
            /// <summary>Floors below the top floor (0 = floor 4 ... 3 = street): more debris and cracks.</summary>
            public int Depth;
            /// <summary>Cells that are never walkable (the street's power line).</summary>
            public List<Vector2Int> Blocked = new List<Vector2Int>();
            /// <summary>Long room: the camera follows the player (the street).</summary>
            public bool FollowCamera;
        }

        /// <summary>How far inside a door the player stands / arrives (grid cells).</summary>
        public const float DoorInset = 0.55f;

        private static readonly Color Warm = new Color(1f, 0.78f, 0.5f, 1f);
        private static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f, 1f);
        private static readonly Color Metal = new Color(0.5f, 0.5f, 0.55f, 1f);

        // ---------- edge geometry ----------

        /// <summary>True for back-wall door codes (L/R); F/G are floor exits on the open front edges.</summary>
        public static bool IsWallEdge(string edge) => edge[0] == 'L' || edge[0] == 'R';

        /// <summary>Grid point at the middle of an edge segment.</summary>
        public static Vector2 EdgeCenter(RoomSpec room, string edge)
        {
            int i = int.Parse(edge.Substring(1));
            switch (edge[0])
            {
                case 'L': return new Vector2(i + 0.5f, room.Height);
                case 'R': return new Vector2(room.Width, i + 0.5f);
                case 'F': return new Vector2(i + 0.5f, 0f);
                default: return new Vector2(0f, i + 0.5f);
            }
        }

        /// <summary>Grid point just inside the edge segment (where you stand to use the door / arrive through it).</summary>
        public static Vector2 EdgeInside(RoomSpec room, string edge)
        {
            Vector2 c = EdgeCenter(room, edge);
            switch (edge[0])
            {
                case 'L': return c - new Vector2(0f, DoorInset);
                case 'R': return c - new Vector2(DoorInset, 0f);
                case 'F': return c + new Vector2(0f, DoorInset);
                default: return c + new Vector2(DoorInset, 0f);
            }
        }

        // ---------- builders ----------

        private static RoomSpec Room(string id, int w, int h, Color floor, string zone = null)
        {
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            return new RoomSpec
            {
                Id = id, Zone = zone, Width = w, Height = h, Floor = floor,
                PlayerSpawn = new Vector2(cx - 0.6f, cy - 0.6f), WifeSpawn = new Vector2(cx, cy - 0.9f), ChildSpawn = new Vector2(cx - 0.9f, cy)
            };
        }

        private static RoomSpec Art(RoomSpec room, RoomArt art, string gaps = "", string windows = "", bool walls = true)
        {
            room.Art = art;
            room.Walls = walls;
            room.Gaps.AddRange(gaps.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
            room.Windows.AddRange(windows.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
            return room;
        }

        /// <summary>Two-way door: a's edge ⇄ b's edge (wall gaps opened on both sides).</summary>
        private static void Link(RoomSpec a, string edgeA, RoomSpec b, string edgeB)
        {
            OneWay(a, edgeA, b, EdgeInside(b, edgeB)).TwoWay = true;
            OneWay(b, edgeB, a, EdgeInside(a, edgeA)).TwoWay = true;
        }

        /// <summary>One-way exit: no door back.</summary>
        private static DoorSpec OneWay(RoomSpec from, string edge, RoomSpec to, Vector2 arrival)
        {
            var door = new DoorSpec { Edge = edge, TargetRoom = to.Id, Arrival = arrival };
            from.Doors.Add(door);
            if (IsWallEdge(edge) && !from.Gaps.Contains(edge)) from.Gaps.Add(edge);
            return door;
        }

        /// <summary>
        /// The way down: stairs (Art/items/merdiven.png in the gap + "MERDİVEN ↓" sign) on a back-wall gap; clicking it
        /// plays the node's stairs move choice like any door into another zone.
        /// </summary>
        private static void StairwellDoor(RoomSpec from, string edge, RoomSpec to, Vector2 arrival)
        {
            OneWay(from, edge, to, arrival).Art = DoorArtKind.Stairs;
        }

        /// <summary>Decorative stairs in a back-wall gap (adds the gap).</summary>
        private static void DecorStairs(RoomSpec room, string edge, bool broken)
        {
            if (!room.Gaps.Contains(edge)) room.Gaps.Add(edge);
            room.DecorStairs.Add(new DecorStairsSpec { Edge = edge, Broken = broken });
        }

        /// <summary>
        /// Moves a door to another edge of its room (Layout Editor): the wall gap follows it and, for a two-way door,
        /// the arrival of the door back (in the target room) moves with it.
        /// </summary>
        public static void MoveDoor(List<RoomSpec> rooms, RoomSpec room, DoorSpec door, string edge)
        {
            if (door.Edge == edge) return;
            string old = door.Edge;
            door.Edge = edge;
            if (IsWallEdge(old) && !room.Doors.Exists(d => d.Edge == old) && !room.Hotspots.Exists(h => h.Edge == old)) room.Gaps.Remove(old);
            if (IsWallEdge(edge) && !room.Gaps.Contains(edge)) room.Gaps.Add(edge);
            if (!door.TwoWay) return;
            RoomSpec target = rooms.Find(r => r.Id == door.TargetRoom);
            DoorSpec back = target?.Doors.Find(d => d.TargetRoom == room.Id);
            if (back != null) back.Arrival = EdgeInside(room, edge);
        }

        /// <summary>Moves a stairwell-door hotspot to another wall edge (the gap follows it).</summary>
        public static void MoveHotspot(RoomSpec room, HotspotSpec h, string edge)
        {
            if (string.IsNullOrEmpty(h.Edge) || h.Edge == edge) return;
            string old = h.Edge;
            h.Edge = edge;
            Vector2 c = EdgeCenter(room, edge);
            h.Gx = c.x;
            h.Gy = c.y;
            if (!room.Doors.Exists(d => d.Edge == old) && !room.Hotspots.Exists(x => x.Edge == old)) room.Gaps.Remove(old);
            if (!room.Gaps.Contains(edge)) room.Gaps.Add(edge);
        }

        /// <summary>Layout id of a wall hotspot (the F2 stairwell door): "hotspot_" + its name.</summary>
        public static string HotspotLayoutId(HotspotSpec h) => "hotspot_" + h.Name;

        /// <summary>The wall-gap edge whose centre is nearest to a grid point (where a dragged door snaps to).</summary>
        public static string NearestWallEdge(RoomSpec room, Vector2 grid)
        {
            string best = null;
            float bestDistance = float.MaxValue;
            foreach (string edge in WallEdges(room))
            {
                float d = (EdgeCenter(room, edge) - grid).sqrMagnitude;
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = edge;
            }

            return best;
        }

        /// <summary>
        /// Nav cells an object of a uniform scale covers: its own cell at scale 1 (as before), growing by half a
        /// cell per side for every extra unit of scale (scale 2 = 2 x 2 cells).
        /// </summary>
        public static IEnumerable<Vector2Int> Footprint(float gx, float gy, float scale)
        {
            float extra = Mathf.Max(0f, scale - 1f) * 0.5f;
            int x0 = Mathf.FloorToInt(gx - extra), x1 = Mathf.FloorToInt(gx + extra);
            int y0 = Mathf.FloorToInt(gy - extra), y1 = Mathf.FloorToInt(gy + extra);
            for (int x = x0; x <= x1; x++)
            {
                for (int y = y0; y <= y1; y++) yield return new Vector2Int(x, y);
            }
        }

        /// <summary>Wall-gap edge codes of a room (L0..L(w-1), R0..R(h-1)).</summary>
        public static List<string> WallEdges(RoomSpec room)
        {
            var edges = new List<string>();
            for (int x = 0; x < room.Width; x++) edges.Add("L" + x);
            for (int y = 0; y < room.Height; y++) edges.Add("R" + y);
            return edges;
        }

        private static HotspotSpec Person(string name, string choiceKey, float gx, float gy, Color color, bool hideWhenGone = true, string idle = null)
        {
            return new HotspotSpec { Name = name, ChoiceKey = choiceKey, Gx = gx, Gy = gy, Person = true, Color = color, HideWhenGone = hideWhenGone, Idle = idle };
        }

        private static HotspotSpec Thing(string name, string choiceKey, float gx, float gy, float sx, float sy, Color color)
        {
            return new HotspotSpec { Name = name, ChoiceKey = choiceKey, Gx = gx, Gy = gy, Scale = new Vector2(sx, sy), Color = color };
        }

        /// <summary>A stairwell door in a back-wall gap that plays a choice (the F2 stairs down to the street).</summary>
        private static HotspotSpec StairwellHotspot(RoomSpec room, string edge, string choiceKey)
        {
            Vector2 c = EdgeCenter(room, edge);
            if (!room.Gaps.Contains(edge)) room.Gaps.Add(edge);
            return new HotspotSpec { Name = "Stairwell", ChoiceKey = choiceKey, Gx = c.x, Gy = c.y, Art = StairsArt, Edge = edge };
        }

        /// <summary>Hotspot art name of a stairwell door (door art + sign).</summary>
        public const string StairwellArt = "Stairwell";

        /// <summary>Hotspot art name of stairs (steps down + sign).</summary>
        public const string StairsArt = "Stairs";

        /// <summary>Where the player stands to use a hotspot: inside its wall gap, else in front of it (camera side).</summary>
        public static Vector2 HotspotApproach(RoomSpec room, HotspotSpec h)
        {
            return !string.IsNullOrEmpty(h.Edge) ? EdgeInside(room, h.Edge) : new Vector2(h.Gx - 0.5f, h.Gy - 0.5f);
        }

        private static AnchorSpec A(string id, float gx, float gy, AnchorTag tag, int cap, string hazard, string label, params string[] adjacent)
        {
            return new AnchorSpec { Id = id, Gx = gx, Gy = gy, Tag = tag, Capacity = cap, HazardId = hazard, LabelKey = label, Adjacent = adjacent };
        }

        private static PropSpec P(string propId, string name, float gx, float gy, float lift, float sx, float sy, Color color, float topple = -80f, float drop = 0.8f)
        {
            return new PropSpec { PropId = propId, Name = name, Gx = gx, Gy = gy, Lift = lift, Scale = new Vector2(sx, sy), Color = color, Topple = topple, Drop = drop };
        }

        /// <summary>Marks an item of our own flat: only usable during the pre-quake prep.</summary>
        private static ItemSpec Own(ItemSpec spec)
        {
            spec.PrepOnly = true;
            return spec;
        }

        private static ItemSpec I(string itemId, float gx, float gy, Color color, float cost = 1f, bool surface = true, bool borrowed = false, bool hidden = false, string id = null)
        {
            return new ItemSpec { Id = id ?? itemId, ItemId = itemId, Gx = gx, Gy = gy, Color = color, Cost = cost, Surface = surface, Borrowed = borrowed, Hidden = hidden };
        }

        /// <summary>A searchable piece of furniture (first search 3 s of building time + 1.5 s real time).</summary>
        private static ContainerSpec Box(string id, string kind, float gx, float gy, Color color, params string[] items)
        {
            ContainerSpec spec = ContainerPreset(id, kind);
            spec.Gx = gx;
            spec.Gy = gy;
            spec.Items = items;
            spec.Color = color;
            return spec;
        }

        /// <summary>Container kinds the Layout Editor palette offers (container.&lt;kind&gt; name keys).</summary>
        public static readonly string[] ContainerKinds = { "wardrobe", "wardrobe_v2", "cabinet", "kitchen_cabinet", "bookshelf", "drawer", "toolbox", "nightstand" };

        /// <summary>A container of a kind with its art name and block size (position and contents left empty).</summary>
        public static ContainerSpec ContainerPreset(string id, string kind)
        {
            var spec = new ContainerSpec { Id = id, NameKey = "container." + kind, Color = Wood };
            switch (kind)
            {
                case "wardrobe": spec.Art = "Wardrobe"; spec.Scale = new Vector2(1.0f, 1.7f); break;
                // The newer wardrobe art: still a "Gardırop" (container.wardrobe).
                case "wardrobe_v2": spec.NameKey = "container.wardrobe"; spec.Art = "WardrobeV2"; spec.Scale = new Vector2(1.0f, 1.7f); break;
                case "bookshelf": spec.Art = "Bookshelf"; spec.Scale = new Vector2(1.0f, 1.7f); break;
                case "cabinet": spec.Art = "Cabinet"; spec.Scale = new Vector2(0.9f, 1.2f); break;
                case "kitchen_cabinet": spec.Art = "KitchenCabinet"; spec.Scale = new Vector2(0.9f, 0.9f); break;
                case "toolbox": spec.Art = "Toolbox"; spec.Scale = new Vector2(0.55f, 0.35f); break;
                case "drawer": spec.Scale = new Vector2(0.8f, 0.5f); break;
                case "nightstand": spec.Art = "Nightstand"; spec.Scale = new Vector2(0.55f, 0.55f); break;
                default: spec.Scale = new Vector2(0.55f, 0.35f); break;
            }

            return spec;
        }

        /// <summary>Our own flat: only searchable during the pre-quake prep.</summary>
        private static ContainerSpec Own(ContainerSpec spec)
        {
            spec.PrepOnly = true;
            return spec;
        }

        /// <summary>A neighbour's furniture: everything taken is borrowed ("ödünç al"), neighbour-style art.</summary>
        private static ContainerSpec Lent(ContainerSpec spec)
        {
            spec.Borrowed = true;
            if (spec.Art == "Wardrobe") spec.Art = "NeighbourWardrobe";
            return spec;
        }

        /// <summary>
        /// The visible gas valve next to a stove: clicking it plays the node's valve choice (ChoiceHotspot), so the
        /// panel no longer shows a valve button.
        /// </summary>
        private static HotspotSpec Valve(float gx, float gy, string choiceKey, params string[] alsoKeys)
        {
            return new HotspotSpec { Name = "Valve", ChoiceKey = choiceKey, Gx = gx, Gy = gy, Scale = new Vector2(0.3f, 0.3f), Color = new Color(0.85f, 0.75f, 0.2f, 1f), Art = "Valve", AlsoKeys = alsoKeys };
        }

        /// <summary>A wall lamp on the back-left wall at segment x (a Light2D practical sits on it).</summary>
        private static void WallLamp(RoomSpec room, float x, string lightName)
        {
            room.Props.Add(P(null, "WallLamp", x, room.Height, 0.72f, 0.25f, 0.25f, new Color(1f, 0.9f, 0.6f, 1f)));
            room.Lights.Add(L(lightName, x, room.Height - 0.2f, 0.8f, 2.4f));
        }

        private static LightSpec L(string name, float gx, float gy, float lift, float radius, Color? color = null)
        {
            return new LightSpec { Name = name, Gx = gx, Gy = gy, Lift = lift, Radius = radius, Color = color ?? Warm };
        }

        /// <summary>All rooms, top floor first.</summary>
        public static List<RoomSpec> Rooms()
        {
            var floor2 = new List<RoomSpec>(Floor2());
            var floor3 = new List<RoomSpec>(Floor3(floor2));
            var floor4 = new List<RoomSpec>(Floor4(floor3));
            foreach (RoomSpec r in floor3) r.Depth = 1;
            foreach (RoomSpec r in floor2) r.Depth = r.Depth > 0 ? r.Depth : 2;
            var rooms = new List<RoomSpec>();
            rooms.AddRange(floor4);
            rooms.AddRange(floor3);
            rooms.AddRange(floor2);
            foreach (RoomSpec room in rooms) AssignPropIds(room);
            return rooms;
        }

        /// <summary>Gives every prop a stable id: its name, then Name#2, Name#3 ... for repeats in the same room.</summary>
        private static void AssignPropIds(RoomSpec room)
        {
            var counts = new Dictionary<string, int>();
            foreach (PropSpec p in room.Props)
            {
                if (!string.IsNullOrEmpty(p.Id)) continue;
                counts.TryGetValue(p.Name, out int n);
                counts[p.Name] = ++n;
                p.Id = n == 1 ? p.Name : p.Name + "#" + n;
            }
        }

        /// <summary>Where the player stands to search a container: in front of it (camera side), on the floor.</summary>
        public static Vector2 ContainerApproach(RoomSpec room, ContainerSpec c)
        {
            return new Vector2(Mathf.Clamp(c.Gx - 0.45f, 0.3f, room.Width - 0.3f), Mathf.Clamp(c.Gy - 0.45f, 0.3f, room.Height - 0.3f));
        }

        /// <summary>
        /// The street finale (StreetLayout): 20 x 6 cells, Rocky floor, no walls; our building's door at the right end,
        /// the "Acil Toplanma Alanı" sign far left, a kerb and façade silhouettes along the back edge, parked cars,
        /// the downed power line (built by the scene builder) and its fallen pole. The camera follows the player.
        /// </summary>
        private static RoomSpec Street()
        {
            RoomSpec street = Art(Room(BuildingLevelsGenerator.Street, StreetLayout.Width, StreetLayout.Height, new Color(0.4f, 0.42f, 0.45f, 1f)), RoomArt.Street, walls: false);
            street.Depth = 3;
            street.FollowCamera = true;
            Vector2Int door = StreetLayout.DoorCell;
            street.PlayerSpawn = new Vector2(door.x + 0.5f, door.y + 0.5f);
            street.WifeSpawn = new Vector2(door.x + 0.3f, door.y + 1.3f);
            street.ChildSpawn = new Vector2(door.x + 0.3f, door.y - 0.4f);
            street.Blocked.AddRange(StreetLayout.StaticBlocked());

            Color facade = new Color(0.16f, 0.17f, 0.21f, 1f);
            for (int x = 0; x < StreetLayout.Width; x += 2)
            {
                street.Props.Add(P(null, "Kerb", x + 1f, StreetLayout.Height + 0.05f, 0f, 1.1f, 0.1f, new Color(0.55f, 0.55f, 0.57f, 1f)));
            }

            for (int x = 0; x < StreetLayout.Width; x += 3)
            {
                float shade = 0.85f + 0.15f * ((x * 7) % 3);
                street.Props.Add(P(null, "Facade", x + 1.5f, StreetLayout.Height + 0.9f, 0f, 1.6f, 2.6f + 0.4f * ((x / 3) % 3), facade * shade));
            }

            // Our building at the right end, its door where the family comes out.
            street.Props.Add(P(null, "OurBuilding", StreetLayout.Width + 0.9f, door.y + 0.5f, 0f, 2.2f, 3.4f, new Color(0.24f, 0.22f, 0.24f, 1f)));
            street.Props.Add(P(null, "BuildingDoor", StreetLayout.Width + 0.05f, door.y + 0.5f, 0f, 0.5f, 0.9f, new Color(0.3f, 0.2f, 0.12f, 1f)));

            Color[] cars = { new Color(0.55f, 0.12f, 0.1f, 1f), new Color(0.2f, 0.3f, 0.5f, 1f), new Color(0.75f, 0.75f, 0.72f, 1f) };
            for (int i = 0; i + 1 < StreetLayout.CarCells.Length; i += 2)
            {
                Vector2Int a = StreetLayout.CarCells[i];
                Vector2Int b = StreetLayout.CarCells[i + 1];
                street.Props.Add(P(null, "Car", (a.x + b.x) * 0.5f + 0.5f, (a.y + b.y) * 0.5f + 0.5f, 0f, 1.5f, 0.6f, cars[(i / 2) % cars.Length]));
            }

            // The power pole lies across the back of the street where the line snapped.
            street.Props.Add(P(null, "FallenPole", StreetLayout.LineX + 0.5f, StreetLayout.Height - 0.3f, 0f, 0.25f, 1.4f, new Color(0.35f, 0.28f, 0.2f, 1f)));

            Vector2Int sign = StreetLayout.SignCell;
            street.Hotspots.Add(Thing("Sign", BuildingLevelsGenerator.F2Street + ".c_sign", sign.x + 1.0f, sign.y + 1.0f, 0.5f, 1.4f, new Color(0.15f, 0.55f, 0.3f, 1f)));
            street.Lights.Add(L("StreetLampA", 5.0f, StreetLayout.Height - 0.5f, 2.0f, 4.5f, new Color(0.8f, 0.85f, 1f, 1f)));
            street.Lights.Add(L("StreetLampB", 15.0f, StreetLayout.Height - 0.5f, 2.0f, 4.5f, new Color(0.8f, 0.85f, 1f, 1f)));
            return street;
        }

        // ---------- navigation (mirrors Room.RequiredCells / Room.BaseNav at runtime) ----------

        /// <summary>The room's walkable grid without debris.</summary>
        public static GridNav NavFor(RoomSpec room)
        {
            var extra = new List<Vector2Int>();
            foreach (Vector2 e in room.ExtraTiles) extra.Add(new Vector2Int(Mathf.FloorToInt(e.x), Mathf.FloorToInt(e.y)));
            var nav = new GridNav(room.Width, room.Height, extra);
            nav.SetBlocked(room.Blocked);
            return nav;
        }

        /// <summary>
        /// Cells debris must never cover or cut off: spawns, anchors, door approaches (and arrivals), hotspot
        /// approaches, pickups / containers and rope points.
        /// </summary>
        public static List<Vector2Int> RequiredCells(RoomSpec room)
        {
            var points = new List<Vector2> { room.PlayerSpawn, room.WifeSpawn, room.ChildSpawn };
            foreach (AnchorSpec a in room.Anchors) points.Add(new Vector2(a.Gx, a.Gy));
            foreach (DoorSpec d in room.Doors) points.Add(EdgeInside(room, d.Edge));
            foreach (HotspotSpec h in room.Hotspots) if (!string.IsNullOrEmpty(h.ChoiceKey)) points.Add(HotspotApproach(room, h));
            foreach (ItemSpec i in room.Items) points.Add(new Vector2(i.Gx, i.Gy));
            foreach (RopeSpec r in room.Ropes) points.Add(new Vector2(r.Gx, r.Gy));
            foreach (ContainerSpec c in room.Containers) points.Add(ContainerApproach(room, c));
            var cells = new List<Vector2Int>();
            foreach (Vector2 p in points) cells.Add(GridNav.CellOf(p));
            return cells;
        }

        private static RoomSpec Find(List<RoomSpec> rooms, string id) => rooms.Find(r => r.Id == id);

        private static IEnumerable<RoomSpec> Floor4(List<RoomSpec> floor3)
        {
            // Flat 11 salon: the opening-quake placement room. Doorway (L3) to the hall is also a placement trap.
            RoomSpec salon = Art(Room(BuildingLevelsGenerator.Flat11Salon, 6, 6, new Color(0.78f, 0.74f, 0.66f, 1f)), RoomArt.Home, gaps: "R0", windows: "R1");
            salon.ExtraTiles.Add(new Vector2(6.5f, 0.5f));
            salon.ExtraTiles.Add(new Vector2(6.5f, 1.5f));
            salon.PlayerSpawn = new Vector2(0.9f, 0.9f);
            salon.WifeSpawn = new Vector2(1.5f, 0.5f);
            salon.ChildSpawn = new Vector2(0.5f, 1.5f);
            salon.Anchors.Add(A("lr_table", 2.5f, 2.5f, AnchorTag.Safe, 2, null, "anchor.table", "lr_inner_wall", "lr_doorway", "lr_wardrobe", "lr_window"));
            salon.Anchors.Add(A("lr_inner_wall", 1.0f, 5.3f, AnchorTag.Safe, 1, null, "anchor.inner_wall", "lr_table", "lr_doorway"));
            salon.Anchors.Add(A("lr_doorway", 3.5f, 5.4f, AnchorTag.Trap, 1, "hz.doorway", "anchor.doorway", "lr_inner_wall", "lr_table"));
            salon.Anchors.Add(A("lr_wardrobe", 5.3f, 4.0f, AnchorTag.Trap, 1, "hz.wardrobe", "anchor.wardrobe", "lr_table", "lr_window"));
            salon.Anchors.Add(A("lr_window", 5.3f, 1.5f, AnchorTag.Trap, 1, "hz.window", "anchor.window", "lr_wardrobe", "lr_balcony", "lr_table"));
            salon.Anchors.Add(A("lr_balcony", 6.5f, 1.0f, AnchorTag.Trap, 1, "hz.balcony", "anchor.balcony", "lr_window"));
            salon.Props.Add(P(null, "Table", 2.5f, 2.5f, -0.12f, 1.5f, 0.45f, Wood));
            salon.Props.Add(P(null, "Chair", 1.7f, 2.9f, 0f, 0.45f, 0.55f, Wood));
            salon.Props.Add(P(null, "Chair", 3.3f, 2.1f, 0f, 0.45f, 0.55f, Wood));
            salon.Props.Add(P("wardrobe", "Wardrobe", 5.85f, 4.0f, 0f, 1.3f, 1.9f, new Color(0.4f, 0.25f, 0.15f, 1f), topple: 80f));
            salon.Props.Add(P("window", "Window", 5.95f, 1.5f, 0.35f, 0.9f, 1.0f, new Color(0.6f, 0.85f, 1f, 0.9f)));
            salon.Props.Add(P("door_frame", "DoorLintel", 3.5f, 5.95f, 1.0f, 1.0f, 0.25f, new Color(0.35f, 0.22f, 0.12f, 1f), drop: 0.9f));
            salon.Props.Add(P("balcony_rail", "BalconyRail", 6.95f, 1.0f, 0f, 1.0f, 0.5f, Metal, drop: 0.6f));
            salon.Props.Add(P("ceiling_lamp", "CeilingLamp", 2.5f, 3.4f, 1.6f, 0.5f, 0.35f, new Color(1f, 0.95f, 0.6f, 1f), drop: 1.4f));
            // Our flat (pre-quake loot only): a hidden food tin. Prep tension comes from time and bag slots only.
            salon.Items.Add(Own(I("food", 0.6f, 4.6f, new Color(0.9f, 0.7f, 0.35f, 1f), cost: 2f, surface: false, hidden: true, id: "salon_hidden_food")));
            // An empty bookshelf: searching it only costs prep seconds.
            salon.Containers.Add(Own(Box("salon_bookshelf", "bookshelf", 2.2f, 5.7f, Wood)));
            salon.Lights.Add(L("CeilingLamp", 2.5f, 3.4f, 1.6f, 3.2f));
            salon.Lights.Add(L("Window", 5.95f, 1.5f, 0.6f, 2.2f, new Color(1f, 0.86f, 0.62f, 1f)));

            RoomSpec kitchen = Art(Room(BuildingLevelsGenerator.Flat11Kitchen, 4, 4, new Color(0.7f, 0.8f, 0.78f, 1f)), RoomArt.Home, windows: "R1");
            kitchen.Props.Add(P(null, "Stove", 3.5f, 2.5f, 0f, 0.8f, 0.7f, new Color(0.75f, 0.75f, 0.78f, 1f)));
            kitchen.Hotspots.Add(Valve(3.7f, 1.6f, BuildingLevelsGenerator.F4PrepKitchen + ".c_valve", BuildingLevelsGenerator.F4Kitchen + ".c_valve"));
            kitchen.Containers.Add(Own(Box("kitchen_drawer", "drawer", 1.2f, 3.3f, Wood, "gloves")));
            // The family's first aid kit lives in the kitchen cabinet (mutfak dolabı).
            kitchen.Containers.Add(Own(Box("kitchen_cabinet", "kitchen_cabinet", 3.6f, 3.5f, Wood, "first_aid")));
            kitchen.Items.Add(Own(I("water", 2.6f, 3.4f, new Color(0.45f, 0.7f, 1f, 1f))));
            kitchen.Lights.Add(L("KitchenLamp", 2f, 2f, 1.5f, 2.8f));

            RoomSpec hall = Art(Room(BuildingLevelsGenerator.Flat11Hall, 3, 4, new Color(0.72f, 0.68f, 0.6f, 1f)), RoomArt.Home);
            hall.Props.Add(P(null, "FuseBox", 0.3f, 2.5f, 0.4f, 0.5f, 0.6f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            WallLamp(hall, 1.5f, "HallWallLamp");
            // Old emergency bag behind the coats: a second chance at a whistle for players who cut the intro short.
            hall.Items.Add(Own(I("whistle", 0.6f, 0.6f, new Color(0.95f, 0.5f, 0.2f, 1f), cost: 2f, surface: false, hidden: true, id: "hall_hidden_whistle")));
            hall.Containers.Add(Own(Box("hall_nightstand", "nightstand", 1.6f, 3.4f, Wood, "flashlight")));
            hall.Containers.Add(Own(Box("hall_toolbox", "toolbox", 0.5f, 1.8f, new Color(0.75f, 0.2f, 0.15f, 1f), "wrench")));

            // Flat 11 bedroom: two bedsheets for the rope (bed = open surface, wardrobe = 3s).
            RoomSpec bedroom = Art(Room(BuildingLevelsGenerator.Flat11Bedroom, 4, 4, new Color(0.76f, 0.72f, 0.7f, 1f)), RoomArt.Home, windows: "R2");
            bedroom.Props.Add(P(null, "Bed", 1.6f, 2.6f, -0.05f, 1.6f, 0.6f, new Color(0.85f, 0.85f, 0.9f, 1f)));
            bedroom.Items.Add(I("bedsheet", 1.6f, 2.6f, new Color(0.92f, 0.92f, 1f, 1f), id: "bedroom_bed_sheet"));
            bedroom.Containers.Add(Box("bedroom_wardrobe_sheet", "wardrobe", 3.6f, 3.3f, Wood, "bedsheet"));
            // An empty nightstand by the bed (komodin): searching it only costs time.
            bedroom.Containers.Add(Box("bedroom_nightstand", "nightstand", 0.5f, 3.4f, Wood));
            bedroom.Lights.Add(L("BedLamp", 0.6f, 3.4f, 0.8f, 2.4f));

            // Corridor: one door per flat, the stairwell door (L0), the elevator (trap) and Sevim Teyze.
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F4Corridor, 7, 2, new Color(0.6f, 0.6f, 0.62f, 1f)), RoomArt.Corridor);
            corridor.Hotspots.Add(Thing("Elevator", "f4.corridor.c_elevator", 7.0f, 1.5f, 1.0f, 1.6f, Metal));
            WallLamp(corridor, 2.5f, "CorridorWallLampA");
            WallLamp(corridor, 5.5f, "CorridorWallLampB");
            // Sevim Teyze: her dialogue starts on the way out of Flat 11; a click talks to her (or, after that, one
            // short line). She never disappears.
            corridor.Hotspots.Add(new HotspotSpec
            {
                Name = "Sevim", ChoiceKey = "f4.corridor.c_sevim", AlsoKeys = new[] { "f4.corridor.c_sevim_talk" },
                Gx = 4.6f, Gy = 1.3f, Person = true, Color = Color.white, Idle = "sevim", HideWhenGone = false
            });

            // Flat 12: the neighbours' open flat. Everything here is borrowed ("ödünç al").
            RoomSpec f12Hall = Art(Room(BuildingLevelsGenerator.Flat12, 3, 3, new Color(0.7f, 0.66f, 0.74f, 1f)), RoomArt.Home);
            f12Hall.Items.Add(I("rope", 0.8f, 0.7f, new Color(0.8f, 0.65f, 0.4f, 1f), borrowed: true));
            f12Hall.Items.Add(I("tape", 1.6f, 2.5f, new Color(0.6f, 0.6f, 0.65f, 1f), borrowed: true));
            f12Hall.Containers.Add(Lent(Box("flat12_closet_flashlight", "wardrobe", 2.4f, 2.4f, Wood, "flashlight")));
            WallLamp(f12Hall, 0.5f, "Flat12WallLamp");
            RoomSpec f12Living = Art(Room(BuildingLevelsGenerator.Flat12Living, 4, 4, new Color(0.74f, 0.7f, 0.78f, 1f), BuildingLevelsGenerator.Flat12), RoomArt.Home, windows: "R3");
            f12Living.Props.Add(P(null, "Sofa", 1.6f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.45f, 0.35f, 0.5f, 1f)));
            f12Living.Items.Add(I("cloth", 0.9f, 1.0f, new Color(0.85f, 0.85f, 0.9f, 1f), borrowed: true));
            f12Living.Items.Add(I("lighter", 2.8f, 1.2f, new Color(1f, 0.4f, 0.2f, 1f), borrowed: true));
            f12Living.Containers.Add(Lent(Box("flat12_cabinet", "cabinet", 0.6f, 3.2f, Wood, "first_aid")));
            f12Living.Containers.Add(Lent(Box("flat12_bookshelf", "bookshelf", 3.4f, 3.7f, Wood, "water")));
            RoomSpec f12Bedroom = Art(Room(BuildingLevelsGenerator.Flat12Bedroom, 4, 4, new Color(0.72f, 0.68f, 0.76f, 1f), BuildingLevelsGenerator.Flat12), RoomArt.Home, windows: "R3");
            f12Bedroom.Props.Add(P(null, "Bed", 1.6f, 2.4f, -0.05f, 1.6f, 0.6f, new Color(0.85f, 0.82f, 0.9f, 1f)));
            f12Bedroom.Containers.Add(Lent(Box("flat12_wardrobe_sheet", "wardrobe", 2.6f, 3.4f, Wood, "bedsheet")));
            f12Bedroom.Containers.Add(Lent(Box("flat12_nightstand", "nightstand", 0.5f, 3.3f, Wood)));

            Link(salon, "L3", hall, "L0");
            Link(salon, "L5", kitchen, "L0");
            Link(salon, "R2", bedroom, "L0");
            Link(hall, "R3", corridor, "L1");
            Link(corridor, "L4", f12Hall, "R1");
            Link(f12Hall, "L1", f12Living, "R1");
            Link(f12Living, "L2", f12Bedroom, "R1");
            RoomSpec f3Corridor = Find(floor3, BuildingLevelsGenerator.F3Corridor);
            StairwellDoor(corridor, "L0", f3Corridor, f3Corridor.PlayerSpawn);

            return new[] { salon, kitchen, hall, bedroom, corridor, f12Hall, f12Living, f12Bedroom };
        }

        private static IEnumerable<RoomSpec> Floor3(List<RoomSpec> floor2)
        {
            // Floor 3 corridor: aftershock placement on arrival; Cemil the trader; the stairs below collapsed (R1).
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F3Corridor, 7, 3, new Color(0.55f, 0.55f, 0.6f, 1f)), RoomArt.DamagedCorridor, gaps: "R1", windows: "L5");
            corridor.PlayerSpawn = new Vector2(0.6f, 0.6f);
            corridor.WifeSpawn = new Vector2(1.2f, 0.4f);
            corridor.ChildSpawn = new Vector2(0.4f, 1.2f);
            corridor.Anchors.Add(A("f3_inner_wall", 1.0f, 2.5f, AnchorTag.Safe, 2, null, "anchor.inner_wall", "f3_pillar"));
            corridor.Anchors.Add(A("f3_pillar", 2.4f, 2.5f, AnchorTag.Safe, 1, null, "anchor.pillar", "f3_inner_wall", "f3_lamp"));
            corridor.Anchors.Add(A("f3_lamp", 3.6f, 1.4f, AnchorTag.Trap, 1, "hz.f3_lamp", "anchor.lamp", "f3_pillar", "f3_window"));
            corridor.Anchors.Add(A("f3_window", 5.5f, 2.5f, AnchorTag.Trap, 1, "hz.f3_window", "anchor.window", "f3_lamp", "f3_stair_edge"));
            corridor.Anchors.Add(A("f3_stair_edge", 6.4f, 1.0f, AnchorTag.Trap, 1, "hz.f3_stair_edge", "anchor.stair_edge", "f3_window"));
            corridor.Props.Add(P("corridor_lamp", "CorridorLamp", 3.6f, 1.4f, 1.6f, 0.45f, 0.3f, new Color(1f, 0.95f, 0.6f, 1f), drop: 1.4f));
            corridor.Props.Add(P("corridor_window", "CorridorWindow", 5.5f, 2.95f, 0.35f, 0.9f, 1.0f, new Color(0.6f, 0.85f, 1f, 0.9f)));
            corridor.Props.Add(P("stair_edge", "StairEdge", 6.8f, 1.0f, 0f, 0.9f, 0.35f, new Color(0.45f, 0.45f, 0.5f, 1f), drop: 0.7f));
            // Decorative stairs: the flight we came down from Floor 4 (L0, by the arrival) and the collapsed flight
            // below (R1: dark, debris on top; the stair-edge hazard anchor stays in front of it).
            DecorStairs(corridor, "L0", false);
            DecorStairs(corridor, "R1", true);
            corridor.Props.Add(P("ceiling_debris", "CeilingDebris", 3.0f, 0.8f, 1.8f, 0.6f, 0.25f, new Color(0.6f, 0.58f, 0.55f, 1f), drop: 1.6f));
            corridor.Hotspots.Add(Person("Cemil", "f3.corridor.c_trader", 4.7f, 0.7f, new Color(0.75f, 0.6f, 0.35f, 1f), idle: "cemil"));
            corridor.Hotspots[corridor.Hotspots.Count - 1].Face = new Vector2(corridor.Width * 0.5f, corridor.Height * 0.5f);
            corridor.Hotspots[corridor.Hotspots.Count - 1].Face = new Vector2(corridor.Width * 0.5f, corridor.Height * 0.5f);
            WallLamp(corridor, 0.5f, "F3WallLamp");

            // Flat 9: fuse box in the hall, balcony (bedsheet rope) off the living room, hanging cables in the kitchen.
            RoomSpec f9Hall = Art(Room(BuildingLevelsGenerator.Flat9, 3, 3, new Color(0.6f, 0.64f, 0.68f, 1f)), RoomArt.Home);
            f9Hall.Props.Add(P(null, "FuseBox", 1.5f, 2.85f, 0.5f, 0.6f, 0.7f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            RoomSpec f9Living = Art(Room(BuildingLevelsGenerator.Flat9Living, 4, 4, new Color(0.62f, 0.66f, 0.7f, 1f), BuildingLevelsGenerator.Flat9), RoomArt.Home, gaps: "R0 R1");
            f9Living.ExtraTiles.Add(new Vector2(4.5f, 0.5f));
            f9Living.ExtraTiles.Add(new Vector2(4.5f, 1.5f));
            f9Living.Props.Add(P(null, "Sofa", 1.4f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.35f, 0.4f, 0.5f, 1f)));
            // F3 bedsheets (borrowed): the rope can still be made after the F3 checkpoint (every descent needs it).
            f9Living.Items.Add(I("bedsheet", 1.4f, 3.3f, new Color(0.92f, 0.92f, 1f, 1f), borrowed: true, id: "flat9_sofa_sheet"));
            f9Living.Containers.Add(Lent(Box("flat9_bookshelf", "bookshelf", 3.1f, 3.7f, Wood, "food", "bedsheet")));
            f9Living.Props.Add(P(null, "BalconyRail", 4.95f, 0.6f, 0f, 1.0f, 0.5f, Metal));
            f9Living.Ropes.Add(new RopeSpec { Id = "flat9_balcony_rope", Gx = 4.5f, Gy = 0.6f, TargetNodeId = BuildingLevelsGenerator.F2Aftershock });
            RoomSpec f9Kitchen = Art(Room(BuildingLevelsGenerator.Flat9Kitchen, 3, 3, new Color(0.6f, 0.62f, 0.6f, 1f), BuildingLevelsGenerator.Flat9), RoomArt.Home, windows: "R0");
            f9Kitchen.Props.Add(P(null, "HangingCables", 1.6f, 1.8f, 0.4f, 0.15f, 1.6f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            f9Kitchen.Props.Add(P(null, "Shaft", 1.8f, 2.4f, 0f, 1.0f, 0.3f, new Color(0.1f, 0.1f, 0.12f, 1f)));

            // Flat 10: dark (no practical lights), gas leaking from the kitchen stove, collapsed living-room floor.
            RoomSpec f10Hall = Art(Room(BuildingLevelsGenerator.Flat10, 3, 3, new Color(0.42f, 0.42f, 0.46f, 1f)), RoomArt.Home);
            RoomSpec f10Kitchen = Art(Room(BuildingLevelsGenerator.Flat10Kitchen, 3, 4, new Color(0.42f, 0.44f, 0.44f, 1f), BuildingLevelsGenerator.Flat10), RoomArt.Home);
            f10Kitchen.Props.Add(P(null, "Stove", 2.5f, 3.4f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));
            f10Kitchen.Hotspots.Add(Valve(1.7f, 3.6f, BuildingLevelsGenerator.F3Flat10 + ".c_valve"));
            RoomSpec f10Living = Art(Room(BuildingLevelsGenerator.Flat10Living, 4, 4, new Color(0.4f, 0.4f, 0.44f, 1f), BuildingLevelsGenerator.Flat10), RoomArt.Home);
            // The collapsed floor: a hole decal on the floor layer (the way down still needs the rope + harness).
            f10Living.Props.Add(P(null, "FloorHole", 2.4f, 2.5f, 0f, 1.4f, 0.25f, new Color(0.05f, 0.05f, 0.06f, 1f)));
            f10Living.Props[f10Living.Props.Count - 1].BlocksNav = true;
            f10Living.Items.Add(I("tape", 0.6f, 3.4f, new Color(0.6f, 0.6f, 0.65f, 1f), cost: 2f, surface: false, hidden: true, id: "flat10_hidden_tape"));
            f10Living.Containers.Add(Lent(Box("flat10_bookshelf", "bookshelf", 2.6f, 3.7f, Wood, "cloth", "bedsheet")));

            Link(corridor, "L1", f9Hall, "R1");
            Link(f9Hall, "L0", f9Kitchen, "R1");
            Link(f9Hall, "L2", f9Living, "R2");
            Link(corridor, "L3", f10Hall, "R1");
            Link(f10Hall, "L0", f10Kitchen, "R1");
            Link(f10Hall, "L2", f10Living, "R1");

            return new[] { corridor, f9Hall, f9Living, f9Kitchen, f10Hall, f10Kitchen, f10Living };
        }

        private static IEnumerable<RoomSpec> Floor2()
        {
            // Flat 7 living room: arrival from the cables / collapsed floor / rope; strong aftershock placement.
            RoomSpec flat7 = Art(Room(BuildingLevelsGenerator.Flat7, 5, 5, new Color(0.66f, 0.62f, 0.58f, 1f)), RoomArt.Home, windows: "R1");
            flat7.PlayerSpawn = new Vector2(0.7f, 0.7f);
            flat7.WifeSpawn = new Vector2(1.3f, 0.5f);
            flat7.ChildSpawn = new Vector2(0.5f, 1.3f);
            flat7.Anchors.Add(A("f7_desk", 2.0f, 2.0f, AnchorTag.Safe, 2, null, "anchor.table", "f7_inner_wall", "f7_bookshelf", "f7_window"));
            flat7.Anchors.Add(A("f7_inner_wall", 0.8f, 4.3f, AnchorTag.Safe, 1, null, "anchor.inner_wall", "f7_desk", "f7_bookshelf"));
            flat7.Anchors.Add(A("f7_bookshelf", 3.2f, 4.4f, AnchorTag.Trap, 1, "hz.f7_bookshelf", "anchor.bookshelf", "f7_inner_wall", "f7_desk"));
            flat7.Anchors.Add(A("f7_window", 4.4f, 1.5f, AnchorTag.Trap, 1, "hz.f7_window", "anchor.window", "f7_desk"));
            flat7.Props.Add(P(null, "Desk", 2.0f, 2.0f, -0.12f, 1.3f, 0.45f, Wood));
            flat7.Props.Add(P(null, "Chair", 2.8f, 1.5f, 0f, 0.45f, 0.55f, Wood));
            flat7.Props.Add(P("bookshelf", "Bookshelf", 3.2f, 4.85f, 0f, 1.2f, 1.8f, new Color(0.45f, 0.3f, 0.18f, 1f), topple: 80f));
            flat7.Props.Add(P("f7_window", "Window", 4.95f, 1.5f, 0.35f, 0.9f, 1.0f, new Color(0.6f, 0.85f, 1f, 0.9f)));
            flat7.Props.Add(P("f7_ceiling", "CeilingPlaster", 2.6f, 3.0f, 1.8f, 0.7f, 0.25f, new Color(0.7f, 0.68f, 0.64f, 1f), drop: 1.6f));
            flat7.Props.Add(P(null, "Cables", 0.4f, 0.4f, 0.6f, 0.15f, 1.4f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            RoomSpec f7Hall = Art(Room(BuildingLevelsGenerator.Flat7Hall, 3, 3, new Color(0.64f, 0.6f, 0.56f, 1f), BuildingLevelsGenerator.Flat7), RoomArt.Home);
            RoomSpec f7Kitchen = Art(Room(BuildingLevelsGenerator.Flat7Kitchen, 3, 4, new Color(0.62f, 0.66f, 0.64f, 1f), BuildingLevelsGenerator.Flat7), RoomArt.Home, windows: "R1");
            f7Kitchen.Props.Add(P(null, "Stove", 1.5f, 3.4f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));
            f7Kitchen.Hotspots.Add(Valve(2.3f, 3.6f, BuildingLevelsGenerator.F2Flat7 + ".c_valve"));

            // Corridor: the rabid dog blocks the stairwell door (L0).
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F2CorridorRoom, 7, 2, new Color(0.58f, 0.57f, 0.6f, 1f)), RoomArt.DamagedCorridor);
            // The rabid dog: decorative NPC (its choices are in the panel), facing the player's arrival door (L1).
            corridor.Hotspots.Add(new HotspotSpec { Name = "Dog", Gx = 1.0f, Gy = 1.0f, Person = true, Color = new Color(0.55f, 0.4f, 0.25f, 1f), Idle = "dog", Face = EdgeInside(corridor, "L1") });
            WallLamp(corridor, 2.5f, "F2WallLamp");
            // Elevators sit flush on the back-right wall line, centred on a wall segment (DoorArt fits them to it).
            corridor.Props.Add(P(null, "Elevator", 7.0f, 1.5f, 0f, 1.0f, 1.6f, Metal));

            // Flat 8: empty flat (one hidden item).
            RoomSpec f8Hall = Art(Room(BuildingLevelsGenerator.Flat8, 3, 3, new Color(0.68f, 0.64f, 0.6f, 1f)), RoomArt.Home);
            RoomSpec f8Living = Art(Room(BuildingLevelsGenerator.Flat8Living, 4, 4, new Color(0.7f, 0.66f, 0.62f, 1f), BuildingLevelsGenerator.Flat8), RoomArt.Home, windows: "R3");
            f8Living.Props.Add(P(null, "Sofa", 1.4f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.5f, 0.42f, 0.35f, 1f)));
            f8Living.Items.Add(I("water", 3.3f, 3.2f, new Color(0.45f, 0.7f, 1f, 1f), cost: 2f, surface: false, hidden: true, id: "flat8_hidden_water"));
            f8Living.Containers.Add(Lent(Box("flat8_bookshelf", "bookshelf", 3.7f, 2.5f, Wood, "food")));
            RoomSpec f8Bedroom = Art(Room(BuildingLevelsGenerator.Flat8Bedroom, 4, 4, new Color(0.7f, 0.64f, 0.62f, 1f), BuildingLevelsGenerator.Flat8), RoomArt.Home, windows: "R3");
            f8Bedroom.Props.Add(P(null, "Bed", 1.6f, 2.4f, -0.05f, 1.6f, 0.6f, new Color(0.8f, 0.8f, 0.85f, 1f)));
            f8Bedroom.Containers.Add(Lent(Box("flat8_nightstand", "nightstand", 0.5f, 3.3f, Wood)));

            // The stairwell door down to the street (L1) and the elevator trap. The mugger (Tehlikeli Adam)
            // stands in front of it: going down starts the scripted robbery, then the street.
            RoomSpec stairs = Art(Room(BuildingLevelsGenerator.F2Stairs, 3, 5, new Color(0.5f, 0.5f, 0.54f, 1f)), RoomArt.DamagedCorridor);
            stairs.PlayerSpawn = new Vector2(1.5f, 0.8f);
            stairs.Hotspots.Add(StairwellHotspot(stairs, "L1", BuildingLevelsGenerator.F2Exit + ".c_out"));
            stairs.Hotspots.Add(Thing("Elevator", "f2.exit.c_elevator", 3.0f, 1.5f, 0.6f, 1.6f, Metal));
            stairs.Hotspots.Add(new HotspotSpec { Name = SixtyOne.Dialogue.RobberSetup.ActorName, Gx = 1.6f, Gy = 3.5f, Person = true, Color = new Color(0.62f, 0.46f, 0.4f, 1f), Idle = "robber", Face = stairs.PlayerSpawn });

            RoomSpec street = Street();

            Link(flat7, "L1", f7Hall, "R1");
            Link(flat7, "R3", f7Kitchen, "L0");
            Link(f7Hall, "L1", corridor, "L1");
            Link(corridor, "L4", f8Hall, "R1");
            Link(f8Hall, "L1", f8Living, "R1");
            Link(f8Living, "L2", f8Bedroom, "R1");
            StairwellDoor(corridor, "L0", stairs, stairs.PlayerSpawn);

            return new[] { flat7, f7Hall, f7Kitchen, corridor, f8Hall, f8Living, f8Bedroom, stairs, street };
        }
    }
}
