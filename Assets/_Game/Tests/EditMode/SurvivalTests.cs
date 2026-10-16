using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    /// <summary>Panic input delay, stamina, poison, item use (pure survival model).</summary>
    public class SurvivalTests
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
        public void InputDelayIsZeroAtOrBelowThreshold()
        {
            Assert.AreEqual(0f, SurvivalModel.InputDelay(0f, 0.7f, 0.3f, false));
            Assert.AreEqual(0f, SurvivalModel.InputDelay(0.5f, 0.7f, 0.3f, false));
            Assert.AreEqual(0f, SurvivalModel.InputDelay(0.7f, 0.7f, 0.3f, false), "exactly at the threshold: still instant");
        }

        [Test]
        public void InputDelayScalesAndIsCappedAtMax()
        {
            Assert.AreEqual(0.15f, SurvivalModel.InputDelay(0.85f, 0.7f, 0.3f, false), 0.0001f, "halfway between threshold and 1");
            Assert.AreEqual(0.3f, SurvivalModel.InputDelay(1f, 0.7f, 0.3f, false), 0.0001f);
            Assert.AreEqual(0.3f, SurvivalModel.InputDelay(5f, 0.7f, 0.3f, false), 0.0001f, "capped at maxDelay");
        }

        [Test]
        public void ReduceEffectsForcesZeroDelay()
        {
            Assert.AreEqual(0f, SurvivalModel.InputDelay(1f, 0.7f, 0.3f, true));
        }

        [Test]
        public void StaminaDrainsAndRestoresWithinBounds()
        {
            Assert.AreEqual(99.5f, SurvivalModel.DrainStamina(100f, 0.25f, 2f), 0.0001f);
            Assert.AreEqual(0f, SurvivalModel.DrainStamina(0.1f, 1f, 1f), 0.0001f, "never below 0");
            Assert.AreEqual(100f, SurvivalModel.RestoreStamina(90f, 35f, 100f), 0.0001f, "never above max");
        }

        [Test]
        public void ZeroStaminaLocksHeavyActionsAndSlowsEverything()
        {
            SceneNode node = data.Node("f3.flat9");
            node.AddChoice(new Choice("climb").Costs(12f, 25f, isHeavy: true));
            node.AddChoice(new Choice("walk").Move(2f));
            var state = new GameState { Stamina = 0f };

            var views = ChoiceView.Build(node, state);
            Assert.IsTrue(views[0].Locked, "heavy locked at 0 stamina");
            Assert.IsFalse(views[0].Fatal);
            Assert.AreEqual(UiKeys.StaminaExhausted, views[0].ReasonOverride);
            Assert.IsFalse(views[1].Locked, "non-heavy still allowed");

            Assert.AreEqual(1.5f, SurvivalModel.TimeMultiplier(0, false, 0.25f, true, 1.5f), 0.0001f);
            state.Stamina = 50f;
            Assert.IsFalse(ChoiceView.Build(node, state)[0].Locked);
        }

        [Test]
        public void InjuredMembersSlowMovesOnly()
        {
            var state = new GameState();
            state.GetMember(MemberType.Wife).Health = MemberHealth.Injured;
            state.GetMember(MemberType.Player).Health = MemberHealth.Injured;
            int injured = SurvivalModel.InjuredCount(state);
            Assert.AreEqual(2, injured);
            Assert.AreEqual(1.5f, SurvivalModel.TimeMultiplier(injured, true, 0.25f, false, 1.5f), 0.0001f);
            Assert.AreEqual(1f, SurvivalModel.TimeMultiplier(injured, false, 0.25f, false, 1.5f), 0.0001f);
        }

        [Test]
        public void PoisonAccumulatesInOpenGasAndDecaysOutside()
        {
            float p = 0f;
            for (int i = 0; i < 10; i++) p = SurvivalModel.StepPoison(p, true, 0.02f, false, 0.5f, 0.01f, 1f);
            Assert.AreEqual(0.2f, p, 0.0001f);
            for (int i = 0; i < 10; i++) p = SurvivalModel.StepPoison(p, false, 0.02f, false, 0.5f, 0.01f, 1f);
            Assert.AreEqual(0.1f, p, 0.0001f, "slow decay outside gas / valve closed");
            Assert.AreEqual(0f, SurvivalModel.StepPoison(0f, false, 0.02f, false, 0.5f, 0.01f, 5f), "never below 0");
        }

        [Test]
        public void WetClothHalvesPoisonGain()
        {
            float bare = SurvivalModel.StepPoison(0f, true, 0.04f, false, 0.5f, 0.01f, 5f);
            float wet = SurvivalModel.StepPoison(0f, true, 0.04f, true, 0.5f, 0.01f, 5f);
            Assert.AreEqual(bare * 0.5f, wet, 0.0001f);
        }

        [Test]
        public void PoisonClampsAtOneWhichIsDeath()
        {
            float p = SurvivalModel.StepPoison(0.95f, true, 0.1f, false, 0.5f, 0.01f, 2f);
            Assert.AreEqual(1f, p, 0.0001f);
        }

        [Test]
        public void PanicFollowsBaselineAndCalmsSlowly()
        {
            Assert.AreEqual(0.9f, SurvivalModel.PanicBaseline(0.6f, 0.5f, 0.6f), 0.0001f, "timer + poison");
            Assert.AreEqual(0.5f, SurvivalModel.StepPanic(0.2f, 0.5f, 0.04f, 1f), 0.0001f, "rises to the baseline at once");
            Assert.AreEqual(0.86f, SurvivalModel.StepPanic(0.9f, 0.3f, 0.04f, 1f), 0.0001f, "aftershock spike decays slowly");
            Assert.AreEqual(0.3f, SurvivalModel.StepPanic(0.31f, 0.3f, 0.04f, 1f), 0.0001f, "never below baseline");
        }

        [Test]
        public void FoodRestoresStaminaAndIsConsumed()
        {
            Item food = data.Item("food");
            food.ConfigureUse(35f, false, 2f);
            var state = new GameState { Stamina = 10f };
            state.Inventory.TryAdd("food");
            Assert.IsTrue(ItemUse.Apply(state, food, 100f));
            Assert.AreEqual(45f, state.Stamina, 0.0001f);
            Assert.IsFalse(state.Inventory.Has("food"));
        }

        [Test]
        public void FirstAidTreatsInjuredMembers()
        {
            Item kit = data.Item("first_aid");
            kit.ConfigureUse(0f, true, 6f);
            var state = new GameState();
            state.Inventory.TryAdd("first_aid");
            state.GetMember(MemberType.Wife).Health = MemberHealth.Injured;
            state.Flags.Set(FlagNames.WifeInjured, true);
            Assert.IsTrue(ItemUse.Apply(state, kit, 100f));
            Assert.AreEqual(MemberHealth.Healthy, state.GetMember(MemberType.Wife).Health);
            Assert.IsFalse(state.Flags.Get(FlagNames.WifeInjured));
        }

        [Test]
        public void PlainItemsAreNotUsableAlone()
        {
            Item rope = data.Item("rope");
            var state = new GameState();
            state.Inventory.TryAdd("rope");
            Assert.IsFalse(ItemUse.Apply(state, rope, 100f));
            Assert.IsTrue(state.Inventory.Has("rope"));
        }
    }
}
