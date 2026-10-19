namespace SixtyOne.Core
{
    /// <summary>Data passed with QuakeStarted/QuakeEnded.</summary>
    public readonly struct QuakeInfo
    {
        public readonly int Act;
        public readonly float Magnitude;
        public readonly float Duration;

        /// <summary>Camera shake multiplier for this quake (1 = act default).</summary>
        public readonly float ShakeScale;

        /// <summary>Which floor's quake recording plays (1 = cue_quake_f4 ... 3 = cue_quake_f2).</summary>
        public readonly int SoundAct;

        /// <summary>Volume scale of the quake recording (the street quake is louder).</summary>
        public readonly float Loudness;

        /// <summary>magnitude &lt;= 0 uses the act default; shakeScale &lt;= 0 means 1; soundAct &lt;= 0 = act; loudness &lt;= 0 = 1.</summary>
        public QuakeInfo(int act, float duration, float magnitude = 0f, float shakeScale = 1f, int soundAct = 0, float loudness = 1f)
        {
            Act = act;
            Magnitude = magnitude > 0f ? magnitude : MagnitudeForAct(act);
            Duration = duration;
            ShakeScale = shakeScale > 0f ? shakeScale : 1f;
            SoundAct = soundAct > 0 ? soundAct : act;
            Loudness = loudness > 0f ? loudness : 1f;
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
