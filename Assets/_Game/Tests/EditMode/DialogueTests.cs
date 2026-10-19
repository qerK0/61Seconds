using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    public class DialogueTests
    {
        private TestData data;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void DuringAndAfterDialogueChoicesAreSplit()
        {
            DialogueNode node = data.Make<DialogueNode>();
            node.Configure("f4.sevim", "t", "b", "room", 1);
            node.SetLines(new[] { new DialogueLine("npc", "f4.sevim.l1", 4f), new DialogueLine("npc", "f4.sevim.l2", 3f) });
            node.AddChoice(new Choice("hurry").AsDuringDialogue());
            node.AddChoice(new Choice("thanks"));

            var state = new GameState();
            var during = ChoiceView.Build(node, state, DialogueFilter.DuringDialogue);
            var after = ChoiceView.Build(node, state, DialogueFilter.AfterDialogue);
            Assert.AreEqual(1, during.Count);
            Assert.AreEqual(0, during[0].Index);
            Assert.AreEqual(1, after.Count);
            Assert.AreEqual(1, after[0].Index);
            Assert.AreEqual(2, ChoiceView.Build(node, state).Count);
            Assert.AreEqual(4f, node.Lines[0].TimeCost);
        }

        [Test]
        public void ChoiceCostsAreData()
        {
            Choice move = new Choice("c").Move(3f);
            Assert.AreEqual(3f, move.TimeCost);
            Assert.IsTrue(move.IsMove);
            Choice heavy = new Choice("h").Costs(12f, 25f, isHeavy: true);
            Assert.AreEqual(25f, heavy.StaminaCost);
            Assert.IsTrue(heavy.Heavy);
        }
    }
}
