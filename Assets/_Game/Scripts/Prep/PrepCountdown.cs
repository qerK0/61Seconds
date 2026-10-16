using System;

namespace SixtyOne.Prep
{
    /// <summary>
    /// The 61-second countdown, pure C# so it is unit-testable. Tick it with delta time.
    /// Fires PlacementWindowReached once when Remaining &lt;= PlacementWindow, SecondTick(s) for every
    /// whole second s in [1, TickFrom] that is crossed, and Zero once.
    /// </summary>
    public sealed class PrepCountdown
    {
        private bool windowFired;

        public float Total { get; private set; }
        public float Remaining { get; private set; }
        public float PlacementWindow { get; private set; }
        public int TickFrom { get; private set; } = 10;
        public bool Running { get; private set; }

        /// <summary>True once the countdown is inside the placement window.</summary>
        public bool InPlacementWindow => Remaining <= PlacementWindow;

        public event Action PlacementWindowReached;
        public event Action<int> SecondTick;
        public event Action Zero;

        /// <summary>Starts from total seconds. If total is already inside the window the window event is not fired.</summary>
        public void Start(float total, float placementWindow, int tickFrom = 10)
        {
            Total = Math.Max(0f, total);
            Remaining = Total;
            PlacementWindow = Math.Max(0f, placementWindow);
            TickFrom = tickFrom;
            windowFired = Remaining <= PlacementWindow;
            Running = Remaining > 0f;
        }

        /// <summary>Moves to a remaining time without firing events for the skipped range (used by rewinds).</summary>
        public void JumpTo(float remaining)
        {
            Remaining = Math.Max(0f, Math.Min(remaining, Total));
            windowFired = Remaining <= PlacementWindow;
            Running = Remaining > 0f;
        }

        public void Stop()
        {
            Running = false;
        }

        /// <summary>Advances the countdown and fires threshold events.</summary>
        public void Tick(float deltaTime)
        {
            if (!Running || deltaTime <= 0f)
            {
                return;
            }

            float before = Remaining;
            Remaining = Math.Max(0f, Remaining - deltaTime);

            if (!windowFired && Remaining <= PlacementWindow)
            {
                windowFired = true;
                PlacementWindowReached?.Invoke();
            }

            // Every whole second crossed in (Remaining, before), counting down.
            for (int s = Math.Min(TickFrom, (int)Math.Ceiling(before) - 1); s >= 1 && s >= Remaining; s--)
            {
                SecondTick?.Invoke(s);
            }

            if (Remaining <= 0f)
            {
                Running = false;
                Zero?.Invoke();
            }
        }
    }
}
