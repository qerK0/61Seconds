using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>The bag: a number of item id slots (LevelConfig.MaxInventorySlots, 8 by default). Key items are not stored here.</summary>
    public sealed class Inventory
    {
        public const int DefaultCapacity = 8;

        private readonly List<string> items = new List<string>();

        /// <summary>Maximum number of items.</summary>
        public int Capacity { get; private set; }

        /// <summary>Item ids in pickup order.</summary>
        public IReadOnlyList<string> Items => items;

        public int Count => items.Count;
        public bool IsFull => items.Count >= Capacity;

        public Inventory(int capacity = DefaultCapacity)
        {
            Capacity = capacity < 1 ? 1 : capacity;
        }

        /// <summary>True if the bag holds the item.</summary>
        public bool Has(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && items.Contains(itemId);
        }

        /// <summary>Adds the item if a slot is free.</summary>
        public bool TryAdd(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || IsFull)
            {
                return false;
            }

            items.Add(itemId);
            return true;
        }

        /// <summary>Removes one instance of the item. Returns false if absent.</summary>
        public bool Remove(string itemId)
        {
            return items.Remove(itemId);
        }

        /// <summary>Changes the slot count (items beyond the new size are kept until removed).</summary>
        public void Resize(int capacity)
        {
            Capacity = capacity < 1 ? 1 : capacity;
        }

        /// <summary>How many instances of the item the bag holds (stackable items like bedsheets take one slot each).</summary>
        public int CountOf(string itemId)
        {
            int count = 0;
            foreach (string id in items) if (id == itemId) count++;
            return count;
        }

        public void Clear()
        {
            items.Clear();
        }

        /// <summary>Replaces contents with a copy of another inventory.</summary>
        public void CopyFrom(Inventory other)
        {
            items.Clear();
            items.AddRange(other.items);
        }

        /// <summary>Returns an independent copy.</summary>
        public Inventory Clone()
        {
            var copy = new Inventory(Capacity);
            copy.CopyFrom(this);
            return copy;
        }
    }
}
