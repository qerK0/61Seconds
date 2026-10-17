using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Item ids "ödünç alındı" (borrowed) from a neighbour's open flat. Part of GameState, so it is
    /// restored by snapshots and reset by a checkpoint rewind exactly like the bag.
    /// </summary>
    public sealed class BorrowLedger
    {
        private readonly HashSet<string> items = new HashSet<string>(StringComparer.Ordinal);

        public int Count => items.Count;
        public IEnumerable<string> Items => items;

        public bool Add(string itemId) => !string.IsNullOrEmpty(itemId) && items.Add(itemId);
        public bool Remove(string itemId) => !string.IsNullOrEmpty(itemId) && items.Remove(itemId);
        public bool Contains(string itemId) => !string.IsNullOrEmpty(itemId) && items.Contains(itemId);

        public void CopyFrom(BorrowLedger other)
        {
            items.Clear();
            foreach (string id in other.items) items.Add(id);
        }

        public BorrowLedger Clone()
        {
            var copy = new BorrowLedger();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
