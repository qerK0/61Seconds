namespace SixtyOne.Audio
{
    /// <summary>Which music layer plays.</summary>
    public enum MusicTrack
    {
        None = 0,
        Prep = 1,
        Tension = 2,
        Outro = 3
    }

    /// <summary>Pure music choice (unit-tested): calm prep, silence while shaking, tension after a quake, outro outside.</summary>
    public static class MusicRules
    {
        public static MusicTrack TrackFor(bool epilogue, bool quaking, bool buildingTimerStarted)
        {
            if (epilogue) return MusicTrack.Outro;
            if (quaking) return MusicTrack.None;
            return buildingTimerStarted ? MusicTrack.Tension : MusicTrack.Prep;
        }
    }
}
