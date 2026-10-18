using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Prep;

namespace SixtyOne.Tests
{
    /// <summary>Floor 4 pre-quake prep: countdown thresholds, cost routing, the quake hand-over and checkpoint restores.</summary>
    public sealed class PrepTests
    {
        [Test]
        public void AfadAlertOnlyAtThePrepStart()
        {
            var state = new GameState();
            PrepRules.ResetForPrep(state, 60f, 300f);
            Assert.IsTrue(PrepRules.ShowsStartAlert(state, 60f), "prep start");
            state.PrepTime = 41f;
            Assert.IsFalse(PrepRules.ShowsStartAlert(state, 60f), "back in the salon later in the prep");
            PrepRules.StartBuildingTimer(state);
            state.PrepTime = 60f;
            Assert.IsFalse(PrepRules.ShowsStartAlert(state, 60f), "never after the quake");
        }

        private static PrepTimer Timer(List<int> ticks, int[] zeros, float total = 60f, int tickFrom = 10)
        {
            var timer = new PrepTimer();
            timer.Configure(total, tickFrom);
            timer.Sync(total);
            timer.SecondTick += s => ticks.Add(s);
            timer.Zero += () => zeros[0]++;
            return timer;
        }

        [Test]
        public void TicksOnlyInTheLastTenSecondsAndZeroFiresOnce()
        {
            var ticks = new List<int>();
            var zeros = new int[1];
            PrepTimer timer = Timer(ticks, zeros);
            float remaining = 60f;
            for (int i = 0; i < 70; i++) remaining = timer.Advance(remaining, 1f);

            CollectionAssert.AreEqual(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, ticks);
            Assert.AreEqual(1, zeros[0]);
            Assert.AreEqual(0f, remaining);
        }

        [Test]
        public void ActionCostJumpTicksOnceAndNoTicksAboveTheWindow()
        {
            var ticks = new List<int>();
            var zeros = new int[1];
            PrepTimer timer = Timer(ticks, zeros);

            float remaining = timer.Advance(60f, 5f);  // valve: 60 -> 55, outside the window
            Assert.IsEmpty(ticks);
            remaining = timer.Advance(remaining, 42f); // 55 -> 13
            Assert.IsEmpty(ticks);
            remaining = timer.Advance(remaining, 10f); // 13 -> 3: one tick, not eight
            Assert.AreEqual(1, ticks.Count);
            Assert.AreEqual(3, ticks[0]);
            Assert.AreEqual(3f, remaining, 0.0001f);
            Assert.AreEqual(0, zeros[0]);
        }

        [Test]
        public void SyncAfterRestoreReArmsZero()
        {
            var ticks = new List<int>();
            var zeros = new int[1];
            PrepTimer timer = Timer(ticks, zeros);
            timer.Advance(1f, 2f);
            Assert.AreEqual(1, zeros[0]);

            timer.Sync(60f); // macro rewind to the prep start
            timer.Advance(1f, 2f);
            Assert.AreEqual(2, zeros[0]);
        }

        [Test]
        public void CostsGoToPrepUntilTheQuakeThenToTheBuildingTimer()
        {
            var state = new GameState();
            PrepRules.ResetForPrep(state, 60f, 300f);
            Assert.IsTrue(PrepRules.SpendsPrep(state));
            Assert.IsFalse(state.BuildingTimerStarted);

            PrepRules.StartBuildingTimer(state);
            Assert.IsFalse(PrepRules.SpendsPrep(state));
            Assert.IsTrue(state.BuildingTimerStarted);
            Assert.AreEqual(0f, state.PrepTime);
            Assert.AreEqual(300f, state.BuildingTime, "the building timer starts full at the quake");
        }

        [Test]
        public void NoPrepConfiguredMeansTheBuildingTimerRunsFromTheStart()
        {
            var state = new GameState();
            PrepRules.ResetForPrep(state, 0f, 300f);
            Assert.IsTrue(state.BuildingTimerStarted);
            Assert.IsFalse(PrepRules.SpendsPrep(state));
        }

        [Test]
        public void BuildingTimerDoesNotMoveDuringPrep()
        {
            var state = new GameState();
            PrepRules.ResetForPrep(state, 60f, 300f);
            var building = new BuildingTimer();
            building.Configure(300f, 60f, 30f);
            building.Sync(state.BuildingTime);

            // SurvivalController only advances the building clock once SpendsPrep is false.
            for (int i = 0; i < 59; i++)
            {
                if (!PrepRules.SpendsPrep(state)) state.BuildingTime = building.Advance(state.BuildingTime, 1f);
            }

            Assert.AreEqual(300f, state.BuildingTime);
            PrepRules.StartBuildingTimer(state);
            if (!PrepRules.SpendsPrep(state)) state.BuildingTime = building.Advance(state.BuildingTime, 1f);
            Assert.AreEqual(299f, state.BuildingTime, 0.0001f);
        }

        [Test]
        public void Floor4CheckpointRestoresBothTimersAndMicroRewindKeepsThePlacementTimer()
        {
            var session = new GameSession();
            PrepRules.ResetForPrep(session.State, 60f, 300f);
            session.TakeCheckpoint("f4.prep.salon");

            // Loot: prep spent, a bag item, then the quake.
            session.State.PrepTime = 12f;
            session.State.Inventory.TryAdd("wrench");
            PrepRules.StartBuildingTimer(session.State);
            session.State.BuildingTime = 300f;
            session.TakeSnapshot("f4.quake");

            // Placement death: micro rewind back into the placement, building timer saved at that point.
            session.State.BuildingTime = 291f;
            session.Die(new DeathInfo { RewindTarget = RewindTarget.LastNode });
            Assert.AreEqual("f4.quake", session.Rewind());
            Assert.IsTrue(session.State.BuildingTimerStarted);
            Assert.AreEqual(300f, session.State.BuildingTime, 0.0001f);
            Assert.IsTrue(session.State.Inventory.Has("wrench"));

            // Later macro death on floor 4: back to the prep start with an empty bag and both timers reset.
            session.State.BuildingTime = 150f;
            session.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint });
            Assert.AreEqual("f4.prep.salon", session.Rewind());
            Assert.IsFalse(session.State.BuildingTimerStarted);
            Assert.AreEqual(60f, session.State.PrepTime, 0.0001f);
            Assert.AreEqual(300f, session.State.BuildingTime, 0.0001f);
            Assert.AreEqual(0, session.State.Inventory.Count);
        }
    }
}
