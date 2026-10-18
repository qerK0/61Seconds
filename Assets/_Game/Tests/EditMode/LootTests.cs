using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    /// <summary>Loot containers: item moves, bag full, key items, rewinds, free second visit, borrowing.</summary>
    public class LootTests
    {
        private TestData data;

        [SetUp]
        public void SetUp() => data = new TestData();

        [TearDown]
        public void TearDown() => data.Cleanup();

        private static GameState Searched(string containerId, params string[] items)
        {
            var state = new GameState();
            LootLogic.CompleteSearch(state, containerId, items, null);
            return state;
        }

        [Test]
        public void ItemsMoveBetweenContainerAndBag()
        {
            GameState state = Searched("hall_toolbox", "wrench", "tape");

            Assert.AreEqual(LootTakeResult.Taken, LootLogic.Take(state, "hall_toolbox", "wrench", false, false));
            Assert.IsTrue(state.Inventory.Has("wrench"));
            CollectionAssert.AreEqual(new[] { "tape" }, state.Containers.ItemsIn("hall_toolbox"));

            Assert.IsTrue(LootLogic.PutBack(state, "hall_toolbox", "wrench"));
            Assert.IsFalse(state.Inventory.Has("wrench"));
            CollectionAssert.AreEquivalent(new[] { "tape", "wrench" }, state.Containers.ItemsIn("hall_toolbox"));

            Assert.AreEqual(LootTakeResult.NotThere, LootLogic.Take(state, "hall_toolbox", "food", false, false));
        }

        [Test]
        public void BagFullRefusesAndRaisesTheBagFullMessage()
        {
            GameState state = Searched("wardrobe", "bedsheet");
            for (int i = 0; i < state.Inventory.Capacity; i++) state.Inventory.TryAdd("food");
            string message = null;
            GameEvents.Message += key => message = key;

            Assert.AreEqual(LootTakeResult.BagFull, LootLogic.Take(state, "wardrobe", "bedsheet", false, false));
            Assert.AreEqual(UiKeys.BagFull, message, "the bag flashes and the denied cue plays on this message");
            CollectionAssert.AreEqual(new[] { "bedsheet" }, state.Containers.ItemsIn("wardrobe"), "nothing lost");
        }

        [Test]
        public void TakeAllTakesWhatFitsAndKeyItemsGoToPockets()
        {
            GameState state = Searched("closet", "flashlight", "water", "food");
            state.Inventory.Resize(1);

            int moved = LootLogic.TakeAll(state, "closet", id => id == "flashlight", true);

            Assert.AreEqual(2, moved, "the flashlight (pocket) and one bag item");
            Assert.IsTrue(state.KeyItems.Has("flashlight"));
            Assert.IsFalse(state.Inventory.Has("flashlight"), "key items never take a bag slot");
            Assert.AreEqual(1, state.Inventory.Count);
            Assert.AreEqual(1, state.Containers.ItemsIn("closet").Count, "the rest stays inside");
            Assert.IsTrue(state.Borrowed.Contains("flashlight"), "neighbours' things are borrowed");
        }

        [Test]
        public void KeyItemTakesNoSlotEvenWhenTheBagIsFull()
        {
            GameState state = Searched("closet", "whistle");
            for (int i = 0; i < state.Inventory.Capacity; i++) state.Inventory.TryAdd("food");
            Assert.AreEqual(LootTakeResult.Taken, LootLogic.Take(state, "closet", "whistle", true, false));
            Assert.IsTrue(state.KeyItems.Has("whistle"));
        }

        [Test]
        public void ContentsPersistAcrossARewindToTheSnapshot()
        {
            var session = new GameSession();
            LootLogic.CompleteSearch(session.State, "flat12_cabinet", new[] { "first_aid", "tape" }, null);
            session.TakeSnapshot("f4.flat12");

            LootLogic.Take(session.State, "flat12_cabinet", "first_aid", false, true);
            Assert.AreEqual(1, session.State.Containers.ItemsIn("flat12_cabinet").Count);

            session.Die(new DeathInfo());
            session.Rewind();

            CollectionAssert.AreEqual(new[] { "first_aid", "tape" }, session.State.Containers.ItemsIn("flat12_cabinet"));
            Assert.IsFalse(session.State.Inventory.Has("first_aid"));
            Assert.IsFalse(session.State.Borrowed.Contains("first_aid"));
            Assert.IsTrue(LootLogic.IsSearched(session.State, "flat12_cabinet"), "searched before the snapshot: stays searched");
        }

        [Test]
        public void RewindBeforeTheSearchForgetsIt()
        {
            var session = new GameSession();
            session.TakeSnapshot("f4.prep.hall");
            LootLogic.CompleteSearch(session.State, "hall_toolbox", new[] { "wrench" }, null);
            session.Die(new DeathInfo());
            session.Rewind();
            Assert.IsFalse(LootLogic.IsSearched(session.State, "hall_toolbox"));
        }

        [Test]
        public void SecondVisitHasNoCost()
        {
            var state = new GameState();
            Assert.AreEqual(3f, LootLogic.BuildingCost(state, "hall_toolbox", 3f));
            Assert.AreEqual(1.5f, LootLogic.SearchSeconds(state, "hall_toolbox", 1.5f));

            LootLogic.CompleteSearch(state, "hall_toolbox", new[] { "wrench" }, null);

            Assert.AreEqual(0f, LootLogic.BuildingCost(state, "hall_toolbox", 3f));
            Assert.AreEqual(0f, LootLogic.SearchSeconds(state, "hall_toolbox", 1.5f), "the window opens at once");
        }

        [Test]
        public void CancelledSearchIsNotChargedTwice()
        {
            var state = new GameState();
            state.Flags.Set(LootLogic.PaidFlag("hall_toolbox"), true);
            Assert.AreEqual(0f, LootLogic.BuildingCost(state, "hall_toolbox", 3f), "paid once, refunded never");
            Assert.AreEqual(1.5f, LootLogic.SearchSeconds(state, "hall_toolbox", 1.5f), "but the rummaging starts over");
        }

        [Test]
        public void HiddenExtrasAppearOnceTheContainerIsKnown()
        {
            var first = new GameState();
            LootLogic.CompleteSearch(first, "bookshelf", new[] { "cloth" }, new[] { "food" });
            CollectionAssert.AreEqual(new[] { "cloth" }, first.Containers.ItemsIn("bookshelf"));

            var later = new GameState();
            later.Knowledge.Add(LootLogic.KnowledgeKey("bookshelf"));
            LootLogic.CompleteSearch(later, "bookshelf", new[] { "cloth" }, new[] { "food" });
            CollectionAssert.AreEqual(new[] { "cloth", "food" }, later.Containers.ItemsIn("bookshelf"));
        }
    }
}
