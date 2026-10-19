using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Furniture the player has searched (loot containers) and what is still inside, per container id. A container
    /// is unknown until its first search fills it from the authored contents; after that the items taken out or
    /// put back live here. Part of GameState (restored by snapshots, like RoomDropLedger).
    /// </summary>
    public sealed class ContainerLedger
    {
        private readonly Dictionary<string, List<string>> containers = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private static readonly List<string> Empty = new List<string>();

        /// <summary>True once the container has been searched (a second visit opens at once, free).</summary>
        public bool IsSearched(string containerId) => !string.IsNullOrEmpty(containerId) && containers.ContainsKey(containerId);

        /// <summary>First search: records the container with its authored contents. No-op if already searched.</summary>
        public void Open(string containerId, IEnumerable<string> contents)
        {
            if (string.IsNullOrEmpty(containerId) || containers.ContainsKey(containerId)) return;
            var list = new List<string>();
            if (contents != null)
            {
                foreach (string id in contents) if (!string.IsNullOrEmpty(id)) list.Add(id);
            }

            containers[containerId] = list;
        }

        /// <summary>Items still inside (in order), empty if unsearched.</summary>
        public IReadOnlyList<string> ItemsIn(string containerId)
        {
            return containerId != null && containers.TryGetValue(containerId, out List<string> list) ? list : Empty;
        }

        /// <summary>Removes one instance from the container. False if it is not there.</summary>
        public bool Take(string containerId, string itemId)
        {
            return containerId != null && containers.TryGetValue(containerId, out List<string> list) && list.Remove(itemId);
        }

        /// <summary>Puts an item (back) into a searched container.</summary>
        public void Put(string containerId, string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !containers.TryGetValue(containerId ?? string.Empty, out List<string> list)) return;
            list.Add(itemId);
        }

        public void CopyFrom(ContainerLedger other)
        {
            containers.Clear();
            foreach (KeyValuePair<string, List<string>> pair in other.containers) containers[pair.Key] = new List<string>(pair.Value);
        }

        public ContainerLedger Clone()
        {
            var copy = new ContainerLedger();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
