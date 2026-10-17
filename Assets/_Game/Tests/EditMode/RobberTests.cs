using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Items;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    /// <summary>The scripted robbery on the F2 stairs: what he takes and that every branch reaches the street.</summary>
    public class RobberTests
    {
        private TestData data;
        private Item food;
        private Item water;

        [SetUp]
        public void SetUp()
        {
            data = new TestData();
            food = data.Item("food");
            water = data.Item("water");
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        private List<string> Rob(GameState state)
        {
            OutcomeResult result = OutcomeExecutor.Execute(RobberSetup.TakeOutcomes(food, water), state);
            return result.Taken ?? new List<string>();
        }

        [Test]
        public void TakesAllTheFood()
        {
            var state = new GameState();
            state.Inventory.TryAdd("food");
            state.Inventory.TryAdd("water");
            state.Inventory.TryAdd("food");
            state.Inventory.TryAdd("tape");

            List<string> taken = Rob(state);

            CollectionAssert.AreEqual(new[] { "food", "food" }, taken);
            Assert.IsFalse(state.Inventory.Has("food"), "all food is gone");
            Assert.IsTrue(state.Inventory.Has("water"), "water stays when there was food");
            Assert.IsTrue(state.Inventory.Has("tape"));
            Assert.IsTrue(state.Flags.Get(FlagNames.RobbedAtExit));
        }

        [Test]
        public void WithoutFoodHeTakesOneWater()
        {
            var state = new GameState();
            state.Inventory.TryAdd("water");
            state.Inventory.TryAdd("water");

            List<string> taken = Rob(state);

            CollectionAssert.AreEqual(new[] { "water" }, taken);
            Assert.AreEqual(1, state.Inventory.CountOf("water"));
        }

        [Test]
        public void EmptyBagGivesTheBitterLine()
        {
            var state = new GameState();
            state.Inventory.TryAdd("rope");
            List<string> taken = Rob(state);
            Assert.AreEqual(0, taken.Count);
            Assert.IsTrue(state.Inventory.Has("rope"));

            RobberNode reply = data.Make<RobberNode>();
            reply.ConfigureRobbery(RobberSetup.TakeOutcomes(food, water), RobberSetup.SpeakerKey, "f2.robber.taken", "f2.robber.empty", RobberSetup.TakenItemKey,
                RobberSetup.ActorName, UnityEngine.Vector2.zero, null);
            Assert.AreEqual("f2.robber.empty", reply.ClosingLine(taken.Count).Key);
            Assert.AreEqual("f2.robber.taken", reply.ClosingLine(1).Key);
        }

        [Test]
        public void KeyItemsAreNeverTaken()
        {
            var state = new GameState();
            StateOps.TryAddItem(state, "flashlight", true);
            StateOps.TryAddItem(state, "whistle", true);

            List<string> taken = RobberLogic.Take(state, new HashSet<string> { "food", "whistle" }, "flashlight", 1, true, id => id == "flashlight" || id == "whistle");

            Assert.AreEqual(0, taken.Count);
            Assert.IsTrue(state.KeyItems.Has("flashlight"));
            Assert.IsTrue(state.KeyItems.Has("whistle"));
        }

        [Test]
        public void BorrowLedgerIsUntouched()
        {
            var state = new GameState();
            state.Inventory.TryAdd("food");
            state.Borrowed.Add("food");
            Rob(state);
            Assert.IsTrue(state.Borrowed.Contains("food"), "the epilogue still knows what we borrowed");
        }

        [Test]
        public void EveryBranchLeadsToTheStreet()
        {
            DialogueNode robber = data.Make<DialogueNode>();
            robber.Configure(RobberSetup.NodeId, "t", "b", "f2_stairs", 3);
            var replies = new List<RobberNode>();
            foreach (string reply in RobberSetup.Replies)
            {
                RobberNode node = data.Make<RobberNode>();
                node.Configure(RobberSetup.ReplyId(reply), "t", "b", "f2_stairs", 3);
                replies.Add(node);
            }

            SceneNode street = data.Node("f2.street", 3);
            RobberSetup.Wire(robber, replies, street, food, water, UnityEngine.Vector2.zero);

            Assert.AreEqual(3, robber.Choices.Count, "Yalvar / Kısa kes / Ona bağır");
            var seen = new HashSet<RobberNode>();
            foreach (Choice choice in robber.Choices)
            {
                Assert.AreEqual(ChoiceType.Normal, choice.Type, "no trap, no death, no fight");
                OutcomeResult result = OutcomeExecutor.Execute(choice.Outcomes, new GameState());
                var reply = result.NextNode as RobberNode;
                Assert.IsNotNull(reply, choice.LabelKey.Key);
                Assert.IsTrue(seen.Add(reply), "each choice has its own reply");
                Assert.AreSame(street, reply.NextNode, choice.LabelKey.Key + " must end on the street");

                bool takes = false;
                foreach (Outcome o in reply.TakeOutcomes) takes |= o.Type == OutcomeType.TakeItems;
                Assert.IsTrue(takes, "every branch takes the food");
            }
        }
    }
}
