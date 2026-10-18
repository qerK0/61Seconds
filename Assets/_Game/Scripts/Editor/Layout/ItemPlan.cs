using System.Collections.Generic;

namespace SixtyOne.Editor
{
    /// <summary>
    /// What every container holds (applied after the layout overrides, keyed by container id; ids missing from a
    /// room are skipped). Generous on purpose: food, water and the progression gear show up in several flats.
    /// Flat 11 is ours; every other flat's contents are borrowed (the container's Borrowed flag).
    /// </summary>
    public static class ItemPlan
    {
        private static readonly Dictionary<string, string[]> Contents = new Dictionary<string, string[]>
        {
            // Flat 11 (ours)
            { "kitchen_cabinet", new[] { "water", "water", "food", "first_aid" } },
            { "kitchen_drawer", new[] { "gloves", "lighter" } },
            { "hall_toolbox", new[] { "wrench", "tape" } },
            { "hall_nightstand", new[] { "flashlight" } },
            { "bedroom_wardrobe_sheet", new[] { "bedsheet", "bedsheet", "cloth", "child_harness" } },
            { "bedroom_nightstand", new[] { "food" } },
            { "cabinet_6b054", new[] { "water" } },
            // Flat 12
            { "flat12_cabinet", new[] { "first_aid", "lighter" } },
            { "flat12_bookshelf", new[] { "water", "food" } },
            { "flat12_wardrobe_sheet", new[] { "bedsheet", "child_harness" } },
            { "flat12_nightstand", new[] { "food" } },
            // Flat 9
            { "flat9_bookshelf", new[] { "food", "water", "bedsheet" } },
            { "wardrobe_8cb45", new[] { "bedsheet", "cloth", "tape", "gloves" } },
            // Flat 10 (dark)
            { "flat10_bookshelf", new[] { "food", "water", "cloth", "bedsheet" } },
            // Flat 7
            { "flat7_kitchen_cabinet", new[] { "water", "first_aid" } },
            // Flat 8
            { "flat8_bookshelf", new[] { "food", "water" } },
            { "flat8_nightstand", new[] { "cloth", "food", "bedsheet" } }
        };

        /// <summary>Fills the planned containers of every room.</summary>
        public static void Apply(List<BuildingLayout.RoomSpec> rooms)
        {
            foreach (BuildingLayout.RoomSpec room in rooms)
            {
                foreach (BuildingLayout.ContainerSpec c in room.Containers)
                {
                    if (Contents.TryGetValue(c.Id, out string[] items)) c.Items = (string[])items.Clone();
                }
            }
        }

        /// <summary>Every item id the run can find (pickups, hidden items, container contents), by room depth.</summary>
        public static List<KeyValuePair<int, string>> Sources(List<BuildingLayout.RoomSpec> rooms)
        {
            var list = new List<KeyValuePair<int, string>>();
            foreach (BuildingLayout.RoomSpec room in rooms)
            {
                foreach (BuildingLayout.ItemSpec i in room.Items) list.Add(new KeyValuePair<int, string>(room.Depth, i.ItemId));
                foreach (BuildingLayout.ContainerSpec c in room.Containers)
                {
                    foreach (string id in c.Items) list.Add(new KeyValuePair<int, string>(room.Depth, id));
                    foreach (string id in c.Hidden) list.Add(new KeyValuePair<int, string>(room.Depth, id));
                }
            }

            return list;
        }

        /// <summary>
        /// Progression problems: every critical item is findable on its floor or above before it is needed
        /// (depth 0 = floor 4, 1 = floor 3).
        /// </summary>
        public static List<string> Check(List<BuildingLayout.RoomSpec> rooms)
        {
            var problems = new List<string>();
            List<KeyValuePair<int, string>> sources = Sources(rooms);
            int Count(string id, int maxDepth) => sources.FindAll(s => s.Value == id && s.Key <= maxDepth).Count;
            if (Count("bedsheet", 1) < 3) problems.Add("items: fewer than 3 bedsheets on floors 4-3 (bedsheet rope).");
            if (Count("child_harness", 1) < 1 && (Count("cloth", 1) < 1 || Count("rope", 1) < 1)) problems.Add("items: no child harness (or cloth + rope) on floors 4-3.");
            if (Count("gloves", 1) < 1 && (Count("cloth", 1) < 1 || Count("tape", 1) < 1)) problems.Add("items: no gloves or cloth + tape on floors 4-3 (fuse box).");
            if (Count("flashlight", 0) < 1) problems.Add("items: no flashlight on floor 4 (Flat 10 is dark).");
            if (Count("whistle", 1) < 1) problems.Add("items: no whistle on floors 4-3 (the dog).");
            if (Count("wrench", 0) < 1) problems.Add("items: no wrench on floor 4 (Flat 11 valve).");
            if (Count("food", 9) < 3) problems.Add("items: fewer than 3 food in the run.");
            if (Count("water", 9) < 3) problems.Add("items: fewer than 3 water in the run.");
            return problems;
        }
    }
}
