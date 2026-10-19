using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Placement;

namespace SixtyOne.Tests
{
    /// <summary>Members run to their anchors: reservation, late arrivals at the quake, rewinds snap.</summary>
    public class PlacementRunTests
    {
        private TestData data;

        [SetUp]
        public void SetUp() => data = new TestData();

        [TearDown]
        public void TearDown() => data.Cleanup();

        private static PlacementLayout Layout(Hazard trapHazard = null)
        {
            return new PlacementLayout()
                .Add(new AnchorInfo("table", AnchorTag.Safe, 2, null, new[] { "wall" }))
                .Add(new AnchorInfo("wall", AnchorTag.Safe, 1, null, new[] { "table" }))
                .Add(new AnchorInfo("window", AnchorTag.Trap, 1, trapHazard));
        }

        private static Dictionary<MemberType, string> Placements(GameState state, PlacementLayout layout)
        {
            var placements = new Dictionary<MemberType, string>();
            foreach (FamilyMemberState m in state.Members) placements[m.Type] = layout.Contains(m.AnchorId) ? m.AnchorId : null;
            return placements;
        }

        [Test]
        public void AnchorIsReservedImmediately()
        {
            var state = new GameState();
            var runs = new PlacementRuns();
            PlacementLayout layout = Layout();

            runs.Reserve(state, MemberType.Wife, "wall");

            Assert.AreEqual("wall", state.GetMember(MemberType.Wife).AnchorId, "written to GameState before the run ends");
            Assert.IsTrue(runs.IsRunning(MemberType.Wife));
            // The next member can be queued at once and sees the reservation (capacity 1).
            Assert.IsFalse(PlacementRules.CanPlace(layout, Placements(state, layout), MemberType.Child, "wall", out string reason));
            Assert.AreEqual(SixtyOne.Text.UiKeys.PlacementNoRoom, reason);
            Assert.IsTrue(PlacementRules.CanPlace(layout, Placements(state, layout), MemberType.Child, "table", out _), "adjacent to the running wife's anchor");
        }

        [Test]
        public void MemberStillRunningAtTimeoutResolvesAtTargetAnchor()
        {
            var state = new GameState();
            var runs = new PlacementRuns();
            Hazard window = data.Hazard("hz.window", HazardEffect.Death, HazardEffect.Injured, HazardEffect.Trapped);
            PlacementLayout layout = Layout(window);

            runs.Reserve(state, MemberType.Wife, "window");
            runs.Reserve(state, MemberType.Child, "table");
            runs.Arrived(MemberType.Child);
            runs.Reserve(state, MemberType.Player, "table");

            // Timer ends: the wife and player have not arrived yet.
            List<KeyValuePair<MemberType, string>> unfinished = runs.FinishAll();
            Assert.AreEqual(2, unfinished.Count);
            Assert.Contains(new KeyValuePair<MemberType, string>(MemberType.Wife, "window"), unfinished);
            Assert.AreEqual(0, runs.Count);

            List<PlacementResult> results = PlacementResolver.Resolve(layout, Placements(state, layout), 1);
            PlacementResult wife = results.Find(r => r.Member == MemberType.Wife);
            Assert.AreEqual("window", wife.AnchorId, "resolves at the target, never at the old spot");
            Assert.AreEqual(HazardEffect.Injured, wife.Effect);
        }

        [Test]
        public void ReplacingARunningMemberMovesTheReservation()
        {
            var state = new GameState();
            var runs = new PlacementRuns();
            runs.Reserve(state, MemberType.Wife, "window");
            runs.Reserve(state, MemberType.Wife, "table");
            Assert.AreEqual("table", state.GetMember(MemberType.Wife).AnchorId);
            Assert.AreEqual("table", runs.TargetOf(MemberType.Wife));
        }

        [Test]
        public void RewindsSnapEverythingElseRuns()
        {
            Assert.AreEqual(MoveMode.Snap, MemberMoveRules.ArrivalMode(rewind: true, reduceEffects: false));
            Assert.AreEqual(MoveMode.Snap, MemberMoveRules.ArrivalMode(rewind: true, reduceEffects: true));
            Assert.AreEqual(MoveMode.Run, MemberMoveRules.ArrivalMode(rewind: false, reduceEffects: false));
            Assert.AreEqual(MoveMode.Run, MemberMoveRules.ArrivalMode(rewind: false, reduceEffects: true), "Reduce Effects keeps running");
        }

        [Test]
        public void MicroRewindKeepsTheReservedAnchors()
        {
            var session = new GameSession();
            var runs = new PlacementRuns();
            session.TakeSnapshot("quake");
            runs.Reserve(session.State, MemberType.Wife, "table");
            session.Die(new DeathInfo { Victim = MemberType.Player, RewindTarget = RewindTarget.LastNode });
            session.Rewind();
            Assert.AreEqual("table", session.State.GetMember(MemberType.Wife).AnchorId, "micro rewind snaps members to their last chosen anchors");
        }
    }
}
