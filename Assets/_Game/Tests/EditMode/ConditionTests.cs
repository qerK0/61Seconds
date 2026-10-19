using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    public class ConditionTests
    {
        private TestData data;
        private GameState state;
        private Item wrench;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            state = new GameState();
            wrench = data.Item("wrench");
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void HasAndLacksItem()
        {
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.HasItem(wrench), state));
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.LacksItem(wrench), state));
            state.Inventory.TryAdd("wrench");
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.HasItem(wrench), state));
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.LacksItem(wrench), state));
        }

        [Test]
        public void FlagTrueAndFalse()
        {
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.FlagFalse(FlagNames.GasOff), state), "unset flags read false");
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.FlagTrue(FlagNames.GasOff), state));
            state.Flags.Set(FlagNames.GasOff, true);
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.FlagTrue(FlagNames.GasOff), state));
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.FlagFalse(FlagNames.GasOff), state));
        }

        [Test]
        public void MemberState()
        {
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.MemberIs(MemberType.Wife, MemberHealth.Healthy), state));
            state.GetMember(MemberType.Wife).Health = MemberHealth.Injured;
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.MemberIs(MemberType.Wife, MemberHealth.Healthy), state));
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.MemberIs(MemberType.Wife, MemberHealth.Injured), state));
        }

        [Test]
        public void AllMetHandlesEmptyAndNull()
        {
            Assert.IsTrue(ConditionEvaluator.AllMet(null, state));
            Assert.IsTrue(ConditionEvaluator.AllMet(new Condition[0], state));
            Assert.IsFalse(ConditionEvaluator.AllMet(new[] { Condition.FlagFalse("A"), Condition.HasItem(wrench) }, state));
        }

        [Test]
        public void ChoiceViewHidesLocksAndRespectsShowIf()
        {
            SceneNode node = data.Node("n");
            node.AddChoice(new Choice("hidden").Require(Condition.HasItem(wrench)));
            node.AddChoice(new Choice("locked").Require(Condition.HasItem(wrench)).WhenLocked(FailBehaviour.ShowLocked, "why"));
            node.AddChoice(new Choice("gone").ShowWhen(Condition.FlagFalse(FlagNames.GasOff)));
            node.AddChoice(new Choice("timeout", ChoiceType.Trap).AsTimeoutOnly());
            node.AddChoice(new Choice("open"));

            var views = ChoiceView.Build(node, state);
            Assert.AreEqual(3, views.Count);
            Assert.AreEqual(1, views[0].Index);
            Assert.IsTrue(views[0].Locked);
            Assert.AreEqual(2, views[1].Index);
            Assert.AreEqual(4, views[2].Index);

            state.Inventory.TryAdd("wrench");
            state.Flags.Set(FlagNames.GasOff, true);
            views = ChoiceView.Build(node, state);
            Assert.AreEqual(3, views.Count, "hidden + locked now open, 'gone' hidden by showIf");
            Assert.IsFalse(views[1].Locked);
        }

        [Test]
        public void TimeoutChoiceFallsBackToFirstTrap()
        {
            SceneNode node = data.Node("n");
            node.AddChoice(new Choice("ok"));
            int trap = node.AddChoice(new Choice("trap", ChoiceType.Trap));
            Assert.AreSame(node.Choices[trap], node.GetTimeoutChoice());

            int special = node.AddChoice(new Choice("special", ChoiceType.Trap).AsTimeoutOnly());
            node.SetTimeoutChoice(special);
            Assert.AreSame(node.Choices[special], node.GetTimeoutChoice());
        }
    }
}
