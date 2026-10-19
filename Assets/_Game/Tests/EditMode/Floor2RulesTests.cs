using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;

namespace SixtyOne.Tests
{
    /// <summary>Crafting, the Floor 2 dog gate, and epilogue line selection (borrow ledger + LeftNote).</summary>
    public class Floor2RulesTests
    {
        private TestData data;
        private Item cloth, rope, water, tape, gloves, food, whistle, harness, wetCloth, grip;
        private RecipeBook book;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            cloth = data.Item("cloth");
            rope = data.Item("rope");
            water = data.Item("water");
            tape = data.Item("tape");
            gloves = data.Item("gloves");
            food = data.Item("food");
            whistle = data.Item("whistle");
            harness = data.Item("child_harness");
            wetCloth = data.Item("wet_cloth");
            grip = data.Item("insulated_grip");

            book = data.Make<RecipeBook>();
            book.Configure(new List<Recipe>
            {
                Recipe(cloth, rope, harness, 6f, 5f),
                Recipe(cloth, water, wetCloth, 3f, 0f),
                Recipe(tape, gloves, grip, 4f, 2f)
            });
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        private Recipe Recipe(Item a, Item b, Item output, float seconds, float stamina)
        {
            Recipe recipe = data.Make<Recipe>();
            recipe.Configure("item." + output.Id + ".name", a, b, output, seconds, stamina);
            return recipe;
        }

        [Test]
        public void RecipesAreOrderIndependent()
        {
            Assert.AreSame(harness, Crafting.Find(book, cloth, rope).Output);
            Assert.AreSame(harness, Crafting.Find(book, rope, cloth).Output);
            Assert.AreSame(wetCloth, Crafting.Find(book, water, cloth).Output);
            Assert.AreSame(grip, Crafting.Find(book, gloves, tape).Output);
            Assert.IsNull(Crafting.Find(book, rope, water), "no recipe: fail message");
            Assert.IsNull(Crafting.Find(book, cloth, cloth));
        }

        [Test]
        public void CraftingSwapsInputsForOutputAndKeepsTheBagSize()
        {
            var state = new GameState();
            state.Inventory.TryAdd("rope");
            state.Inventory.TryAdd("whistle");
            state.Inventory.TryAdd("food");
            state.Inventory.TryAdd("cloth");
            Assert.IsTrue(Crafting.Craft(state, Crafting.Find(book, rope, cloth)));
            Assert.IsTrue(state.Inventory.Has("child_harness"));
            Assert.IsFalse(state.Inventory.Has("rope"));
            Assert.IsFalse(state.Inventory.Has("cloth"));
            Assert.AreEqual(3, state.Inventory.Count);
            Assert.AreEqual(Inventory.DefaultCapacity, state.Inventory.Capacity, "crafting never changes the bag size");

            Assert.IsFalse(Crafting.Craft(state, Crafting.Find(book, tape, gloves)), "inputs missing");
            Assert.IsFalse(state.Inventory.Has("insulated_grip"));
        }

        private SceneNode DogCorridor(out SceneNode flat7, out SceneNode exit)
        {
            SceneNode corridor = data.Node("f2.corridor", 3);
            flat7 = data.Node("f2.flat7", 3);
            exit = data.Node("f2.exit", 3);
            corridor.AddChoice(new Choice("f2.corridor.c_food").ShowWhen(Condition.FlagFalse("DogGone"))
                .Require(Condition.HasItem(food)).WhenLocked(FailBehaviour.ShowLocked, "f2.corridor.c_food.locked")
                .Then(Outcome.RemoveItem(food)).Then(Outcome.SetFlag("DogGone")));
            corridor.AddChoice(new Choice("f2.corridor.c_whistle").ShowWhen(Condition.FlagFalse("DogGone"))
                .Require(Condition.HasItem(whistle)).WhenLocked(FailBehaviour.ShowLocked, "f2.corridor.c_whistle.locked")
                .Then(Outcome.SetFlag("DogGone")));
            corridor.AddChoice(new Choice("f2.corridor.c_pass").ShowWhen(Condition.FlagFalse("DogGone"))
                .Then(Outcome.GoTo(flat7)));
            corridor.AddChoice(new Choice("f2.corridor.c_stairs").ShowWhen(Condition.FlagTrue("DogGone"))
                .Then(Outcome.GoTo(exit)));
            return corridor;
        }

        private static ChoiceView Find(SceneNode node, GameState state, string key)
        {
            foreach (ChoiceView view in ChoiceView.Build(node, state))
            {
                if (view.Choice.LabelKey.Key == key) return view;
            }

            return default;
        }

        [Test]
        public void DogGateFoodIsConsumed()
        {
            SceneNode corridor = DogCorridor(out _, out SceneNode exit);
            var state = new GameState();
            Assert.IsNull(Find(corridor, state, "f2.corridor.c_stairs").Choice, "stairs hidden while the dog is there");
            Assert.IsTrue(Find(corridor, state, "f2.corridor.c_food").Locked);

            state.Inventory.TryAdd("food");
            ChoiceView feed = Find(corridor, state, "f2.corridor.c_food");
            Assert.IsFalse(feed.Locked);
            OutcomeExecutor.Execute(feed.Choice.Outcomes, state);
            Assert.IsFalse(state.Inventory.Has("food"));
            Assert.AreSame(exit, OutcomeExecutor.Execute(Find(corridor, state, "f2.corridor.c_stairs").Choice.Outcomes, state).NextNode);
        }

        [Test]
        public void DogGateWhistleIsKept()
        {
            SceneNode corridor = DogCorridor(out _, out _);
            var state = new GameState();
            whistle.ConfigureKeyItem();
            StateOps.TryAddItem(state, "whistle", whistle.IsKeyItem);
            Assert.IsFalse(Find(corridor, state, "f2.corridor.c_whistle").Locked, "a key item satisfies HasItem");
            OutcomeExecutor.Execute(Find(corridor, state, "f2.corridor.c_whistle").Choice.Outcomes, state);
            Assert.IsTrue(StateOps.HasItem(state, "whistle"), "the whistle is kept");
            Assert.IsNotNull(Find(corridor, state, "f2.corridor.c_stairs").Choice);
        }

        [Test]
        public void EpilogueBorrowLineDependsOnLedgerAndNote()
        {
            EpilogueNode node = data.Make<EpilogueNode>();
            node.Configure("f2.epilogue", "f2.epilogue.title", "f2.epilogue.body", "street", 3);
            node.ConfigureEpilogue("f2.epilogue");

            var state = new GameState();
            Assert.AreEqual("f2.epilogue.borrow_none", EpilogueText.BorrowLineKey(node, state));
            Assert.AreEqual("f2.epilogue.family_ok", EpilogueText.FamilyLineKey(node, state));

            state.Borrowed.Add("rope");
            Assert.AreEqual("f2.epilogue.borrow_no_note", EpilogueText.BorrowLineKey(node, state));
            state.Flags.Set(FlagNames.LeftNote, true);
            Assert.AreEqual("f2.epilogue.borrow_note", EpilogueText.BorrowLineKey(node, state));

            StateOps.SetHealth(state, MemberType.Child, MemberHealth.Injured);
            Assert.AreEqual("f2.epilogue.family_hurt", EpilogueText.FamilyLineKey(node, state));
        }

        [Test]
        public void BorrowLedgerIsRestoredByCheckpointSnapshot()
        {
            var state = new GameState();
            state.Borrowed.Add("cloth");
            Snapshot checkpoint = Snapshot.Capture(state, "f2.flat7");
            state.Borrowed.Add("rope");
            checkpoint.RestoreInto(state, false);
            Assert.AreEqual(1, state.Borrowed.Count);
            Assert.IsTrue(state.Borrowed.Contains("cloth"));
        }
    }
}
