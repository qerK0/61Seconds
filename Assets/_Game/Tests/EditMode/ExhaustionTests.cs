using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    /// <summary>Hunger/thirst exhaustion: rises only at 0 stamina, decays after eating, clamps, survives snapshots.</summary>
    public class ExhaustionTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
        }

        [Test]
        public void RisesOnlyAtZeroStamina()
        {
            Assert.AreEqual(0.3f, SurvivalModel.StepExhaustion(0f, 0f, 0.03f, 0.2f, 10f), 0.0001f);
            Assert.AreEqual(0f, SurvivalModel.StepExhaustion(0f, 0.5f, 0.03f, 0.2f, 10f), 0.0001f, "any stamina left: no rise");
            Assert.AreEqual(0f, SurvivalModel.StepExhaustion(0f, 40f, 0.03f, 0.2f, 10f), 0.0001f);
        }

        [Test]
        public void DecaysWhenStaminaIsAboveZero()
        {
            Assert.AreEqual(0.4f, SurvivalModel.StepExhaustion(0.6f, 35f, 0.03f, 0.2f, 1f), 0.0001f);
            Assert.Less(SurvivalModel.StepExhaustion(0.9f, 1f, 0.03f, 0.2f, 0.5f), 0.9f);
        }

        [Test]
        public void ClampsBetweenZeroAndOne()
        {
            Assert.AreEqual(1f, SurvivalModel.StepExhaustion(0.95f, 0f, 0.03f, 0.2f, 60f), 0.0001f);
            Assert.AreEqual(0f, SurvivalModel.StepExhaustion(0.1f, 50f, 0.03f, 0.2f, 60f), 0.0001f);
            Assert.AreEqual(0f, SurvivalModel.StepExhaustion(0f, 0f, -1f, 0.2f, 5f), 0.0001f, "negative gain is ignored");
        }

        [Test]
        public void SnapshotRestoresExhaustion()
        {
            var session = new GameSession();
            session.State.Exhaustion = 0.25f;
            session.TakeCheckpoint("f4.prep.salon");

            session.State.Exhaustion = 1f;
            session.Die(new DeathInfo { RewindTarget = RewindTarget.Checkpoint });
            session.Rewind();
            Assert.AreEqual(0.25f, session.State.Exhaustion, 0.0001f);
            Assert.AreEqual(0.25f, session.State.Clone().Exhaustion, 0.0001f, "Clone copies it");
        }
    }
}
