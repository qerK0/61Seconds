using System;

namespace SixtyOne.Core
{
    /// <summary>Bitmask of the three quake acts (6.0 / 7.0 / 8.0).</summary>
    [Flags]
    public enum ActMask
    {
        None = 0,
        Act1 = 1,
        Act2 = 2,
        Act3 = 4,
        All = Act1 | Act2 | Act3
    }

    /// <summary>Helpers for <see cref="ActMask"/>.</summary>
    public static class ActMaskUtil
    {
        /// <summary>True if the mask contains the given act (1-3).</summary>
        public static bool Includes(ActMask mask, int act)
        {
            if (act < 1 || act > 3)
            {
                return false;
            }

            return (mask & FromAct(act)) != 0;
        }

        /// <summary>Converts an act number (1-3) to its mask bit.</summary>
        public static ActMask FromAct(int act)
        {
            return act < 1 || act > 3 ? ActMask.None : (ActMask)(1 << (act - 1));
        }
    }
}
