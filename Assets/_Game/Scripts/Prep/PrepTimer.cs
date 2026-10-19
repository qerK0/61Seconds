using System;

namespace SixtyOne.Prep
{
    /// <summary>
    /// The pre-quake prep countdown, pure C# (like BuildingTimer): the remaining value lives in GameState.PrepTime
    /// so snapshots restore it. Advance fires SecondTick(s) for every whole second s in [1, TickFrom] crossed
    /// (a big jump from an action cost ticks once) and Zero once.
    /// </summary>
    public sealed class PrepTimer
    {
        private bool zeroFired;

        public float Total { get; private set; } = 60f;
        public int TickFrom { get; private set; } = 10;

        public event Action<int> SecondTick;
        public event Action Zero;

        public void Configure(float total, int tickFrom)
        {
            Total = Math.Max(0f, total);
            TickFrom = Math.Max(0, tickFrom);
        }

        /// <summary>Re-arms the zero event for a remaining value without firing (after a rewind/restore).</summary>
        public void Sync(float remaining)
        {
            zeroFired = remaining <= 0f;
        }

        /// <summary>Elapsed fraction 0 (alert) .. 1 (quake).</summary>
        public float ElapsedFraction(float remaining)
        {
            return Total <= 0f ? 1f : 1f - Math.Max(0f, Math.Min(1f, remaining / Total));
        }

        /// <summary>Returns remaining - delta (min 0) and fires ticks/zero for what was crossed.</summary>
        public float Advance(float remaining, float delta)
        {
            if (delta <= 0f || zeroFired) return Math.Max(0f, remaining);

            float before = remaining;
            float after = Math.Max(0f, remaining - delta);

            int highest = Math.Min(TickFrom, (int)Math.Ceiling(before) - 1);
            int lowest = after <= 0f ? 1 : (int)Math.Ceiling(after);
            if (highest >= lowest && highest >= 1)
            {
                if (highest - lowest >= 2) SecondTick?.Invoke(lowest);
                else for (int s = highest; s >= lowest; s--) SecondTick?.Invoke(s);
            }

            if (after <= 0f)
            {
                zeroFired = true;
                Zero?.Invoke();
            }

            return after;
        }
    }
}
