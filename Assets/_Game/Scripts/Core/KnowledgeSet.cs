using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Item ids the player has learned they need (a ShowLockedFatal choice killed them for lacking it).
    /// Persists across every rewind; prep highlights these items in later loops.
    /// </summary>
    public sealed class KnowledgeSet
    {
        private readonly HashSet<string> items = new HashSet<string>(StringComparer.Ordinal);

        public IEnumerable<string> Items => items;
        public int Count => items.Count;

        /// <summary>Adds an item id. Returns true if it was new.</summary>
        public bool Add(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && items.Add(itemId);
        }

        public bool Contains(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && items.Contains(itemId);
        }

        public KnowledgeSet Clone()
        {
            var copy = new KnowledgeSet();
            foreach (string id in items) copy.items.Add(id);
            return copy;
        }
    }
}
