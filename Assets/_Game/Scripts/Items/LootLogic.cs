using System;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Text;

namespace SixtyOne.Items
{
    /// <summary>Result of moving one item from a container into the bag.</summary>
    public enum LootTakeResult
    {
        Taken,
        BagFull,
        NotThere
    }

    /// <summary>
    /// Pure loot-container rules (unit-tested): the first search costs building time and real search time, later
    /// visits are free and instant; items move between the container (GameState.Containers) and the bag; key items
    /// go to the backpack pockets; neighbours' items are recorded as borrowed ("ödünç al").
    /// </summary>
    public static class LootLogic
    {
        /// <summary>Flag set once the first search's building cost was paid (a cancelled search refunds nothing).</summary>
        public static string PaidFlag(string containerId) => "SearchPaid_" + containerId;

        /// <summary>KnowledgeSet key: this container was searched in some loop (reveals its hidden extras later).</summary>
        public static string KnowledgeKey(string containerId) => "container:" + containerId;

        public static bool IsSearched(GameState state, string containerId) => state.Containers.IsSearched(containerId);

        /// <summary>Building seconds a click costs now: the configured cost once, then nothing.</summary>
        public static float BuildingCost(GameState state, string containerId, float buildingCost)
        {
            return IsSearched(state, containerId) || state.Flags.Get(PaidFlag(containerId)) ? 0f : buildingCost;
        }

        /// <summary>Real seconds of rummaging before the window opens: none once searched.</summary>
        public static float SearchSeconds(GameState state, string containerId, float searchSeconds)
        {
            return IsSearched(state, containerId) ? 0f : searchSeconds;
        }

        /// <summary>Contents on the first search: the authored items, plus the hidden extras once the container is known from an earlier loop.</summary>
        public static List<string> InitialContents(IEnumerable<string> contents, IEnumerable<string> hiddenExtras, bool known)
        {
            var list = new List<string>();
            if (contents != null) list.AddRange(contents);
            if (known && hiddenExtras != null) list.AddRange(hiddenExtras);
            list.RemoveAll(string.IsNullOrEmpty);
            return list;
        }

        /// <summary>Marks the container searched (fills it) and remembers it in the KnowledgeSet.</summary>
        public static void CompleteSearch(GameState state, string containerId, IEnumerable<string> contents, IEnumerable<string> hiddenExtras)
        {
            if (IsSearched(state, containerId)) return;
            bool known = state.Knowledge.Contains(KnowledgeKey(containerId));
            state.Containers.Open(containerId, InitialContents(contents, hiddenExtras, known));
            state.Knowledge.Add(KnowledgeKey(containerId));
        }

        /// <summary>Moves one item into the bag (key items into the pockets). Bag full = "ui.bag.full" (flash + denied cue).</summary>
        public static LootTakeResult Take(GameState state, string containerId, string itemId, bool isKeyItem, bool borrowed)
        {
            if (!ContainsItem(state, containerId, itemId)) return LootTakeResult.NotThere;
            if (!isKeyItem && state.Inventory.IsFull)
            {
                GameEvents.RaiseMessage(UiKeys.BagFull);
                return LootTakeResult.BagFull;
            }

            state.Containers.Take(containerId, itemId);
            if (!StateOps.TryAddItem(state, itemId, isKeyItem))
            {
                state.Containers.Put(containerId, itemId);
                return LootTakeResult.BagFull;
            }

            if (borrowed) state.Borrowed.Add(itemId);
            return LootTakeResult.Taken;
        }

        /// <summary>Takes everything that fits (key items always). Returns how many items moved.</summary>
        public static int TakeAll(GameState state, string containerId, Func<string, bool> isKeyItem, bool borrowed)
        {
            int moved = 0;
            var items = new List<string>(state.Containers.ItemsIn(containerId));
            bool full = false;
            foreach (string id in items)
            {
                bool key = isKeyItem != null && isKeyItem(id);
                if (!key && state.Inventory.IsFull)
                {
                    full = true;
                    continue;
                }

                state.Containers.Take(containerId, id);
                if (StateOps.TryAddItem(state, id, key))
                {
                    if (borrowed) state.Borrowed.Add(id);
                    moved++;
                }
                else
                {
                    state.Containers.Put(containerId, id);
                }
            }

            if (full) GameEvents.RaiseMessage(UiKeys.BagFull);
            return moved;
        }

        /// <summary>Puts a bag item (back) into the open container; a borrowed item no longer owned leaves the borrow ledger.</summary>
        public static bool PutBack(GameState state, string containerId, string itemId)
        {
            if (!IsSearched(state, containerId) || !state.Inventory.Has(itemId)) return false;
            if (!StateOps.RemoveItem(state, itemId)) return false;
            state.Containers.Put(containerId, itemId);
            if (state.Borrowed.Contains(itemId) && !StateOps.HasItem(state, itemId)) state.Borrowed.Remove(itemId);
            return true;
        }

        private static bool ContainsItem(GameState state, string containerId, string itemId)
        {
            foreach (string id in state.Containers.ItemsIn(containerId))
            {
                if (id == itemId) return true;
            }

            return false;
        }
    }
}
