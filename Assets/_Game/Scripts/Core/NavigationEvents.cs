using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Room-to-room transition events (raised by RoomTransitionController). Input is locked between them.</summary>
    public static class NavigationEvents
    {
        /// <summary>(fromRoomId, toRoomId) a transition started; input is locked.</summary>
        public static event Action<string, string> OnTransitionStart;

        /// <summary>(roomId) the new room is shown and input is unlocked.</summary>
        public static event Action<string> OnTransitionComplete;

        public static void RaiseTransitionStart(string fromRoom, string toRoom) => OnTransitionStart?.Invoke(fromRoom, toRoom);
        public static void RaiseTransitionComplete(string roomId) => OnTransitionComplete?.Invoke(roomId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnTransitionStart = null;
            OnTransitionComplete = null;
        }
    }
}
