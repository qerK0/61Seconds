using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// Holds the first node of a new run (the AFAD alert and the prep) until the intro briefing is dismissed. The
    /// briefing closes it in Awake, NodePlayer waits in Start.
    /// </summary>
    public static class RunStartGate
    {
        public static bool Held { get; private set; }

        /// <summary>Raised once when the gate opens.</summary>
        public static event Action Opened;

        public static void Hold()
        {
            Held = true;
        }

        public static void Open()
        {
            if (!Held) return;
            Held = false;
            Action opened = Opened;
            Opened = null;
            opened?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            Held = false;
            Opened = null;
        }
    }
}
