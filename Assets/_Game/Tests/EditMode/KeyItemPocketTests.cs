using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Backpack side pockets (flashlight / whistle) and the 8-slot bag default.</summary>
    public class KeyItemPocketTests
    {
        private TestData data;
        private GameState state;
        private Item whistle;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            state = new GameState();
            whistle = data.Item("whistle");
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void LevelConfigDefaultsToEightSlots()
        {
            var config = data.Make<LevelConfig>();
            Assert.AreEqual(8, config.MaxInventorySlots);
            state.Inventory.Resize(config.MaxInventorySlots);
            Assert.AreEqual(8, state.Inventory.Capacity);
        }

        [Test]
        public void MissingItemOnlyShowsItsName()
        {
            SceneNode node = data.Node("dog");
            node.AddChoice(new Choice("whistle").Require(Condition.HasItem(whistle)).WhenLocked(FailBehaviour.ShowLocked, "why"));
            Assert.AreEqual(PocketAction.ShowName, KeyItemPocketRules.Decide(node, state, "whistle", out int index));
            Assert.AreEqual(-1, index);
        }

        [Test]
        public void OwnedWhistlePlaysTheNodeChoiceThatNeedsIt()
        {
            SceneNode node = data.Node("dog");
            node.AddChoice(new Choice("feed"));
            int whistleChoice = node.AddChoice(new Choice("whistle").Require(Condition.HasItem(whistle)));
            StateOps.TryAddItem(state, "whistle", true);

            Assert.AreEqual(PocketAction.UseChoice, KeyItemPocketRules.Decide(node, state, "whistle", out int index));
            Assert.AreEqual(whistleChoice, index);
        }

        [Test]
        public void WhistleWithoutAUseShowsItsName()
        {
            SceneNode node = data.Node("corridor");
            node.AddChoice(new Choice("walk"));
            StateOps.TryAddItem(state, "whistle", true);
            Assert.AreEqual(PocketAction.ShowName, KeyItemPocketRules.Decide(node, state, "whistle", out _));
            Assert.AreEqual(PocketAction.ShowName, KeyItemPocketRules.Decide(null, state, "whistle", out _));
        }

        [Test]
        public void FlashlightTogglesWhenNoChoiceNeedsIt()
        {
            StateOps.TryAddItem(state, "flashlight", true);
            Assert.IsTrue(KeyItemPocketRules.FlashlightOn(state), "on by default");
            Assert.AreEqual(PocketAction.ToggleFlashlight, KeyItemPocketRules.Decide(data.Node("flat10"), state, "flashlight", out _));

            StateOps.SetFlag(state, FlagNames.FlashlightOff, true);
            Assert.IsFalse(KeyItemPocketRules.FlashlightOn(state));
            Assert.IsTrue(StateOps.HasItem(state, "flashlight"), "switching it off keeps the item");
            Assert.IsFalse(StateOps.DropItem(state, "flashlight", "room"), "pocket items cannot be dropped");
        }
    }
}
