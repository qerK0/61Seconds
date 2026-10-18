using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// A full-screen card (intro briefing, floor info card) is up: every countdown stops (NavigationEvents.TimerDelta
    /// returns 0, node timers do not tick) and world / choice input is blocked. Counted, so nested holds are safe.
    /// </summary>
    public static class ScreenHold
    {
        private static int holds;

        public static bool Active => holds > 0;

        public static void Begin()
        {
            holds++;
            InputGate.Block();
        }

        public static void End()
        {
            if (holds <= 0) return;
            holds--;
            InputGate.Unblock();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            holds = 0;
        }
    }
}
