using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    public class BuildingTimerTests
    {
        private BuildingTimer timer;
        private int pulses;
        private int alarms;
        private int zeros;
        private List<int> ticks;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            timer = new BuildingTimer();
            timer.Configure(300f, 60f, 30f);
            timer.Sync(300f);
            pulses = alarms = zeros = 0;
            ticks = new List<int>();
            timer.Pulse += () => pulses++;
            timer.Alarm += () => alarms++;
            timer.Zero += () => zeros++;
            timer.AlarmTick += s => ticks.Add(s);
        }

        [Test]
        public void ThresholdsFireOnceInOrder()
        {
            float t = 300f;
            for (int i = 0; i < 2400; i++) t = timer.Advance(t, 0.125f);
            Assert.AreEqual(0f, t, 0.0001f);
            Assert.AreEqual(1, pulses);
            Assert.AreEqual(1, alarms);
            Assert.AreEqual(1, zeros);
            Assert.AreEqual(30, ticks.Count, "one tick per second of the 30s alarm window");
            Assert.AreEqual(30, ticks[0]);
            Assert.AreEqual(1, ticks[29]);
        }

        [Test]
        public void ActionCostsSubtractAndCrossThresholds()
        {
            float t = timer.Advance(65f, 10f); // e.g. forcing a door
            Assert.AreEqual(55f, t, 0.0001f);
            Assert.AreEqual(1, pulses);
            Assert.AreEqual(0, alarms);

            t = timer.Advance(t, 40f); // big jump into the alarm window ticks once, not 15 times
            Assert.AreEqual(15f, t, 0.0001f);
            Assert.AreEqual(1, alarms);
            Assert.AreEqual(1, ticks.Count);
        }

        [Test]
        public void ZeroFiresOnceAndStops()
        {
            float t = timer.Advance(3f, 10f);
            Assert.AreEqual(0f, t);
            Assert.AreEqual(1, zeros);
            timer.Advance(t, 1f);
            Assert.AreEqual(1, zeros);
        }

        [Test]
        public void SyncReArmsWithoutFiring()
        {
            timer.Sync(20f);
            timer.Advance(20f, 0.5f);
            Assert.AreEqual(0, pulses, "already past the pulse threshold");
            Assert.AreEqual(0, alarms);
            timer.Sync(200f);
            timer.Advance(200f, 150f);
            Assert.AreEqual(1, pulses, "re-armed after rewinding to a higher value");
        }

        [Test]
        public void ElapsedFraction()
        {
            Assert.AreEqual(0f, timer.ElapsedFraction(300f), 0.0001f);
            Assert.AreEqual(0.5f, timer.ElapsedFraction(150f), 0.0001f);
            Assert.AreEqual(1f, timer.ElapsedFraction(0f), 0.0001f);
        }

        [Test]
        public void CheckpointRestoresTheBuildingTimer()
        {
            var session = new GameSession();
            session.State.BuildingTime = 240f;
            session.TakeCheckpoint("f3.aftershock");

            session.State.BuildingTime = 90f;
            session.TakeSnapshot("f3.flat9");
            session.State.BuildingTime = 70f;

            session.Die(new DeathInfo { RewindTarget = RewindTarget.LastNode });
            session.Rewind();
            Assert.AreEqual(90f, session.State.BuildingTime, 0.0001f, "micro rewind: timer of the last decision node");

            session.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint });
            session.Rewind();
            Assert.AreEqual(240f, session.State.BuildingTime, 0.0001f, "macro rewind: timer of the floor checkpoint");
        }

        [Test]
        public void SnapshotsRestoreStaminaPoisonAndBorrowLedger()
        {
            var session = new GameSession();
            session.State.Stamina = 80f;
            session.TakeCheckpoint("f4.intro");

            session.State.Stamina = 10f;
            session.State.Poison = 0.6f;
            session.State.Borrowed.Add("rope");
            session.State.Flags.Set(FlagNames.LeftNote, true);

            session.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint });
            session.Rewind();
            Assert.AreEqual(80f, session.State.Stamina, 0.0001f);
            Assert.AreEqual(0f, session.State.Poison, 0.0001f);
            Assert.AreEqual(0, session.State.Borrowed.Count, "borrow ledger resets on a checkpoint rewind, like the bag");
            Assert.IsFalse(session.State.Flags.Get(FlagNames.LeftNote));
        }
    }
}
