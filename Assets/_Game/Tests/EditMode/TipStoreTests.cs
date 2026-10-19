using NUnit.Framework;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    /// <summary>The saved Deprem Notları string (PlayerPrefs) round-trips and ignores junk.</summary>
    public sealed class TipStoreTests
    {
        [Test]
        public void ParseIgnoresEmptyEntriesAndSpaces()
        {
            CollectionAssert.AreEquivalent(new[] { "tip.a", "tip.b" }, TipStore.Parse(" tip.a,, tip.b ,"));
            Assert.AreEqual(0, TipStore.Parse(string.Empty).Count);
            Assert.AreEqual(0, TipStore.Parse(null).Count);
        }

        [Test]
        public void SerializeIsSortedAndRoundTrips()
        {
            string saved = TipStore.Serialize(new[] { "tip.gas_off", "tip.drop_cover_hold" });
            Assert.AreEqual("tip.drop_cover_hold,tip.gas_off", saved);
            CollectionAssert.AreEquivalent(new[] { "tip.gas_off", "tip.drop_cover_hold" }, TipStore.Parse(saved));
        }
    }
}
