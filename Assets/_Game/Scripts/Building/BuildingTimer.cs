using System;

namespace SixtyOne.Building
{
    /// <summary>
    /// Time left until the building collapses. Pure C#: the remaining value lives in GameState (so snapshots
    /// restore it); Advance() moves it and fires threshold events once each: Pulse, Alarm, AlarmTick(s) for
    /// whole seconds inside the alarm window, Zero.
    /// </summary>
    public sealed class BuildingTimer
    {
        private bool pulseFired;
        private bool alarmFired;
        private bool zeroFired;

        public float Total { get; private set; } = 480f;
        public float PulseAt { get; private set; } = 60f;
        public float AlarmAt { get; private set; } = 30f;

        public event Action Pulse;
        public event Action Alarm;
        public event Action<int> AlarmTick;
        public event Action Zero;

        public void Configure(float total, float pulseAt, float alarmAt)
        {
            Total = Math.Max(1f, total);
            PulseAt = Math.Max(0f, pulseAt);
            AlarmAt = Math.Max(0f, alarmAt);
        }

        /// <summary>Re-arms thresholds for a remaining value without firing (after a rewind/restore).</summary>
        public void Sync(float remaining)
        {
            pulseFired = remaining <= PulseAt;
            alarmFired = remaining <= AlarmAt;
            zeroFired = remaining <= 0f;
        }

        /// <summary>Elapsed fraction 0 (start) .. 1 (collapse).</summary>
        public float ElapsedFraction(float remaining)
        {
            return 1f - Math.Max(0f, Math.Min(1f, remaining / Total));
        }

        /// <summary>Returns remaining - delta (min 0) and fires any thresholds crossed.</summary>
        public float Advance(float remaining, float delta)
        {
            if (delta <= 0f || zeroFired) return Math.Max(0f, remaining);

            float before = remaining;
            float after = Math.Max(0f, remaining - delta);

            if (!pulseFired && after <= PulseAt)
            {
                pulseFired = true;
                Pulse?.Invoke();
            }

            if (!alarmFired && after <= AlarmAt)
            {
                alarmFired = true;
                Alarm?.Invoke();
            }

            // Ticks for whole seconds crossed inside the alarm window; a big jump (action cost) ticks once.
            int highest = Math.Min((int)AlarmAt, (int)Math.Ceiling(before) - 1);
            int lowest = (int)Math.Ceiling(after);
            if (after <= 0f) lowest = 1;
            if (highest >= lowest && highest >= 1)
            {
                if (highest - lowest >= 2) AlarmTick?.Invoke(lowest);
                else for (int s = highest; s >= lowest; s--) AlarmTick?.Invoke(s);
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
