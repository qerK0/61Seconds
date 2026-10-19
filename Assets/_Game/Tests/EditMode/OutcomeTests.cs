using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    public class OutcomeTests
    {
        private TestData data;
        private GameState state;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            state = new GameState();
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void SetFlagRaisesEventOnlyOnChange()
        {
            int raised = 0;
            GameEvents.FlagChanged += (f, v) => raised++;
            OutcomeExecutor.Execute(new[] { Outcome.SetFlag(FlagNames.GasOff) }, state);
            OutcomeExecutor.Execute(new[] { Outcome.SetFlag(FlagNames.GasOff) }, state);
            Assert.IsTrue(state.Flags.Get(FlagNames.GasOff));
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void AddAndRemoveItem()
        {
            Item wrench = data.Item("wrench");
            OutcomeExecutor.Execute(new[] { Outcome.AddItem(wrench) }, state);
            Assert.IsTrue(state.Inventory.Has("wrench"));
            OutcomeExecutor.Execute(new[] { Outcome.RemoveItem(wrench) }, state);
            Assert.IsFalse(state.Inventory.Has("wrench"));
        }

        [Test]
        public void AddItemWhenBagFullShowsMessage()
        {
            for (int i = 0; i < Inventory.DefaultCapacity; i++) state.Inventory.TryAdd("x" + i);
            string message = null;
            GameEvents.Message += key => message = key;
            OutcomeExecutor.Execute(new[] { Outcome.AddItem(data.Item("extra")) }, state);
            Assert.IsFalse(state.Inventory.Has("extra"));
            Assert.AreEqual(Inventory.DefaultCapacity, state.Inventory.Count);
            Assert.AreEqual(UiKeys.BagFull, message);
        }

        [Test]
        public void SetMemberState()
        {
            OutcomeExecutor.Execute(new[] { Outcome.SetMember(MemberType.Wife, MemberHealth.Injured) }, state);
            Assert.AreEqual(MemberHealth.Injured, state.GetMember(MemberType.Wife).Health);
        }

        [Test]
        public void GoToTakesFirstTargetAndDeathIsReported()
        {
            SceneNode a = data.Node("a");
            SceneNode b = data.Node("b");
            OutcomeResult result = OutcomeExecutor.Execute(new List<Outcome> { Outcome.GoTo(a), Outcome.GoTo(b) }, state);
            Assert.AreSame(a, result.NextNode);
            Assert.IsFalse(result.Died);

            result = OutcomeExecutor.Execute(new[] { Outcome.SetFlag("X"), Outcome.Death() }, state);
            Assert.IsTrue(result.Died);
            Assert.IsTrue(state.Flags.Get("X"), "state outcomes still apply before death");
        }

        [Test]
        public void NullListIsNoop()
        {
            OutcomeResult result = OutcomeExecutor.Execute(null, state);
            Assert.IsNull(result.NextNode);
            Assert.IsFalse(result.Died);
        }
    }
}
