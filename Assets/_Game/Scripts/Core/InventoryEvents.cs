using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// Bag-specific events (raised by StateOps). UI (BagUI, KeyItemCornerHUD) and world (DroppedItemsPresenter)
    /// listen here, so they never reference each other.
    /// </summary>
    public static class InventoryEvents
    {
        /// <summary>(itemId, roomId) an item was dropped from the bag into a room.</summary>
        public static event Action<string, string> OnItemDropped;

        /// <summary>(itemId, roomId) a dropped item was picked up again.</summary>
        public static event Action<string, string> OnDroppedItemPicked;

        /// <summary>A key item (flashlight, whistle) was acquired.</summary>
        public static event Action<string> OnKeyItemAcquired;

        /// <summary>Bag, key items or dropped items changed in any way.</summary>
        public static event Action OnInventoryChanged;

        public static void RaiseItemDropped(string itemId, string roomId) => OnItemDropped?.Invoke(itemId, roomId);
        public static void RaiseDroppedItemPicked(string itemId, string roomId) => OnDroppedItemPicked?.Invoke(itemId, roomId);
        public static void RaiseKeyItemAcquired(string itemId) => OnKeyItemAcquired?.Invoke(itemId);
        public static void RaiseInventoryChanged() => OnInventoryChanged?.Invoke();

        /// <summary>Drops all listeners (also called by GameEvents.ClearAll).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnItemDropped = null;
            OnDroppedItemPicked = null;
            OnKeyItemAcquired = null;
            OnInventoryChanged = null;
        }
    }
}
