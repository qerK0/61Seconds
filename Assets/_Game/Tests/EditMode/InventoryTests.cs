using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.UI;
using SixtyOne.World;

namespace SixtyOne.Tests
{
    /// <summary>8-slot bag, dropping into rooms, key items (no slot, backpack pockets), bedsheet rope.</summary>
    public class InventoryTests
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
        public void BagHoldsEightItems()
        {
            var state = new GameState();
            Assert.AreEqual(8, Inventory.DefaultCapacity);
            Assert.AreEqual(8, state.Inventory.Capacity);
            for (int i = 0; i < 7; i++) Assert.IsTrue(StateOps.TryAddItem(state, "item" + i));
            Assert.IsFalse(state.Inventory.IsFull, "7/8 is not full");
            Assert.IsTrue(StateOps.TryAddItem(state, "item7"));
            Assert.IsTrue(state.Inventory.IsFull);
            Assert.IsFalse(StateOps.TryAddItem(state, "item8"), "ninth item does not fit");

            state.Inventory.Resize(4);
            Assert.AreEqual(4, state.Inventory.Capacity, "LevelConfig.MaxInventorySlots can change it");
        }

        [Test]
        public void DroppingFreesTheSlotAndLeavesTheItemInTheRoom()
        {
            var state = new GameState();
            StateOps.TryAddItem(state, "rope");
            StateOps.TryAddItem(state, "tape");
            string droppedId = null;
            string droppedRoom = null;
            int changes = 0;
            InventoryEvents.OnItemDropped += (id, room) => { droppedId = id; droppedRoom = room; };
            InventoryEvents.OnInventoryChanged += () => changes++;

            Assert.IsTrue(StateOps.DropItem(state, "rope", "flat11_salon"));
            Assert.AreEqual(1, state.Inventory.Count, "slot freed");
            Assert.IsFalse(state.Inventory.Has("rope"));
            CollectionAssert.AreEqual(new[] { "rope" }, state.Dropped.ItemsIn("flat11_salon"));
            Assert.AreEqual(0, state.Dropped.ItemsIn("flat11_kitchen").Count, "only in the room it was dropped in");
            Assert.AreEqual("rope", droppedId);
            Assert.AreEqual("flat11_salon", droppedRoom);
            Assert.AreEqual(1, changes);

            Assert.IsFalse(StateOps.PickUpDropped(state, "rope", "flat11_kitchen"), "not lying in another room");
            Assert.IsTrue(StateOps.PickUpDropped(state, "rope", "flat11_salon"), "recoverable later");
            Assert.IsTrue(state.Inventory.Has("rope"));
            Assert.AreEqual(0, state.Dropped.Count);
        }

        [Test]
        public void DroppedItemsComeBackWithTheSnapshot()
        {
            var state = new GameState();
            StateOps.TryAddItem(state, "rope");
            Snapshot before = Snapshot.Capture(state, "n");
            StateOps.DropItem(state, "rope", "room");
            before.RestoreInto(state);
            Assert.IsTrue(state.Inventory.Has("rope"));
            Assert.AreEqual(0, state.Dropped.Count);
        }

        [Test]
        public void KeyItemsUseNoSlotAndShowInTheCornerHud()
        {
            Item flashlight = data.Item("flashlight");
            Item whistle = data.Item("whistle");
            flashlight.ConfigureKeyItem();
            whistle.ConfigureKeyItem();
            var state = new GameState();
            string acquired = null;
            InventoryEvents.OnKeyItemAcquired += id => acquired = id;

            Assert.IsFalse(KeyItemCornerHUD.IsShownAsOwned(state, "flashlight"));
            Assert.IsTrue(StateOps.TryAddItem(state, flashlight.Id, flashlight.IsKeyItem));
            Assert.IsTrue(StateOps.TryAddItem(state, whistle.Id, whistle.IsKeyItem));

            Assert.AreEqual(0, state.Inventory.Count, "bag stays 0/8");
            Assert.AreEqual(2, state.KeyItems.Count);
            Assert.AreEqual("whistle", acquired);
            Assert.IsTrue(KeyItemCornerHUD.IsShownAsOwned(state, "flashlight"));
            Assert.IsTrue(KeyItemCornerHUD.IsShownAsOwned(state, "whistle"));
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.HasItem(flashlight), state), "HasItem sees key items");

            Assert.IsFalse(StateOps.DropItem(state, "flashlight", "room"), "key items cannot be dropped");
            Assert.IsTrue(state.KeyItems.Has("flashlight"));

            for (int i = 0; i < Inventory.DefaultCapacity; i++) StateOps.TryAddItem(state, "item" + i);
            Assert.IsTrue(state.Inventory.IsFull);
            Assert.IsTrue(StateOps.TryAddItem(state, "whistle", true), "a full bag never blocks a key item");
        }

        [Test]
        public void ThreeBedsheetsTieOneRope()
        {
            var state = new GameState();
            StateOps.TryAddItem(state, "bedsheet");
            StateOps.TryAddItem(state, "bedsheet");
            StateOps.TryAddItem(state, "water");
            Assert.IsFalse(Crafting.CanTieRope(state, "bedsheet", 3));
            Assert.IsFalse(Crafting.TieRope(state, "bedsheet", "bedsheet_rope", 3), "two sheets are not enough");
            Assert.AreEqual(2, state.Inventory.CountOf("bedsheet"));

            StateOps.TryAddItem(state, "bedsheet");
            Assert.IsTrue(Crafting.CanTieRope(state, "bedsheet", 3));
            Assert.IsTrue(Crafting.TieRope(state, "bedsheet", "bedsheet_rope", 3));
            Assert.AreEqual(0, state.Inventory.CountOf("bedsheet"));
            Assert.AreEqual(1, state.Inventory.CountOf("bedsheet_rope"));
            Assert.AreEqual(2, state.Inventory.Count, "three slots became one");
        }

        [Test]
        public void RopeDescentNeedsTheRopeAndStamina()
        {
            Item rope = data.Item("bedsheet_rope");
            var state = new GameState();
            Assert.IsFalse(RopeDescentInteractable.CanDescend(state, rope));
            StateOps.TryAddItem(state, "bedsheet_rope");
            Assert.IsTrue(RopeDescentInteractable.CanDescend(state, rope));
            state.Stamina = 0f;
            Assert.IsFalse(RopeDescentInteractable.CanDescend(state, rope), "heavy: locked at 0 stamina");
        }

        [Test]
        public void RopeDescentIsLockedUntilTheChildCanComeSafely()
        {
            Item rope = data.Item("bedsheet_rope");
            Item harness = data.Item("child_harness");
            var state = new GameState();
            StateOps.TryAddItem(state, "bedsheet_rope");
            Assert.IsFalse(RopeDescentInteractable.FamilyCanCome(state, harness));
            Assert.IsFalse(RopeDescentInteractable.CanDescend(state, rope, harness), "rope alone: the child cannot come");
            StateOps.TryAddItem(state, "child_harness");
            Assert.IsTrue(RopeDescentInteractable.CanDescend(state, rope, harness));
        }
    }
}
