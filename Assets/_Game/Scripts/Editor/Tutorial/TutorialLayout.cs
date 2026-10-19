using System.Collections.Generic;
using SixtyOne.Tutorial;
using UnityEngine;
using static SixtyOne.Editor.BuildingLayout;

namespace SixtyOne.Editor
{
    /// <summary>
    /// The tutorial flat's own room tables (grid cells, the same RoomSpec format and helpers as BuildingLayout, but
    /// never RoomLayoutOverrides): a salon (floor marker, the dolap, the sturdy table, the window) and an entrance hall
    /// with the flat's exit door. One zone: the hall door switches rooms directly.
    /// </summary>
    public static class TutorialLayout
    {
        private static readonly Color Wood = new Color(0.55f, 0.38f, 0.22f, 1f);

        /// <summary>Grid point of the walking step's floor marker (salon).</summary>
        public static readonly Vector2 Marker = new Vector2(3.5f, 1.2f);

        /// <summary>Wall edge of the flat's exit door (hall, back-left wall).</summary>
        public const string ExitEdge = "L1";

        /// <summary>Hotspot name of the exit door (the builder looks it up).</summary>
        public const string ExitName = "ExitDoor";

        public static List<RoomSpec> Rooms()
        {
            RoomSpec salon = Art(Room(TutorialIds.Salon, 6, 5, new Color(0.78f, 0.74f, 0.66f, 1f)), RoomArt.Home, windows: "R1");
            salon.PlayerSpawn = new Vector2(0.9f, 0.9f);
            salon.WifeSpawn = new Vector2(1.5f, 0.5f);
            salon.ChildSpawn = new Vector2(0.5f, 1.5f);
            salon.Props.Add(P(null, "Table", 2.5f, 2.6f, -0.12f, 1.5f, 0.45f, Wood));
            salon.Props.Add(P(null, "Chair", 1.7f, 3.0f, 0f, 0.45f, 0.55f, Wood));
            salon.Props.Add(P(null, "Chair", 3.3f, 2.2f, 0f, 0.45f, 0.55f, Wood));
            salon.Props.Add(P(null, "Sofa", 5.1f, 3.9f, 0f, 1.2f, 0.6f, new Color(0.45f, 0.3f, 0.3f, 1f)));
            salon.Props.Add(P(null, "CeilingLamp", 2.5f, 3.4f, 1.6f, 0.5f, 0.35f, new Color(1f, 0.95f, 0.6f, 1f), drop: 1.4f));

            // Step 2: the dolap with the craft ingredients and the two pocket items.
            ContainerSpec dolap = Box(TutorialIds.Container, "cabinet", 1.0f, 4.5f, Wood, TutorialIds.LootItems);
            dolap.Cost = 0f;
            dolap.Seconds = 1.2f;
            salon.Containers.Add(dolap);

            salon.Lights.Add(L("CeilingLamp", 2.5f, 3.4f, 1.6f, 3.2f));
            salon.Lights.Add(L("Window", 5.95f, 1.5f, 0.6f, 2.2f, new Color(1f, 0.86f, 0.62f, 1f)));

            RoomSpec hall = Art(Room(TutorialIds.Hall, 3, 4, new Color(0.72f, 0.68f, 0.6f, 1f), TutorialIds.Salon), RoomArt.Home);
            hall.Props.Add(P(null, "Nightstand", 2.6f, 3.4f, 0f, 0.55f, 0.55f, Wood));
            WallLamp(hall, 2.5f, "HallWallLamp");

            // Step 6: the flat's door plays the exit choice of the post-quake node.
            Vector2 exit = EdgeCenter(hall, ExitEdge);
            hall.Gaps.Add(ExitEdge);
            hall.Hotspots.Add(new HotspotSpec { Name = ExitName, ChoiceKey = TutorialIds.ChoiceExit, Gx = exit.x, Gy = exit.y, Art = "Door", Edge = ExitEdge });

            Link(salon, "L3", hall, "R1");

            var rooms = new List<RoomSpec> { salon, hall };
            foreach (RoomSpec room in rooms) AssignPropIds(room);
            return rooms;
        }
    }
}
