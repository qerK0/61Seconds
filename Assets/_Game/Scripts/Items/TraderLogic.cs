using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Items
{
    /// <summary>Pure trader decision.</summary>
    public static class TraderLogic
    {
        /// <summary>Item the trader hands over for this inventory, or null if the profile is empty.</summary>
        public static Item Pick(TraderProfile profile, Inventory inventory)
        {
            if (profile == null) return null;
            Item missing = FirstMissing(profile.Needs, inventory);
            if (missing != null) return missing;
            missing = FirstMissing(profile.Fallback, inventory);
            if (missing != null) return missing;
            return profile.Fallback.Count > 0 ? profile.Fallback[0] : null;
        }

        private static Item FirstMissing(IReadOnlyList<Item> items, Inventory inventory)
        {
            foreach (Item item in items)
            {
                if (item != null && !inventory.Has(item.Id)) return item;
            }

            return null;
        }
    }
}
