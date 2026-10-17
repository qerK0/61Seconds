using System.Collections.Generic;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Data tables for every room of the test building (positions in iso grid cells).</summary>
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
        public enum RoomArt { Home, Corridor }

        /// <summary>A window/balcony where the bedsheet rope lowers the family to another node.</summary>
        public sealed class RopeSpec
        {
            public string Id;
            public float Gx;
            public float Gy;
            public string TargetNodeId;
        }

        public sealed class RoomSpec
        {
            public string Id;
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
        }

        private static readonly Color Warm = new Color(1f, 0.78f, 0.5f, 1f);
        private static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f, 1f);

        private static RoomSpec Room(string id, int w, int h, Color floor)
        {
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            return new RoomSpec
            {
                Id = id, Width = w, Height = h, Floor = floor,
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
            var rooms = new List<RoomSpec>();
            rooms.AddRange(Floor4());
            rooms.AddRange(Floor3());
            rooms.AddRange(Floor2());
            return rooms;
        }

        private static IEnumerable<RoomSpec> Floor4()
        {
            // Flat 11 salon: the opening-quake placement room.
            RoomSpec salon = Art(Room(BuildingLevelsGenerator.Flat11Salon, 6, 6, new Color(0.78f, 0.74f, 0.66f, 1f)), RoomArt.Home, gaps: "L3 R0", windows: "R1");
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
            salon.Props.Add(P("balcony_rail", "BalconyRail", 6.95f, 1.0f, 0f, 1.0f, 0.5f, new Color(0.5f, 0.5f, 0.55f, 1f), drop: 0.6f));
            salon.Props.Add(P("ceiling_lamp", "CeilingLamp", 2.5f, 3.4f, 1.6f, 0.5f, 0.35f, new Color(1f, 0.95f, 0.6f, 1f), drop: 1.4f));
            // Our flat (pre-quake loot only): useful gear, two traps that waste slots/time, a hidden food tin.
            salon.Items.Add(Own(I("food", 0.6f, 4.6f, new Color(0.9f, 0.7f, 0.35f, 1f), cost: 2f, surface: false, hidden: true, id: "salon_hidden_food")));
            salon.Items.Add(Own(I("first_aid", 0.4f, 3.0f, new Color(1f, 0.25f, 0.25f, 1f), cost: 3f, surface: false, id: "salon_cabinet")));
            salon.Items.Add(Own(I("laptop", 1.6f, 1.0f, new Color(0.25f, 0.25f, 0.3f, 1f))));
            salon.Items.Add(Own(I("console", 3.6f, 1.2f, new Color(0.15f, 0.15f, 0.2f, 1f))));
            salon.Lights.Add(L("CeilingLamp", 2.5f, 3.4f, 1.6f, 3.2f));
            salon.Lights.Add(L("Window", 5.95f, 1.5f, 0.6f, 2.2f, new Color(1f, 0.86f, 0.62f, 1f)));
            yield return salon;

            RoomSpec kitchen = Art(Room(BuildingLevelsGenerator.Flat11Kitchen, 4, 4, new Color(0.7f, 0.8f, 0.78f, 1f)), RoomArt.Home, gaps: "L0", windows: "R1");
            kitchen.Props.Add(P(null, "Stove", 3.5f, 2.5f, 0f, 0.8f, 0.7f, new Color(0.75f, 0.75f, 0.78f, 1f)));
            kitchen.Items.Add(Own(I("gloves", 1.2f, 3.3f, Wood, cost: 3f, surface: false, id: "kitchen_drawer")));
            kitchen.Items.Add(Own(I("water", 2.6f, 3.4f, new Color(0.45f, 0.7f, 1f, 1f))));
            kitchen.Lights.Add(L("KitchenLamp", 2f, 2f, 1.5f, 2.8f));
            yield return kitchen;

            RoomSpec hall = Art(Room(BuildingLevelsGenerator.Flat11Hall, 3, 4, new Color(0.72f, 0.68f, 0.6f, 1f)), RoomArt.Home, gaps: "R3 L0");
            hall.Props.Add(P(null, "FuseBox", 0.3f, 2.5f, 0.4f, 0.5f, 0.6f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            hall.Props.Add(P(null, "FlatDoor", 2.85f, 3.5f, 0f, 0.9f, 1.5f, Wood));
            // Old emergency bag behind the coats: a second chance at a whistle for players who cut the intro short.
            hall.Items.Add(Own(I("whistle", 0.6f, 0.6f, new Color(0.95f, 0.5f, 0.2f, 1f), cost: 2f, surface: false, hidden: true, id: "hall_hidden_whistle")));
            hall.Items.Add(Own(I("flashlight", 1.6f, 3.4f, Wood, cost: 3f, surface: false, id: "hall_nightstand")));
            hall.Items.Add(Own(I("wrench", 0.5f, 1.8f, new Color(0.55f, 0.55f, 0.6f, 1f), cost: 3f, surface: false, id: "hall_toolbox")));
            hall.Items.Add(Own(I("gold", 2.4f, 1.4f, new Color(0.9f, 0.75f, 0.2f, 1f), cost: 3f, surface: false, id: "hall_jewelry_box")));
            yield return hall;

            // Flat 11 bedroom: two bedsheets for the rope (bed = open surface, wardrobe = 3s).
            RoomSpec bedroom = Art(Room(BuildingLevelsGenerator.Flat11Bedroom, 4, 4, new Color(0.76f, 0.72f, 0.7f, 1f)), RoomArt.Home, gaps: "L0", windows: "R2");
            bedroom.Props.Add(P(null, "Bed", 1.6f, 2.6f, -0.05f, 1.6f, 0.6f, new Color(0.85f, 0.85f, 0.9f, 1f)));
            bedroom.Props.Add(P(null, "Wardrobe", 3.6f, 3.3f, 0f, 1.0f, 1.7f, Wood));
            bedroom.Items.Add(I("bedsheet", 1.6f, 2.6f, new Color(0.92f, 0.92f, 1f, 1f), id: "bedroom_bed_sheet"));
            bedroom.Items.Add(I("bedsheet", 3.4f, 3.1f, new Color(0.8f, 0.85f, 1f, 1f), cost: 3f, surface: false, id: "bedroom_wardrobe_sheet"));
            bedroom.Lights.Add(L("BedLamp", 0.6f, 3.4f, 0.8f, 2.4f));
            yield return bedroom;

            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F4Corridor, 7, 2, new Color(0.6f, 0.6f, 0.62f, 1f)), RoomArt.Corridor, gaps: "L1 L4");
            corridor.Props.Add(P(null, "Elevator", 6.6f, 1.0f, 0f, 1.0f, 1.6f, new Color(0.5f, 0.5f, 0.55f, 1f)));
            corridor.Props.Add(P(null, "StairsDown", 0.4f, 1.0f, 0f, 0.9f, 0.4f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            yield return corridor;

            // Flat 12: the neighbours' open flat. Everything here is borrowed ("ödünç al").
            RoomSpec flat12 = Art(Room(BuildingLevelsGenerator.Flat12, 4, 4, new Color(0.74f, 0.7f, 0.78f, 1f)), RoomArt.Home, gaps: "L0", windows: "R2");
            flat12.Items.Add(I("cloth", 0.8f, 1.0f, new Color(0.85f, 0.85f, 0.9f, 1f), borrowed: true));
            flat12.Items.Add(I("rope", 1.6f, 0.6f, new Color(0.8f, 0.65f, 0.4f, 1f), borrowed: true));
            flat12.Items.Add(I("tape", 2.6f, 0.8f, new Color(0.6f, 0.6f, 0.65f, 1f), borrowed: true));
            flat12.Items.Add(I("flashlight", 3.4f, 1.6f, new Color(0.3f, 0.3f, 0.35f, 1f), borrowed: true));
            flat12.Items.Add(I("lighter", 3.2f, 2.8f, new Color(1f, 0.4f, 0.2f, 1f), borrowed: true));
            flat12.Items.Add(I("bedsheet", 3.4f, 3.4f, new Color(0.85f, 0.8f, 0.95f, 1f), cost: 3f, surface: false, borrowed: true, id: "flat12_wardrobe_sheet"));
            flat12.Items.Add(I("first_aid", 1.0f, 3.2f, new Color(1f, 0.25f, 0.25f, 1f), cost: 3f, surface: false, borrowed: true, id: "flat12_cabinet"));
            yield return flat12;
        }

        private static IEnumerable<RoomSpec> Floor2()
        {
            // Flat 7: arrival from the cables / collapsed floor; strong aftershock placement; gas leak.
            RoomSpec flat7 = Art(Room(BuildingLevelsGenerator.Flat7, 5, 5, new Color(0.66f, 0.62f, 0.58f, 1f)), RoomArt.Home, gaps: "L1", windows: "R1");
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
            flat7.Props.Add(P(null, "Stove", 4.5f, 3.6f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));
            flat7.Props.Add(P(null, "Cables", 0.4f, 0.4f, 0.6f, 0.15f, 1.4f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            yield return flat7;

            // Corridor: the frightened dog blocks the stairs.
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F2CorridorRoom, 7, 2, new Color(0.58f, 0.57f, 0.6f, 1f)), RoomArt.Corridor, gaps: "L1 L4");
            corridor.Props.Add(P(null, "Dog", 1.0f, 1.0f, 0f, 0.6f, 0.45f, new Color(0.55f, 0.4f, 0.25f, 1f)));
            corridor.Props.Add(P(null, "StairsDown", 0.4f, 1.0f, 0f, 0.9f, 0.4f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            corridor.Props.Add(P(null, "Elevator", 6.6f, 1.0f, 0f, 1.0f, 1.6f, new Color(0.5f, 0.5f, 0.55f, 1f)));
            yield return corridor;

            // Flat 8: empty placeholder flat (at most one hidden item).
            RoomSpec flat8 = Art(Room(BuildingLevelsGenerator.Flat8, 4, 4, new Color(0.7f, 0.66f, 0.62f, 1f)), RoomArt.Home, gaps: "L0", windows: "R2");
            flat8.Items.Add(I("water", 3.3f, 3.2f, new Color(0.45f, 0.7f, 1f, 1f), cost: 2f, surface: false, hidden: true, id: "flat8_hidden_water"));
            yield return flat8;

            RoomSpec stairs = Art(Room(BuildingLevelsGenerator.F2Stairs, 3, 5, new Color(0.5f, 0.5f, 0.54f, 1f)), RoomArt.Corridor, gaps: "L1");
            stairs.Props.Add(P(null, "StairsDown", 1.5f, 4.3f, 0f, 1.6f, 0.5f, new Color(0.45f, 0.45f, 0.5f, 1f)));
            stairs.Props.Add(P(null, "Elevator", 2.7f, 1.5f, 0f, 0.6f, 1.6f, new Color(0.5f, 0.5f, 0.55f, 1f)));
            yield return stairs;

            RoomSpec street = Art(Room(BuildingLevelsGenerator.Street, 6, 4, new Color(0.4f, 0.42f, 0.45f, 1f)), RoomArt.Corridor, walls: false);
            street.Lights.Add(L("StreetLamp", 5.0f, 3.0f, 2.0f, 3.5f, new Color(0.8f, 0.85f, 1f, 1f)));
            yield return street;
        }

        private static IEnumerable<RoomSpec> Floor3()
        {
            // Floor 3 corridor: aftershock placement on arrival.
            RoomSpec corridor = Art(Room(BuildingLevelsGenerator.F3Corridor, 7, 3, new Color(0.55f, 0.55f, 0.6f, 1f)), RoomArt.Corridor, gaps: "L1 L3 R1", windows: "L5");
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
            yield return corridor;

            // Flat 9: hanging cables + fuse box.
            RoomSpec flat9 = Art(Room(BuildingLevelsGenerator.Flat9, 4, 4, new Color(0.62f, 0.66f, 0.7f, 1f)), RoomArt.Home, gaps: "L0 R0");
            flat9.Props.Add(P(null, "FuseBox", 0.3f, 3.0f, 0.5f, 0.6f, 0.7f, new Color(0.6f, 0.6f, 0.65f, 1f)));
            flat9.Props.Add(P(null, "HangingCables", 3.2f, 2.8f, 0.4f, 0.15f, 1.6f, new Color(0.15f, 0.15f, 0.15f, 1f)));
            flat9.Props.Add(P(null, "Shaft", 3.4f, 3.4f, 0f, 1.0f, 0.3f, new Color(0.1f, 0.1f, 0.12f, 1f)));
            // Balcony: tie the bedsheet rope here and climb down to Flat 7.
            flat9.ExtraTiles.Add(new Vector2(4.5f, 0.5f));
            flat9.ExtraTiles.Add(new Vector2(4.5f, 1.5f));
            flat9.Props.Add(P(null, "BalconyRail", 4.95f, 0.6f, 0f, 1.0f, 0.5f, new Color(0.5f, 0.5f, 0.55f, 1f)));
            flat9.Ropes.Add(new RopeSpec { Id = "flat9_balcony_rope", Gx = 4.5f, Gy = 0.6f, TargetNodeId = BuildingLevelsGenerator.F2Aftershock });
            yield return flat9;

            // Flat 10: dark (no practical lights), stove leaking gas, collapsed floor.
            RoomSpec flat10 = Art(Room(BuildingLevelsGenerator.Flat10, 4, 4, new Color(0.42f, 0.42f, 0.46f, 1f)), RoomArt.Home, gaps: "L0");
            flat10.Props.Add(P(null, "Stove", 3.5f, 1.2f, 0f, 0.8f, 0.7f, new Color(0.6f, 0.6f, 0.62f, 1f)));
            flat10.Items.Add(I("tape", 0.6f, 3.4f, new Color(0.6f, 0.6f, 0.65f, 1f), cost: 2f, surface: false, hidden: true, id: "flat10_hidden_tape"));
            flat10.Props.Add(P(null, "CollapsedFloor", 1.6f, 2.6f, -0.05f, 1.4f, 0.25f, new Color(0.05f, 0.05f, 0.06f, 1f)));
            yield return flat10;
        }
    }
}
