using NUnit.Framework;
using SixtyOne.World;

namespace SixtyOne.Tests
{
    /// <summary>Hidden-item sparkle rule (the GameSettings toggle is checked before this rule).</summary>
    public class HiddenItemTests
    {
        [Test]
        public void SparkleOnlyWhenKnownOrSearchedLongEnough()
        {
            Assert.IsFalse(HiddenItemHotspot.SparkleRule(false, 10f, 25f));
            Assert.IsTrue(HiddenItemHotspot.SparkleRule(false, 25f, 25f), "after N seconds in the room");
            Assert.IsTrue(HiddenItemHotspot.SparkleRule(true, 0f, 25f), "found in an earlier loop");
            Assert.IsFalse(HiddenItemHotspot.SparkleRule(false, 999f, 0f), "0 disables the time rule");
        }
    }
}
