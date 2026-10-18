using NUnit.Framework;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    public class SnapshotTests
    {
        [SetUp]
        public void SetUp() => GameEvents.ClearAll();

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void CloneIsDeep()
        {
            var state = new GameState();
            state.Inventory.TryAdd("wrench");
            state.Flags.Set("GasOff", true);
            GameState copy = state.Clone();

            state.Inventory.TryAdd("gloves");
            state.Flags.Set("GasOff", false);
            state.GetMember(MemberType.Wife).Health = MemberHealth.Injured;

            Assert.AreEqual(1, copy.Inventory.Count);
            Assert.IsTrue(copy.Flags.Get("GasOff"));
            Assert.AreEqual(MemberHealth.Healthy, copy.GetMember(MemberType.Wife).Health);
        }

        [Test]
        public void RestoreRevertsStateButKeepsLoopTipsLabelsAndAnchors()
        {
            var state = new GameState();
            state.Inventory.TryAdd("wrench");
            Snapshot snapshot = Snapshot.Capture(state, "n1");

            state.Inventory.TryAdd("tape");
            state.Flags.Set("GasOff", true);
            state.GetMember(MemberType.Player).Health = MemberHealth.Dead;
            state.GetMember(MemberType.Wife).AnchorId = "lr_table";
            state.CollectedTips.Add("tip.no_elevator");
            state.RevealedAnchors.Add("lr_wardrobe");
            state.Loop.Increment();
            state.CurrentNodeId = "elsewhere";

            snapshot.RestoreInto(state);

            CollectionAssert.AreEqual(new[] { "wrench" }, state.Inventory.Items);
            Assert.IsFalse(state.Flags.Get("GasOff"));
            Assert.AreEqual(MemberHealth.Healthy, state.GetMember(MemberType.Player).Health);
            Assert.AreEqual("n1", state.CurrentNodeId);
            Assert.AreEqual(1, state.LoopCount, "loop count persists");
            Assert.IsTrue(state.CollectedTips.Contains("tip.no_elevator"), "tips persist");
            Assert.IsTrue(state.RevealedAnchors.Contains("lr_wardrobe"), "revealed labels persist");
            Assert.AreEqual("lr_table", state.GetMember(MemberType.Wife).AnchorId, "members keep their last chosen anchor");
        }

        [Test]
        public void SessionDieThenRewindIncrementsLoopAndCollectsTip()
        {
            var session = new GameSession();
            session.TakeSnapshot("n1");
            string died = null;
            string rewound = null;
            GameEvents.Died += info => died = info.TextKey;
            GameEvents.Rewound += id => rewound = id;

            Assert.IsTrue(session.Die(new DeathInfo { TextKey = "death.x", TipId = "tip.x", AnchorId = "a1" }));
            Assert.IsTrue(session.IsDead);
            Assert.AreEqual("death.x", died);
            Assert.AreEqual(MemberHealth.Dead, session.State.GetMember(MemberType.Player).Health);
            Assert.IsFalse(session.Die(new DeathInfo()), "second death while dead is ignored");

            Assert.AreEqual("n1", session.Rewind());
            Assert.AreEqual("n1", rewound);
            Assert.IsFalse(session.IsDead);
            Assert.AreEqual(1, session.State.LoopCount);
            Assert.IsTrue(session.State.CollectedTips.Contains("tip.x"));
            Assert.IsTrue(session.State.RevealedAnchors.Contains("a1"));
            Assert.AreEqual(MemberHealth.Healthy, session.State.GetMember(MemberType.Player).Health);
        }

        [Test]
        public void ChildIsTrappedNeverDead()
        {
            var session = new GameSession();
            session.TakeSnapshot("n1");
            session.Die(new DeathInfo { Victim = MemberType.Child });
            Assert.AreEqual(MemberHealth.Trapped, session.State.GetMember(MemberType.Child).Health);
        }

        [Test]
        public void RewindWithoutDeathDoesNothing()
        {
            var session = new GameSession();
            session.TakeSnapshot("n1");
            Assert.IsNull(session.Rewind());
            Assert.AreEqual(0, session.State.LoopCount);
        }
    }
}
