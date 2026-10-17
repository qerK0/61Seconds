using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>What the loot window shows: one searched container.</summary>
    public sealed class LootView
    {
        /// <summary>Container id (ContainerLedger key).</summary>
        public string ContainerId;

        /// <summary>Container name text key (container.wardrobe ...).</summary>
        public string NameKey;

        /// <summary>A neighbour's furniture: the title says "Ödünç al" and every item taken is recorded as borrowed.</summary>
        public bool Borrowed;
    }

    /// <summary>
    /// Loot container events (search ring, loot window). World containers raise them; UI (LootWindow, the search
    /// ring, BagUI) listens, so world and UI never reference each other.
    /// </summary>
    public static class LootEvents
    {
        /// <summary>(world position, 0..1) a search is in progress.</summary>
        public static event Action<Vector3, float> OnSearchProgress;

        /// <summary>The search ended (finished or cancelled).</summary>
        public static event Action OnSearchEnded;

        /// <summary>A container's window opened.</summary>
        public static event Action<LootView> OnOpened;

        /// <summary>The window closed.</summary>
        public static event Action OnClosed;

        /// <summary>The container whose window is open, or null. BagUI puts clicked items back into it.</summary>
        public static LootView Open { get; private set; }

        public static bool IsOpen => Open != null;

        public static void RaiseSearchProgress(Vector3 world, float progress) => OnSearchProgress?.Invoke(world, progress);
        public static void RaiseSearchEnded() => OnSearchEnded?.Invoke();

        public static void RaiseOpened(LootView view)
        {
            Open = view;
            OnOpened?.Invoke(view);
        }

        public static void RaiseClosed()
        {
            if (Open == null) return;
            Open = null;
            OnClosed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            OnSearchProgress = null;
            OnSearchEnded = null;
            OnOpened = null;
            OnClosed = null;
            Open = null;
        }
    }
}
