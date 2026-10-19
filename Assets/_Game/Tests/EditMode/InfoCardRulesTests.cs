using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    public sealed class InfoCardRulesTests
    {
        [Test]
        public void StagesAndTransitions()
        {
            Assert.AreEqual(1, InfoCardRules.StageFor(1, "flat11_salon"));
            Assert.AreEqual(3, InfoCardRules.StageFor(3, "flat7"));
            Assert.AreEqual(InfoCardRules.StreetStage, InfoCardRules.StageFor(3, "street"));
            Assert.IsFalse(InfoCardRules.IsTransition(0, 1), "the first node of a run is no transition");
            Assert.IsTrue(InfoCardRules.IsTransition(1, 2));
            Assert.IsFalse(InfoCardRules.IsTransition(2, 2), "a rewind on the same floor");
            Assert.IsFalse(InfoCardRules.IsTransition(3, 2), "rewinds never go up a stage");
        }

        [Test]
        public void PreferredCards()
        {
            Assert.AreEqual(3, InfoCardRules.PreferredForStage(2));
            Assert.AreEqual(4, InfoCardRules.PreferredForStage(3));
            Assert.AreEqual(8, InfoCardRules.PreferredForStage(4));
            Assert.AreEqual(4, InfoCardRules.PreferredForDeath("tip.gas_off"));
            Assert.AreEqual(5, InfoCardRules.PreferredForDeath("tip.electric"));
            Assert.AreEqual(2, InfoCardRules.PreferredForDeath("tip.wardrobe"));
            Assert.AreEqual(6, InfoCardRules.PreferredForDeath("tip.collapse"));
            Assert.AreEqual(0, InfoCardRules.PreferredForDeath(null));
        }

        [Test]
        public void PicksPreferredThenNextUnseenThenCycles()
        {
            var seen = new HashSet<int>();
            Assert.AreEqual(3, InfoCardRules.Pick(3, seen, 0));
            seen.Add(3);
            Assert.AreEqual(4, InfoCardRules.Pick(3, seen, 3), "next unseen after the preferred one");
            seen.UnionWith(new[] { 4, 5, 6, 7, 8 });
            Assert.AreEqual(1, InfoCardRules.Pick(3, seen, 8), "wraps around");
            Assert.AreEqual(1, InfoCardRules.Pick(0, seen, 8), "no preference: from card 1");
            seen.UnionWith(new[] { 1, 2 });
            Assert.AreEqual(3, InfoCardRules.Pick(3, seen, 2), "all seen: the preferred card again");
            Assert.AreEqual(3, InfoCardRules.Pick(0, seen, 2), "all seen, no preference: the one after the last shown");
            Assert.AreEqual(1, InfoCardRules.Pick(0, seen, 8));
        }

        [Test]
        public void StoreParsesAndSerializes()
        {
            HashSet<int> cards = InfoCardStore.Parse(" 3,1,x,9,0,3 ");
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, cards);
            Assert.AreEqual("1,3,8", InfoCardStore.Serialize(new[] { 8, 3, 1 }));
            Assert.AreEqual(0, InfoCardStore.Parse(string.Empty).Count);
        }
    }
}
