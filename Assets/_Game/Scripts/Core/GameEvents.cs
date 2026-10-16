using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Global C# events. Gameplay raises them; UI, audio and FX listen.</summary>
    public static class GameEvents
    {
        public static event Action<string> NodeEntered;
        /// <summary>(nodeId, choice label key).</summary>
        public static event Action<string, string> ChoiceMade;
        public static event Action<DeathInfo> Died;
        /// <summary>Raised after state was restored. Argument is the node id rewound to.</summary>
        public static event Action<string> Rewound;
        public static event Action<string> ItemAdded;
        public static event Action<string> ItemRemoved;
        public static event Action<string, bool> FlagChanged;
        public static event Action<MemberType, MemberHealth> MemberStateChanged;
        public static event Action<QuakeInfo> QuakeStarted;
        public static event Action<QuakeInfo> QuakeEnded;
        /// <summary>AFAD early-warning alert. Argument is the magnitude.</summary>
        public static event Action<float> AfadAlert;
        /// <summary>Short toast message. Argument is a text key.</summary>
        public static event Action<string> Message;

        public static void RaiseNodeEntered(string nodeId) => NodeEntered?.Invoke(nodeId);
        public static void RaiseChoiceMade(string nodeId, string labelKey) => ChoiceMade?.Invoke(nodeId, labelKey);
        public static void RaiseDied(DeathInfo info) => Died?.Invoke(info);
        public static void RaiseRewound(string nodeId) => Rewound?.Invoke(nodeId);
        public static void RaiseItemAdded(string itemId) => ItemAdded?.Invoke(itemId);
        public static void RaiseItemRemoved(string itemId) => ItemRemoved?.Invoke(itemId);
        public static void RaiseFlagChanged(string flag, bool value) => FlagChanged?.Invoke(flag, value);
        public static void RaiseMemberStateChanged(MemberType member, MemberHealth health) => MemberStateChanged?.Invoke(member, health);
        public static void RaiseQuakeStarted(QuakeInfo info) => QuakeStarted?.Invoke(info);
        public static void RaiseQuakeEnded(QuakeInfo info) => QuakeEnded?.Invoke(info);
        public static void RaiseAfadAlert(float magnitude) => AfadAlert?.Invoke(magnitude);
        public static void RaiseMessage(string textKey) => Message?.Invoke(textKey);

        /// <summary>Drops all listeners. Runs automatically before play (safe with domain reload off).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            NodeEntered = null;
            ChoiceMade = null;
            Died = null;
            Rewound = null;
            ItemAdded = null;
            ItemRemoved = null;
            FlagChanged = null;
            MemberStateChanged = null;
            QuakeStarted = null;
            QuakeEnded = null;
            AfadAlert = null;
            Message = null;
        }
    }
}
