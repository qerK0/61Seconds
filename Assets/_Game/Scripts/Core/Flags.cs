using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>String-to-bool story flags (GasOff, PowerOff, ...). Unknown flags read as false.</summary>
    public sealed class Flags
    {
        private readonly Dictionary<string, bool> values = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary>All flags that were ever set.</summary>
        public IReadOnlyDictionary<string, bool> All => values;

        /// <summary>Returns the flag value, false if never set.</summary>
        public bool Get(string flag)
        {
            return !string.IsNullOrEmpty(flag) && values.TryGetValue(flag, out bool value) && value;
        }

        /// <summary>Sets a flag. Returns true when the value actually changed.</summary>
        public bool Set(string flag, bool value)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return false;
            }

            bool old = Get(flag);
            values[flag] = value;
            return old != value;
        }

        /// <summary>Replaces all values with a copy of another set.</summary>
        public void CopyFrom(Flags other)
        {
            values.Clear();
            foreach (KeyValuePair<string, bool> pair in other.values)
            {
                values[pair.Key] = pair.Value;
            }
        }

        /// <summary>Returns an independent copy.</summary>
        public Flags Clone()
        {
            var copy = new Flags();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
