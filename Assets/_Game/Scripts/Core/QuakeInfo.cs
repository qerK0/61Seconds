namespace SixtyOne.Core
{
    /// <summary>Data passed with QuakeStarted/QuakeEnded.</summary>
    public readonly struct QuakeInfo
    {
        public readonly int Act;
        public readonly float Magnitude;
        public readonly float Duration;

        public QuakeInfo(int act, float duration)
        {
            Act = act;
            Magnitude = MagnitudeForAct(act);
            Duration = duration;
        }

        /// <summary>6.0 on floor 6 (act 1), 7.0 on floor 4 (act 2), 8.0 on floor 2 (act 3).</summary>
        public static float MagnitudeForAct(int act)
        {
            switch (act)
            {
                case 1: return 6f;
                case 2: return 7f;
                default: return 8f;
            }
        }
    }
}
