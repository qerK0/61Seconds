using System.Collections.Generic;
using SixtyOne.Placement;
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
            public string PropId;
            public string Name;
            public float Gx;
            public float Gy;
            public float Lift;
            public Vector2 Scale;
            public Color Color;
            public float Topple = -80f;
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
            public Color Color = Color.white;
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
        public enum RoomArt { Home, Corridor, DamagedCorridor }

        /// <summary>A window/balcony where the bedsheet rope lowers the family to another node.</summary>
        public sealed class RopeSpec
        {
            public string Id;
            public float Gx;
            public float Gy;
            public string TargetNodeId;
        }

        /// <summary>A door on an edge of this room leading to another room (arrival = just inside it there).</summary>
        public sealed class DoorSpec
        {
            /// <summary>Edge code: L/R = back-left/back-right wall gap, F/G = front-right/front-left floor exit.</summary>
            public string Edge;
            public string TargetRoom;
            /// <summary>Grid point in the target room where the player appears.</summary>
            public Vector2 Arrival;
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
            /// <summary>Idle frame set ("sevim") looped on the body instead of the placeholder; null = placeholder.</summary>
            public string Idle;
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
            public List<LightSpec> Lights = new List<LightSpec>();
            public List<Vector2> ExtraTiles = new List<Vector2>();
            public List<RopeSpec> Ropes = new List<RopeSpec>();
            public List<DoorSpec> Doors = new List<DoorSpec>();
            public List<HotspotSpec> Hotspots = new List<HotspotSpec>();
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
            OneWay(a, edgeA, b, EdgeInside(b, edgeB));
            OneWay(b, edgeB, a, EdgeInside(a, edgeA));
        }

        /// <summary>One-way exit (stairs down): no door back.</summary>
        private static void OneWay(RoomSpec from, string edge, RoomSpec to, Vector2 arrival)
        {
            from.Doors.Add(new DoorSpec { Edge = edge, TargetRoom = to.Id, Arrival = arrival });
            if (IsWallEdge(edge) && !from.Gaps.Contains(edge)) from.Gaps.Add(edge);
        }

        private static HotspotSpec Person(string name, string choiceKey, float gx, float gy, Color color, bool hideWhenGone = true)
        {
            return new HotspotSpec { Name = name, ChoiceKey = choiceKey, Gx = gx, Gy = gy, Person = true, Color = color, HideWhenGone = hideWhenGone };
        }

        private static HotspotSpec Thing(string name, string choiceKey, float gx, float gy, float sx, float sy, Color color)
        {
            return new HotspotSpec { Name = name, ChoiceKey = choiceKey, Gx = gx, Gy = gy, Scale = new Vector2(sx, sy), Color = color };
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

        private static LightSpec L(string name, float gx, float gy, float lift, float radius, Color? color = null)
        {
            return new LightSpec { Name = name, Gx = gx, Gy = gy, Lift = lift, Radius = radius, Color = color ?? Warm };
        }

        /// <summary>All rooms, top floor first.</summary>
        public static List<RoomSpec> Rooms()
        {
            var floor2 = new List<RoomSpec>(Floor2());
            var floor3 = new List<RoomSpec>(Floor3(floor2));
            var rooms = new List<RoomSpec>();
            rooms.AddRange(Floor4(floor3));
            rooms.AddRange(floor3);
            rooms.AddRange(floor2);
            return rooms;
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
            salon.Props.Add(P("wardrobe", "Wardrobe", 5.85f, 4.0f, 0f, 1.3f, 1.9f, new Color(0.4f, 0.25f, 0.15f, 1f), topple: 80f));
            salon.Props.Add(P("window", "Window", 5.95f, 1.5f, 0.35f, 0.9f, 1.0f, new Color(0.6f, 0.85f, 1f, 0.9f)));
            salon.Props.Add(P("door_frame", "DoorLintel", 3.5f, 5.95f, 1.0f, 1.0f, 0.25f, new Color(0.35f, 0.22f, 0.12f, 1f), drop: 0.9f));
            salon.Props.Add(P("balcony_rail", "BalconyRail", 6.95f, 1.0f, 0f, 1.0f, 0.5f, Metal, drop: 0.6f));
            salon.Props.Add(P("ceiling_lamp", "CeilingLamp", 2.5f, 3.4f, 1.6f, 0.5f, 0.35f, new Color(1f, 0.95f, 0.6f, 1f), drop: 1.4f));
            // Our flat (pre-quake loot only): useful gear, two traps that waste slots/time, a hidden food tin.
            salon.Items.Add(Own(I("food", 0.6f, 4.6f, new Color(0.9f, 0.7f, 0.35f, 1f), cost: 2f, surface: false, hidden: true, id: "salon_hidden_food")));
            salon.Items.Add(Own(I("first_aid", 0.4f, 3.0f, new Color(1f, 0.25f, 0.25f, 1f), cost: 3f, surface: false, id: "salon_cabinet")));
            salon.Items.Add(Own(I("laptop", 1.6f, 1.0f, new Color(0.25f, 0.25f, 0.3f, 1f))));
            salon.Items.Add(Own(I("console", 3.6f, 1.2f, new Color(0.15f, 0.15f, 0.2f, 1f))));
            salon.Lights.Add(L("CeilingLamp", 2.5f, 3.4f, 1.6f, 3.2f));
            salon.Lights.Add(L("Window", 5.95f, 1.5f, 0.6f, 2.2f, new Color(1f, 0.86f, 0.62f, 1f)));

            RoomSpec kitchen = Art(Room(BuildingLevelsGenerator.Flat11Kitchen, 4, 4, new Color(0.7f, 0.8f, 0.78f, 1f)), RoomArt.Home, windows: "R1");
            kitchen.Props.Add(P(null, "Stove", 3.5f, 2.5f, 0f, 0.8f, 0.7f, new Color(0.75f, 0.75f, 0.78f, 1f)));
            kitchen.Items.Add(Own(I("gloves", 1.2f, 3.3f, Wood, cost: 3f, surface: false, id: "kitchen_drawer")));
            kitchen.Items.Add(Own(I("water", 2.6f, 3.4f, new Color(0.45f, 0.7f, 1f, 1f))));
            kitchen.Lights.Add(L("KitchenLamp", 2f, 2f, 1.5f, 2.8f));

            RoomSpec hall = Art(Room(BuildingLevelsGenerator.Flat11Hall, 3, 4, new Color(0.72f, 0.68f, 0.6f, 1f)), RoomArt.Home);
            hall.Props.Add(P(null, "FuseBox", 0.3f, 2.5f, 0.4f, 0.5f, 0.6f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            // Old emergency bag behind the coats: a second chance at a whistle for players who cut the intro short.
            hall.Items.Add(Own(I("whistle", 0.6f, 0.6f, new Color(0.95f, 0.5f, 0.2f, 1f), cost: 2f, surface: false, hidden: true, id: "hall_hidden_whistle")));
            hall.Items.Add(Own(I("flashlight", 1.6f, 3.4f, Wood, cost: 3f, surface: false, id: "hall_nightstand")));
            hall.Items.Add(Own(I("wrench", 0.5f, 1.8f, new Color(0.55f, 0.55f, 0.6f, 1f), cost: 3f, surface: false, id: "hall_toolbox")));
            hall.Items.Add(Own(I("gold", 2.4f, 1.4f, new Color(0.9f, 0.75f, 0.2f, 1f), cost: 3f, surface: false, id: "hall_jewelry_box")));

            // Flat 11 bedroom: two bedsheets for the rope (bed = open surface, wardrobe = 3s).
            RoomSpec bedroom = Art(Room(BuildingLevelsGenerator.Flat11Bedroom, 4, 4, new Color(0.76f, 0.72f, 0.7f, 1f)), RoomArt.Home, windows: "R2");
            bedroom.Props.Add(P(null, "Bed", 1.6f, 2.6f, -0.05f, 1.6f, 0.6f, new Color(0.85f, 0.85f, 0.9f, 1f)));
            bedroom.Props.Add(P(null, "Wardrobe", 3.6f, 3.3f, 0f, 1.0f, 1.7f, Wood));
            bedroom.Items.Add(I("bedsheet", 1.6f, 2.6f, new Color(0.92f, 0.92f, 1f, 1f), id: "bedroom_bed_sheet"));
            bedroom.Items.Add(I("bedsheet", 3.4f, 3.1f, new Color(0.8f, 0.85f, 1f, 1f), cost: 3f, surface: false, id: "bedroom_wardrobe_sheet"));
            bedroom.Lights.Add(L("BedLamp", 0.6f, 3.4f, 0.8f, 2.4f));

            // Corridor: one door per flat, the stairs down (front-left exit), the elevator (trap) and Hasan Amca.
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F4Corridor, 7, 2, new Color(0.6f, 0.6f, 0.62f, 1f)), RoomArt.Corridor);
            corridor.Props.Add(P(null, "StairsDown", 0.35f, 1.5f, 0f, 0.9f, 0.4f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            corridor.Hotspots.Add(Thing("Elevator", "f4.corridor.c_elevator", 6.6f, 1.0f, 1.0f, 1.6f, Metal));
            corridor.Hotspots.Add(Person("Hasan", "f4.corridor.c_talk", 3.0f, 1.2f, new Color(0.55f, 0.7f, 0.5f, 1f)));
            // Sevim Teyze: decorative (her dialogue starts on the way out of Flat 11), idle-animated.
            corridor.Hotspots.Add(new HotspotSpec { Name = "Sevim", Gx = 4.6f, Gy = 1.3f, Person = true, Color = Color.white, Idle = "sevim" });

            // Flat 12: the neighbours' open flat. Everything here is borrowed ("ödünç al").
            RoomSpec f12Hall = Art(Room(BuildingLevelsGenerator.Flat12, 3, 3, new Color(0.7f, 0.66f, 0.74f, 1f)), RoomArt.Home);
            f12Hall.Items.Add(I("rope", 0.8f, 0.7f, new Color(0.8f, 0.65f, 0.4f, 1f), borrowed: true));
            f12Hall.Items.Add(I("tape", 1.6f, 2.5f, new Color(0.6f, 0.6f, 0.65f, 1f), borrowed: true));
            f12Hall.Items.Add(I("flashlight", 2.4f, 2.4f, new Color(0.3f, 0.3f, 0.35f, 1f), cost: 3f, surface: false, borrowed: true, id: "flat12_closet_flashlight"));
            RoomSpec f12Living = Art(Room(BuildingLevelsGenerator.Flat12Living, 4, 4, new Color(0.74f, 0.7f, 0.78f, 1f), BuildingLevelsGenerator.Flat12), RoomArt.Home, windows: "R3");
            f12Living.Props.Add(P(null, "Sofa", 1.6f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.45f, 0.35f, 0.5f, 1f)));
            f12Living.Items.Add(I("cloth", 0.9f, 1.0f, new Color(0.85f, 0.85f, 0.9f, 1f), borrowed: true));
            f12Living.Items.Add(I("lighter", 2.8f, 1.2f, new Color(1f, 0.4f, 0.2f, 1f), borrowed: true));
            f12Living.Items.Add(I("first_aid", 0.6f, 3.2f, new Color(1f, 0.25f, 0.25f, 1f), cost: 3f, surface: false, borrowed: true, id: "flat12_cabinet"));
            RoomSpec f12Bedroom = Art(Room(BuildingLevelsGenerator.Flat12Bedroom, 4, 4, new Color(0.72f, 0.68f, 0.76f, 1f), BuildingLevelsGenerator.Flat12), RoomArt.Home, windows: "R3");
            f12Bedroom.Props.Add(P(null, "Bed", 1.6f, 2.4f, -0.05f, 1.6f, 0.6f, new Color(0.85f, 0.82f, 0.9f, 1f)));
            f12Bedroom.Items.Add(I("bedsheet", 2.6f, 3.4f, new Color(0.85f, 0.8f, 0.95f, 1f), cost: 3f, surface: false, borrowed: true, id: "flat12_wardrobe_sheet"));

            Link(salon, "L3", hall, "L0");
            Link(salon, "L5", kitchen, "L0");
            Link(salon, "R2", bedroom, "L0");
            Link(hall, "R3", corridor, "L1");
            Link(corridor, "L4", f12Hall, "R1");
            Link(f12Hall, "L1", f12Living, "R1");
            Link(f12Living, "L2", f12Bedroom, "R1");
            RoomSpec f3Corridor = Find(floor3, BuildingLevelsGenerator.F3Corridor);
            OneWay(corridor, "G1", f3Corridor, f3Corridor.PlayerSpawn);

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
            corridor.Props.Add(P("ceiling_debris", "CeilingDebris", 3.0f, 0.8f, 1.8f, 0.6f, 0.25f, new Color(0.6f, 0.58f, 0.55f, 1f), drop: 1.6f));
            corridor.Hotspots.Add(Person("Cemil", "f3.corridor.c_trader", 4.7f, 0.7f, new Color(0.75f, 0.6f, 0.35f, 1f)));

            // Flat 9: fuse box in the hall, balcony (bedsheet rope) off the living room, hanging cables in the kitchen.
            RoomSpec f9Hall = Art(Room(BuildingLevelsGenerator.Flat9, 3, 3, new Color(0.6f, 0.64f, 0.68f, 1f)), RoomArt.Home);
            f9Hall.Props.Add(P(null, "FuseBox", 1.5f, 2.85f, 0.5f, 0.6f, 0.7f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            RoomSpec f9Living = Art(Room(BuildingLevelsGenerator.Flat9Living, 4, 4, new Color(0.62f, 0.66f, 0.7f, 1f), BuildingLevelsGenerator.Flat9), RoomArt.Home, gaps: "R0 R1");
            f9Living.ExtraTiles.Add(new Vector2(4.5f, 0.5f));
            f9Living.ExtraTiles.Add(new Vector2(4.5f, 1.5f));
            f9Living.Props.Add(P(null, "Sofa", 1.4f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.35f, 0.4f, 0.5f, 1f)));
            f9Living.Props.Add(P(null, "BalconyRail", 4.95f, 0.6f, 0f, 1.0f, 0.5f, Metal));
            f9Living.Ropes.Add(new RopeSpec { Id = "flat9_balcony_rope", Gx = 4.5f, Gy = 0.6f, TargetNodeId = BuildingLevelsGenerator.F2Aftershock });
            RoomSpec f9Kitchen = Art(Room(BuildingLevelsGenerator.Flat9Kitchen, 3, 3, new Color(0.6f, 0.62f, 0.6f, 1f), BuildingLevelsGenerator.Flat9), RoomArt.Home, windows: "R0");
            f9Kitchen.Props.Add(P(null, "HangingCables", 1.6f, 1.8f, 0.4f, 0.15f, 1.6f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            f9Kitchen.Props.Add(P(null, "Shaft", 1.8f, 2.4f, 0f, 1.0f, 0.3f, new Color(0.1f, 0.1f, 0.12f, 1f)));

            // Flat 10: dark (no practical lights), gas leaking from the kitchen stove, collapsed living-room floor.
            RoomSpec f10Hall = Art(Room(BuildingLevelsGenerator.Flat10, 3, 3, new Color(0.42f, 0.42f, 0.46f, 1f)), RoomArt.Home);
            RoomSpec f10Kitchen = Art(Room(BuildingLevelsGenerator.Flat10Kitchen, 3, 4, new Color(0.42f, 0.44f, 0.44f, 1f), BuildingLevelsGenerator.Flat10), RoomArt.Home);
            f10Kitchen.Props.Add(P(null, "Stove", 2.5f, 3.4f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));
            RoomSpec f10Living = Art(Room(BuildingLevelsGenerator.Flat10Living, 4, 4, new Color(0.4f, 0.4f, 0.44f, 1f), BuildingLevelsGenerator.Flat10), RoomArt.Home);
            f10Living.Props.Add(P(null, "CollapsedFloor", 2.0f, 2.2f, -0.05f, 1.4f, 0.25f, new Color(0.05f, 0.05f, 0.06f, 1f)));
            f10Living.Items.Add(I("tape", 0.6f, 3.4f, new Color(0.6f, 0.6f, 0.65f, 1f), cost: 2f, surface: false, hidden: true, id: "flat10_hidden_tape"));

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
            flat7.Props.Add(P("bookshelf", "Bookshelf", 3.2f, 4.85f, 0f, 1.2f, 1.8f, new Color(0.45f, 0.3f, 0.18f, 1f), topple: 80f));
            flat7.Props.Add(P("f7_window", "Window", 4.95f, 1.5f, 0.35f, 0.9f, 1.0f, new Color(0.6f, 0.85f, 1f, 0.9f)));
            flat7.Props.Add(P("f7_ceiling", "CeilingPlaster", 2.6f, 3.0f, 1.8f, 0.7f, 0.25f, new Color(0.7f, 0.68f, 0.64f, 1f), drop: 1.6f));
            flat7.Props.Add(P(null, "Cables", 0.4f, 0.4f, 0.6f, 0.15f, 1.4f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            RoomSpec f7Hall = Art(Room(BuildingLevelsGenerator.Flat7Hall, 3, 3, new Color(0.64f, 0.6f, 0.56f, 1f), BuildingLevelsGenerator.Flat7), RoomArt.Home);
            RoomSpec f7Kitchen = Art(Room(BuildingLevelsGenerator.Flat7Kitchen, 3, 4, new Color(0.62f, 0.66f, 0.64f, 1f), BuildingLevelsGenerator.Flat7), RoomArt.Home, windows: "R1");
            f7Kitchen.Props.Add(P(null, "Stove", 1.5f, 3.4f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));

            // Corridor: the frightened dog blocks the stairs (front-left exit).
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F2CorridorRoom, 7, 2, new Color(0.58f, 0.57f, 0.6f, 1f)), RoomArt.DamagedCorridor);
            corridor.Props.Add(P(null, "Dog", 1.0f, 1.0f, 0f, 0.6f, 0.45f, new Color(0.55f, 0.4f, 0.25f, 1f)));
            corridor.Props.Add(P(null, "StairsDown", 0.35f, 1.5f, 0f, 0.9f, 0.4f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            corridor.Props.Add(P(null, "Elevator", 6.6f, 1.0f, 0f, 1.0f, 1.6f, Metal));

            // Flat 8: empty flat (one hidden item).
            RoomSpec f8Hall = Art(Room(BuildingLevelsGenerator.Flat8, 3, 3, new Color(0.68f, 0.64f, 0.6f, 1f)), RoomArt.Home);
            RoomSpec f8Living = Art(Room(BuildingLevelsGenerator.Flat8Living, 4, 4, new Color(0.7f, 0.66f, 0.62f, 1f), BuildingLevelsGenerator.Flat8), RoomArt.Home, windows: "R3");
            f8Living.Props.Add(P(null, "Sofa", 1.4f, 3.3f, -0.05f, 1.5f, 0.5f, new Color(0.5f, 0.42f, 0.35f, 1f)));
            f8Living.Items.Add(I("water", 3.3f, 3.2f, new Color(0.45f, 0.7f, 1f, 1f), cost: 2f, surface: false, hidden: true, id: "flat8_hidden_water"));
            RoomSpec f8Bedroom = Art(Room(BuildingLevelsGenerator.Flat8Bedroom, 4, 4, new Color(0.7f, 0.64f, 0.62f, 1f), BuildingLevelsGenerator.Flat8), RoomArt.Home, windows: "R3");
            f8Bedroom.Props.Add(P(null, "Bed", 1.6f, 2.4f, -0.05f, 1.6f, 0.6f, new Color(0.8f, 0.8f, 0.85f, 1f)));

            // Stairs down to the street (L1) and the elevator trap.
            RoomSpec stairs = Art(Room(BuildingLevelsGenerator.F2Stairs, 3, 5, new Color(0.5f, 0.5f, 0.54f, 1f)), RoomArt.DamagedCorridor);
            stairs.PlayerSpawn = new Vector2(1.5f, 0.8f);
            stairs.Props.Add(P(null, "StairsDown", 1.5f, 4.3f, 0f, 1.6f, 0.5f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            stairs.Hotspots.Add(Thing("Elevator", "f2.exit.c_elevator", 2.7f, 1.5f, 0.6f, 1.6f, Metal));

            RoomSpec street = Art(Room(BuildingLevelsGenerator.Street, 6, 4, new Color(0.4f, 0.42f, 0.45f, 1f)), RoomArt.Corridor, walls: false);
            street.Lights.Add(L("StreetLamp", 5.0f, 3.0f, 2.0f, 3.5f, new Color(0.8f, 0.85f, 1f, 1f)));

            Link(flat7, "L1", f7Hall, "R1");
            Link(flat7, "R3", f7Kitchen, "L0");
            Link(f7Hall, "L1", corridor, "L1");
            Link(corridor, "L4", f8Hall, "R1");
            Link(f8Hall, "L1", f8Living, "R1");
            Link(f8Living, "L2", f8Bedroom, "R1");
            OneWay(corridor, "G1", stairs, stairs.PlayerSpawn);
            OneWay(stairs, "L1", street, street.PlayerSpawn);

            return new[] { flat7, f7Hall, f7Kitchen, corridor, f8Hall, f8Living, f8Bedroom, stairs, street };
        }
    }
}
