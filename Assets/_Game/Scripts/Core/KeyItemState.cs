using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Key items the player owns (flashlight, whistle). They never take a bag slot and cannot be dropped.
    /// Part of GameState, so snapshots restore it like the bag. The corner HUD reads it.
    /// </summary>
    public sealed class KeyItemState
    {
        private readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);

        public int Count => owned.Count;
        public IEnumerable<string> Items => owned;

        public bool Has(string itemId) => !string.IsNullOrEmpty(itemId) && owned.Contains(itemId);
        public bool Add(string itemId) => !string.IsNullOrEmpty(itemId) && owned.Add(itemId);
        public bool Remove(string itemId) => !string.IsNullOrEmpty(itemId) && owned.Remove(itemId);

        public void CopyFrom(KeyItemState other)
        {
            owned.Clear();
            foreach (string id in other.owned) owned.Add(id);
        }

        public KeyItemState Clone()
        {
            var copy = new KeyItemState();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
