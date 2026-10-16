using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// Routes world and choice clicks through the panic input delay. DelayProvider returns seconds (0 when calm);
    /// Scheduler shows instant click feedback and runs the action after the delay. Without a scheduler the action
    /// runs immediately.
    /// </summary>
    public static class InputDelayRouter
    {
        /// <summary>Current delay in seconds (set by the survival driver).</summary>
        public static Func<float> DelayProvider { get; set; }

        /// <summary>(action, screen position, delay) — set by the click feedback UI.</summary>
        public static Action<Action, Vector2, float> Scheduler { get; set; }

        public static float CurrentDelay => DelayProvider != null ? Mathf.Max(0f, DelayProvider()) : 0f;

        public static void Run(Action action, Vector2 screenPosition)
        {
            if (action == null) return;
            if (Scheduler != null) Scheduler(action, screenPosition, CurrentDelay);
            else action();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            DelayProvider = null;
            Scheduler = null;
        }
    }
}
