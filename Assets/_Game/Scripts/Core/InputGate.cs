using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Blocks world clicks and choice buttons while a timed action runs. Counted, so nested blocks are safe.</summary>
    public static class InputGate
    {
        private static int blockers;

        public static bool IsBlocked => blockers > 0;

        public static void Block()
        {
            blockers++;
        }

        public static void Unblock()
        {
            if (blockers > 0) blockers--;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            blockers = 0;
        }
    }
}
