using NUnit.Framework;
using SixtyOne.Core;

namespace SixtyOne.Tests
{
    /// <summary>
    /// Regression: after the first death the death card and info cards kept coming back, because a snapshot saved
    /// close to a death (no stamina, poison, a nearly empty timer) killed again right after every rewind.
    /// </summary>
    public class RepeatingInfoTests
    {
        private const float StaminaMax = 100f;
        private const float MinSeconds = 60f;

        [Test]
        public void ThreeDeathsInARowShowThreeDeathCardsAndAtMostOneInfoCardPerMacroRewind()
        {
            var session = new GameSession();
            session.State.BuildingTime = 20f;
            session.State.Stamina = 0f;
            session.State.Exhaustion = 0.95f;
            session.TakeCheckpoint("f2.aftershock");
            session.State.Poison = 0.9f;
            session.TakeSnapshot("f2.flat7");

            int deathCards = 0;
            int infoCards = 0;
            var gate = new RewindCardGate();
            void OnDied(DeathInfo info) => deathCards++;
            GameEvents.Died += OnDied;
            try
            {
                foreach (DeathInfo death in new[]
                {
                    new DeathInfo { TipId = "tip.gas_poison", RewindTarget = RewindTarget.LastNode },
                    new DeathInfo { TipId = "tip.no_window", RewindTarget = RewindTarget.LastNode },
                    new DeathInfo { TipId = "tip.collapse", RewindTarget = RewindTarget.Checkpoint }
                })
                {
                    Assert.IsTrue(session.Die(death));
                    Assert.IsFalse(session.Die(death), "a second death while dead is ignored");
                    session.Rewind();
                    RewindRecovery.Apply(session.State, session.LastRewindWasMacro, StaminaMax, MinSeconds);
                    if (session.LastRewindWasMacro && gate.Allow(session.Checkpoint.NodeId, death.TipId)) infoCards++;

                    // Nothing in the restored state may kill again on its own.
                    Assert.Less(session.State.Poison, 1f);
                    Assert.Less(session.State.Exhaustion, 1f);
                    Assert.GreaterOrEqual(session.State.Stamina, StaminaMax * RewindRecovery.StaminaFloor);
                    Assert.GreaterOrEqual(session.State.BuildingTime, MinSeconds);
                }
            }
            finally
            {
                GameEvents.Died -= OnDied;
            }

            Assert.AreEqual(3, deathCards);
            Assert.LessOrEqual(infoCards, 1);
        }

        [Test]
        public void TheSameMacroDeathNeverShowsItsInfoCardTwice()
        {
            var gate = new RewindCardGate();
            Assert.IsTrue(gate.Allow("f3.corridor", "tip.collapse"));
            Assert.IsFalse(gate.Allow("f3.corridor", "tip.collapse"));
            Assert.IsTrue(gate.Allow("f3.corridor", "tip.electric"));
        }

        [Test]
        public void MicroRewindKeepsSomePoisonButFarFromDeath()
        {
            var state = new GameState { Poison = 0.95f, Exhaustion = 0.2f, Stamina = 80f, BuildingTime = 200f };
            RewindRecovery.Apply(state, false, StaminaMax, MinSeconds);
            Assert.AreEqual(RewindRecovery.MicroCap, state.Poison, 1e-4f);
            Assert.AreEqual(0.2f, state.Exhaustion, 1e-4f);
            Assert.AreEqual(80f, state.Stamina, 1e-4f);
            Assert.AreEqual(200f, state.BuildingTime, 1e-4f);
        }
    }
}
