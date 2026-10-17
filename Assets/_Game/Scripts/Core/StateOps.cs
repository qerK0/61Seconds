using SixtyOne.Text;

namespace SixtyOne.Core
{
    /// <summary>State mutations that also raise the matching GameEvents.</summary>
    public static class StateOps
    {
        /// <summary>Sets a flag; raises FlagChanged if the value changed.</summary>
        public static void SetFlag(GameState state, string flag, bool value)
        {
            if (state.Flags.Set(flag, value))
            {
                GameEvents.RaiseFlagChanged(flag, value);
            }
        }

        /// <summary>True if the player owns the item: in the bag or as a key item.</summary>
        public static bool HasItem(GameState state, string itemId)
        {
            return state.Inventory.Has(itemId) || state.KeyItems.Has(itemId);
        }

        /// <summary>Adds an item to the bag. Raises ItemAdded, or a "bag full" message.</summary>
        public static bool TryAddItem(GameState state, string itemId)
        {
            return TryAddItem(state, itemId, false);
        }

        /// <summary>
        /// Adds an item. Key items (flashlight, whistle) go to KeyItems and never use a bag slot; a key item
        /// the player already owns is a no-op that still counts as success.
        /// </summary>
        public static bool TryAddItem(GameState state, string itemId, bool isKeyItem)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (isKeyItem)
            {
                if (state.KeyItems.Add(itemId))
                {
                    GameEvents.RaiseItemAdded(itemId);
                    InventoryEvents.RaiseKeyItemAcquired(itemId);
                    InventoryEvents.RaiseInventoryChanged();
                }

                return true;
            }

            if (state.Inventory.TryAdd(itemId))
            {
                GameEvents.RaiseItemAdded(itemId);
                InventoryEvents.RaiseInventoryChanged();
                return true;
            }

            if (state.Inventory.IsFull)
            {
                GameEvents.RaiseMessage(UiKeys.BagFull);
            }

            return false;
        }

        /// <summary>Removes an item (bag first, then key items); raises ItemRemoved if it was owned.</summary>
        public static bool RemoveItem(GameState state, string itemId)
        {
            if (!state.Inventory.Remove(itemId) && !state.KeyItems.Remove(itemId))
            {
                return false;
            }

            GameEvents.RaiseItemRemoved(itemId);
            InventoryEvents.RaiseInventoryChanged();
            return true;
        }

        /// <summary>
        /// Drops a bag item into a room: frees the slot and records it in the room so it can be picked up again.
        /// Key items cannot be dropped (they are not in the bag).
        /// </summary>
        public static bool DropItem(GameState state, string itemId, string roomId)
        {
            if (!state.Inventory.Remove(itemId)) return false;
            state.Dropped.Add(roomId, itemId);
            GameEvents.RaiseItemRemoved(itemId);
            InventoryEvents.RaiseItemDropped(itemId, roomId);
            InventoryEvents.RaiseInventoryChanged();
            return true;
        }

        /// <summary>Puts a dropped item from the room back into the bag. False if the bag is full or the item is gone.</summary>
        public static bool PickUpDropped(GameState state, string itemId, string roomId)
        {
            if (state.Inventory.IsFull)
            {
                GameEvents.RaiseMessage(UiKeys.BagFull);
                return false;
            }

            if (!state.Dropped.Take(roomId, itemId)) return false;
            state.Inventory.TryAdd(itemId);
            GameEvents.RaiseItemAdded(itemId);
            InventoryEvents.RaiseDroppedItemPicked(itemId, roomId);
            InventoryEvents.RaiseInventoryChanged();
            return true;
        }

        /// <summary>Sets a member's health; raises MemberStateChanged if it changed.</summary>
        public static void SetHealth(GameState state, MemberType member, MemberHealth health)
        {
            FamilyMemberState m = state.GetMember(member);
            if (m.Health == health)
            {
                return;
            }

            m.Health = health;
            GameEvents.RaiseMemberStateChanged(member, health);
        }
    }
}
