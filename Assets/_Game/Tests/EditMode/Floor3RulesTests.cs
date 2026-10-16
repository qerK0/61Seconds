using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Puzzles;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    /// <summary>Needs-aware trader, fuse puzzle, child-harness lock, missing-item deaths.</summary>
    public class Floor3RulesTests
    {
        private TestData data;
        private Item flashlight, gloves, tape, food, water, harness, grip;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            flashlight = data.Item("flashlight");
            gloves = data.Item("gloves");
            tape = data.Item("tape");
            food = data.Item("food");
            water = data.Item("water");
            harness = data.Item("child_harness");
            grip = data.Item("insulated_grip");
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        private TraderProfile Trader()
        {
            TraderProfile profile = data.Make<TraderProfile>();
            profile.Configure(new[] { flashlight, gloves, tape }, new[] { food, water });
            return profile;
        }

        [Test]
        public void TraderGivesTheFirstMissingUpcomingItem()
        {
            var inv = new Inventory();
            Assert.AreSame(flashlight, TraderLogic.Pick(Trader(), inv));
            inv.TryAdd("flashlight");
            Assert.AreSame(gloves, TraderLogic.Pick(Trader(), inv), "skips what the player already has");
            inv.TryAdd("gloves");
            inv.TryAdd("tape");
            Assert.AreSame(food, TraderLogic.Pick(Trader(), inv), "nothing missing: food or water");
            inv.TryAdd("food");
            Assert.AreSame(water, TraderLogic.Pick(Trader(), inv));
        }

        [Test]
        public void TraderOutcomeAddsTheItem()
        {
            var state = new GameState();
            state.Inventory.TryAdd("flashlight");
            OutcomeExecutor.Execute(new[] { Outcome.GiveFromTrader(Trader()) }, state);
            Assert.IsTrue(state.Inventory.Has("gloves"));
        }

        [Test]
        public void FusePuzzleNeedsTheExactPattern()
        {
            bool[] solution = { true, false, true, true };
            var switches = new bool[4];
            Assert.IsFalse(FusePuzzle.Matches(switches, solution));
            FusePuzzle.Toggle(switches, 0);
            FusePuzzle.Toggle(switches, 2);
            FusePuzzle.Toggle(switches, 3);
            Assert.IsTrue(FusePuzzle.Matches(switches, solution));
            FusePuzzle.Toggle(switches, 1);
            Assert.IsFalse(FusePuzzle.Matches(switches, solution));
            FusePuzzle.Toggle(switches, 9);
            Assert.IsFalse(FusePuzzle.Matches(new bool[3], solution), "length must match");
        }

        [Test]
        public void CableDescentIsLockedWithoutChildHarness()
        {
            SceneNode flat9 = data.Node("f3.flat9");
            flat9.AddChoice(new Choice("f3.flat9.c_cables").Costs(12f, 25f, isHeavy: true, move: true)
                .Require(Condition.HasItem(harness))
                .WhenLocked(FailBehaviour.ShowLocked, "f3.flat9.c_cables.locked"));

            var state = new GameState();
            ChoiceView view = ChoiceView.Build(flat9, state)[0];
            Assert.IsTrue(view.Locked);
            Assert.IsFalse(view.Fatal, "locked with a reason, not a death");
            Assert.AreEqual("f3.flat9.c_cables.locked", view.Choice.LockedReasonKey.Key);

            state.Inventory.TryAdd("child_harness");
            Assert.IsFalse(ChoiceView.Build(flat9, state)[0].Locked);

            state.Stamina = 0f;
            ChoiceView exhausted = ChoiceView.Build(flat9, state)[0];
            Assert.IsTrue(exhausted.Locked, "heavy descent also needs stamina");
            Assert.AreEqual(UiKeys.StaminaExhausted, exhausted.ReasonOverride);
        }

        [Test]
        public void ElectrocutionNamesWhatIsReallyMissingAndRewindsMicro()
        {
            Choice repair = new Choice("f3.flat9.c_repair")
                .Require(Condition.HasAnyItem(gloves, grip))
                .Require(Condition.HasItem(tape))
                .WhenLocked(FailBehaviour.ShowLockedFatal, "locked")
                .WithMissingText(gloves, "death.electrocuted")
                .FatalRewind(RewindTarget.LastNode);

            var state = new GameState();
            state.Inventory.TryAdd("insulated_grip");
            DeathInfo info = repair.BuildMissingItemDeath(state);
            Assert.AreEqual("tape", info.MissingItemId, "grip satisfies the first requirement; tape is missing");
            Assert.AreEqual(RewindTarget.LastNode, info.RewindTarget);
            Assert.AreEqual("death.electrocuted", info.TextKey);

            Assert.AreEqual("gloves", repair.BuildMissingItemDeath(new GameState()).MissingItemId);
            Assert.AreEqual("gloves", repair.BuildMissingItemDeath().MissingItemId, "fallback without state");
        }
    }
}
