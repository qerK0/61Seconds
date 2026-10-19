namespace SixtyOne.Core
{
    /// <summary>Plain countdown used for node timers. Tick it from a MonoBehaviour.</summary>
    public sealed class CountdownTimer
    {
        public float Total { get; private set; }
        public float Remaining { get; private set; }
        public bool Running { get; private set; }

        /// <summary>Remaining time as 0..1 (1 = full).</summary>
        public float Normalized => Total > 0f ? Remaining / Total : 0f;

        public void Start(float seconds)
        {
            Total = seconds;
            Remaining = seconds;
            Running = seconds > 0f;
        }

        public void Stop()
        {
            Running = false;
        }

        /// <summary>Advances the timer. Returns true exactly once, on the tick it expires.</summary>
        public bool Tick(float deltaTime)
        {
            if (!Running)
            {
                return false;
            }

            Remaining -= deltaTime;
            if (Remaining > 0f)
            {
                return false;
            }

            Remaining = 0f;
            Running = false;
            return true;
        }
    }
}
