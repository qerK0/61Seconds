namespace SixtyOne.Prep
{
    /// <summary>A timed action in progress (open a drawer, close the gas valve...). Pure C#.</summary>
    public sealed class PrepAction
    {
        public float Duration { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>0..1 progress.</summary>
        public float Progress => Duration <= 0f ? 1f : (Elapsed >= Duration ? 1f : Elapsed / Duration);

        public void Begin(float seconds)
        {
            Duration = seconds < 0f ? 0f : seconds;
            Elapsed = 0f;
            IsActive = true;
        }

        /// <summary>Advances the action. Returns true exactly once, on the tick it completes.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return false;
            }

            Elapsed += deltaTime;
            if (Elapsed < Duration)
            {
                return false;
            }

            IsActive = false;
            return true;
        }

        public void Cancel()
        {
            IsActive = false;
        }
    }
}
