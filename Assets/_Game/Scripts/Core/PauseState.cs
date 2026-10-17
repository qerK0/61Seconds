using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// The ESC pause. The ONE place in the game that touches Time.timeScale: paused = timeScale 0 (every
    /// deltaTime-driven timer stops: building timer, prep, stamina, poison, exhaustion, node timers),
    /// AudioListener.pause = true (UI clicks use ignoreListenerPause) and world input blocked through InputGate.
    /// Unscaled-time systems (QTE) check IsPaused themselves.
    /// </summary>
    public static class PauseState
    {
        private static float resumeScale = 1f;

        public static bool IsPaused { get; private set; }

        /// <summary>Raised with the new paused value.</summary>
        public static event Action<bool> Changed;

        public static void Set(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            if (paused)
            {
                resumeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                InputGate.Block();
            }
            else
            {
                Time.timeScale = resumeScale;
                AudioListener.pause = false;
                InputGate.Unblock();
            }

            Changed?.Invoke(paused);
        }

        /// <summary>Leaves the pause without events (scene change / quit).</summary>
        public static void ForceClear()
        {
            if (IsPaused)
            {
                Time.timeScale = resumeScale;
                AudioListener.pause = false;
                InputGate.Unblock();
            }

            IsPaused = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsPaused = false;
            resumeScale = 1f;
            Changed = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
