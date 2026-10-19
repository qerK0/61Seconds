using NUnit.Framework;
using SixtyOne.Audio;

namespace SixtyOne.Tests
{
    /// <summary>Music layer choice: calm prep, silence while shaking, tension after the quake, outro outside.</summary>
    public class MusicRulesTests
    {
        [Test]
        public void PicksTheTrackForTheStage()
        {
            Assert.AreEqual(MusicTrack.Prep, MusicRules.TrackFor(false, false, false));
            Assert.AreEqual(MusicTrack.None, MusicRules.TrackFor(false, true, false), "quake onset stops the prep music");
            Assert.AreEqual(MusicTrack.None, MusicRules.TrackFor(false, true, true), "aftershocks stop the music too");
            Assert.AreEqual(MusicTrack.Tension, MusicRules.TrackFor(false, false, true));
            Assert.AreEqual(MusicTrack.Outro, MusicRules.TrackFor(true, false, true));
        }
    }
}
