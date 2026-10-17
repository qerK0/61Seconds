using System;
using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Items
{
    /// <summary>
    /// What the frightened man on the F2 stairs takes (pure, unit-tested): every bag item in the take-all list
    /// (all the food); if there was none, up to fallbackCount of the fallback item (one water); if neither, nothing.
    /// Key items are never taken. The borrow ledger is not touched.
    /// </summary>
    public static class RobberLogic
    {
        /// <summary>Removes the items from the bag and returns their ids in the order taken.</summary>
        public static List<string> Take(GameState state, ICollection<string> takeAll, string fallbackId, int fallbackCount,
            bool excludeKeyItems, Func<string, bool> isKeyItem)
        {
            var taken = new List<string>();
            if (state == null) return taken;

            var bag = new List<string>(state.Inventory.Items);
            foreach (string id in bag)
            {
                if (takeAll == null || !takeAll.Contains(id)) continue;
                if (excludeKeyItems && IsKey(state, id, isKeyItem)) continue;
                if (StateOps.RemoveItem(state, id)) taken.Add(id);
            }

            if (taken.Count > 0 || string.IsNullOrEmpty(fallbackId) || fallbackCount <= 0) return taken;
            if (excludeKeyItems && IsKey(state, fallbackId, isKeyItem)) return taken;
            for (int i = 0; i < fallbackCount && state.Inventory.Has(fallbackId); i++)
            {
                if (StateOps.RemoveItem(state, fallbackId)) taken.Add(fallbackId);
            }

            return taken;
        }

        private static bool IsKey(GameState state, string id, Func<string, bool> isKeyItem)
        {
            return state.KeyItems.Has(id) || (isKeyItem != null && isKeyItem(id));
        }
    }
}
