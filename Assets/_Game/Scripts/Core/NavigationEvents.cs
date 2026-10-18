using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// Room-to-room transition events (raised by RoomTransitionController). Input is locked between them, and the
    /// prep countdown and building timer are paused (TimersPaused / TimerDelta): the tile rise/drop animation never
    /// costs time; a door's configured cost is still paid once when the move is chosen.
    /// </summary>
    public static class NavigationEvents
    {
        /// <summary>(fromRoomId, toRoomId) a transition started; input is locked.</summary>
        public static event Action<string, string> OnTransitionStart;

        /// <summary>(roomId) the new room is shown and input is unlocked.</summary>
        public static event Action<string> OnTransitionComplete;

        /// <summary>True from OnTransitionStart until OnTransitionComplete, and while a full-screen card holds (ScreenHold).</summary>
        public static bool TimersPaused => transitioning || ScreenHold.Active;

        private static bool transitioning;

        /// <summary>Real-time delta the prep / building timers may advance by this frame (0 mid-transition).</summary>
        public static float TimerDelta(float deltaSeconds) => TimersPaused ? 0f : deltaSeconds;

        public static void RaiseTransitionStart(string fromRoom, string toRoom)
        {
            transitioning = true;
            OnTransitionStart?.Invoke(fromRoom, toRoom);
        }

        public static void RaiseTransitionComplete(string roomId)
        {
            transitioning = false;
            OnTransitionComplete?.Invoke(roomId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnTransitionStart = null;
            OnTransitionComplete = null;
            transitioning = false;
        }
    }
}
