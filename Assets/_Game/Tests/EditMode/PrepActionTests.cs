using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Prep;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Timed actions cost countdown time: the countdown keeps running while the action runs.</summary>
    public class PrepActionTests
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
        public void ActionCompletesAfterItsCostWhileCountdownRuns()
        {
            var countdown = new PrepCountdown();
            countdown.Start(61f, 10f);
            var action = new PrepAction();
            action.Begin(5f); // close gas valve

            // 0.125 is exact in binary, so 40 steps are exactly 5 seconds.
            int completed = 0;
            for (int i = 0; i < 40; i++)
            {
                countdown.Tick(0.125f);
                if (action.Tick(0.125f)) completed++;
            }

            Assert.AreEqual(1, completed);
            Assert.IsFalse(action.IsActive);
            Assert.AreEqual(56f, countdown.Remaining, 0.01f, "5 seconds of countdown spent");
        }

        [Test]
        public void ProgressAndCancel()
        {
            var action = new PrepAction();
            action.Begin(4f);
            action.Tick(1f);
            Assert.AreEqual(0.25f, action.Progress, 0.001f);
            action.Cancel();
            Assert.IsFalse(action.IsActive);
            Assert.IsFalse(action.Tick(10f), "cancelled action never completes");
        }

        [Test]
        public void TimeCostsAddUp()
        {
            // Drawer 3s + surface 1s + breaker 4s + gas valve 5s = 13s of the 51s prep budget.
            var countdown = new PrepCountdown();
            countdown.Start(61f, 10f);
            foreach (float cost in new[] { 3f, 1f, 4f, 5f })
            {
                var action = new PrepAction();
                action.Begin(cost);
                // Each frame advances both; 0.125 is exact so costs are hit precisely.
                bool done = false;
                while (!done)
                {
                    countdown.Tick(0.125f);
                    done = action.Tick(0.125f);
                }
            }

            Assert.AreEqual(48f, countdown.Remaining, 0.01f);
        }

        [Test]
        public void InteractableApplyAddsItemFlagsAndUsedFlag()
        {
            Item wrench = data.Item("wrench");
            var go = new GameObject("valve");
            try
            {
                PrepInteractable interactable = go.AddComponent<PrepInteractable>();
                interactable.Configure("drawer", "prep.wrench", 3f, wrench, null, default, isOneShot: true, hide: false,
                    extraOutcomes: new[] { Outcome.SetFlag(FlagNames.PowerOff) }, driver: null, mainSprite: null);
                Assert.AreEqual(3f, interactable.TimeCost);

                var state = new GameState();
                Assert.IsTrue(interactable.Apply(state));
                Assert.IsTrue(state.Inventory.Has("wrench"));
                Assert.IsTrue(state.Flags.Get(FlagNames.PowerOff));
                Assert.IsTrue(state.Flags.Get(interactable.UsedFlag), "one-shot use is stored as a flag so rewinds restore it");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InteractableApplyFailsWhenBagFull()
        {
            Item water = data.Item("water");
            var go = new GameObject("water");
            try
            {
                PrepInteractable interactable = go.AddComponent<PrepInteractable>();
                interactable.Configure("water", "prep.water", 1f, water, null, default, true, true, null, null, null);
                var state = new GameState();
                for (int i = 0; i < Inventory.DefaultCapacity; i++) state.Inventory.TryAdd("junk" + i);

                Assert.IsFalse(interactable.Apply(state));
                Assert.IsFalse(state.Inventory.Has("water"));
                Assert.IsFalse(state.Flags.Get(interactable.UsedFlag), "not used up when nothing was taken");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
