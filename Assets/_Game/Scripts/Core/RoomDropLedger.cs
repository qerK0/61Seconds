using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Items the player dropped, per room id, in drop order. Part of GameState (restored by snapshots), so a
    /// dropped item stays in its room and can be picked up again later.
    /// </summary>
    public sealed class RoomDropLedger
    {
        private readonly Dictionary<string, List<string>> rooms = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        private static readonly List<string> Empty = new List<string>();

        /// <summary>Total dropped items in all rooms.</summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (List<string> list in rooms.Values) count += list.Count;
                return count;
            }
        }

        /// <summary>Item ids lying in the room (oldest first).</summary>
        public IReadOnlyList<string> ItemsIn(string roomId)
        {
            return roomId != null && rooms.TryGetValue(roomId, out List<string> list) ? list : Empty;
        }

        public void Add(string roomId, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            string key = roomId ?? string.Empty;
            if (!rooms.TryGetValue(key, out List<string> list))
            {
                list = new List<string>();
                rooms[key] = list;
            }

            list.Add(itemId);
        }

        /// <summary>Removes one instance from the room. False if it is not there.</summary>
        public bool Take(string roomId, string itemId)
        {
            return rooms.TryGetValue(roomId ?? string.Empty, out List<string> list) && list.Remove(itemId);
        }

        public void CopyFrom(RoomDropLedger other)
        {
            rooms.Clear();
            foreach (KeyValuePair<string, List<string>> pair in other.rooms) rooms[pair.Key] = new List<string>(pair.Value);
        }

        public RoomDropLedger Clone()
        {
            var copy = new RoomDropLedger();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
