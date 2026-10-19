using System;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Items
{
    /// <summary>Pure trader decision.</summary>
    public static class TraderLogic
    {
        /// <summary>Item the trader hands over (bag and key items both count as owned), or null if the profile is empty.</summary>
        public static Item Pick(TraderProfile profile, GameState state)
        {
            return Pick(profile, id => StateOps.HasItem(state, id));
        }

        /// <summary>Item the trader hands over for this bag alone.</summary>
        public static Item Pick(TraderProfile profile, Inventory inventory)
        {
            return Pick(profile, inventory.Has);
        }

        private static Item Pick(TraderProfile profile, Func<string, bool> owns)
        {
            if (profile == null) return null;
            Item missing = FirstMissing(profile.Needs, owns);
            if (missing != null) return missing;
            missing = FirstMissing(profile.Fallback, owns);
            if (missing != null) return missing;
            return profile.Fallback.Count > 0 ? profile.Fallback[0] : null;
        }

        private static Item FirstMissing(IReadOnlyList<Item> items, Func<string, bool> owns)
        {
            foreach (Item item in items)
            {
                if (item != null && !owns(item.Id)) return item;
            }

            return null;
        }
    }
}
