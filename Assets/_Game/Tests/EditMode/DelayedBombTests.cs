using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    /// <summary>Gas valve choice on floor 6 -> bomb check on Act 2 entry -> rewind to the exploration node.</summary>
    public class DelayedBombTests
    {
        private TestData data;
        private SceneNode explore;
        private SceneNode act2Entry;
        private Choice closeGas;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            explore = data.Node("act1.n03_explore");
            act2Entry = data.Node("act2.n01_entry", 2);

            closeGas = new Choice("act1.n03.c_gas").Then(Outcome.SetFlag(FlagNames.GasOff));
            explore.AddChoice(closeGas);
            explore.AddChoice(new Choice("act1.n03.c_leave").Then(Outcome.GoTo(act2Entry)));

            act2Entry.AddBombCheck(new BombCheck()
                .When(Condition.FlagFalse(FlagNames.GasOff))
                .Kills(new DeathData("death.kitchen_fire", data.Tip("tip.gas_off"), MemberType.Player, explore)));
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void BombGoesOffWhenGasLeftOpen()
        {
            var state = new GameState();
            BombCheck bomb = BombCheckRunner.Run(act2Entry.BombChecks, state);
            Assert.IsNotNull(bomb);
            DeathInfo info = bomb.Death.ToInfo();
            Assert.AreEqual("death.kitchen_fire", info.TextKey);
            Assert.AreEqual("tip.gas_off", info.TipId);
            Assert.AreEqual("act1.n03_explore", info.RewindNodeId);
        }

        [Test]
        public void BombIsDefusedByEarlierChoice()
        {
            var state = new GameState();
            OutcomeExecutor.Execute(closeGas.Outcomes, state);
            Assert.IsNull(BombCheckRunner.Run(act2Entry.BombChecks, state));
        }

        [Test]
        public void RewindGoesBackToTheArmingNodeNotTheLatest()
        {
            var session = new GameSession();
            session.TakeSnapshot(explore.Id);
            session.State.Inventory.TryAdd("gold");
            session.TakeSnapshot("act1.n04_stairs");

            BombCheck bomb = BombCheckRunner.Run(act2Entry.BombChecks, session.State);
            Assert.IsNotNull(bomb);
            session.Die(bomb.Death.ToInfo());

            Assert.AreEqual(explore.Id, session.Rewind());
            Assert.AreEqual(explore.Id, session.State.CurrentNodeId);
            Assert.AreEqual(0, session.State.Inventory.Count, "state is the exploration snapshot");
            Assert.AreEqual(1, session.State.LoopCount);
            Assert.IsTrue(session.State.CollectedTips.Contains("tip.gas_off"));

            // After the rewind the player closes the gas and the bomb no longer fires.
            OutcomeExecutor.Execute(closeGas.Outcomes, session.State);
            Assert.IsNull(BombCheckRunner.Run(act2Entry.BombChecks, session.State));
        }
    }
}
