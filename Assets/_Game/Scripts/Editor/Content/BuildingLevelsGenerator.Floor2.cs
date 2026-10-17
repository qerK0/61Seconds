using System.Collections.Generic;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Placement;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string Flat7 = "flat7";
        public const string Flat8 = "flat8";
        public const string F2CorridorRoom = "f2_corridor";
        public const string F2Stairs = "f2_stairs";
        public const string Street = "street";

        public const string F2Aftershock = "f2.aftershock";
        public const string F2Flat7 = "f2.flat7";
        public const string F2CorridorNode = "f2.corridor";
        public const string F2Flat8 = "f2.flat8";
        public const string F2Exit = "f2.exit";
        public const string F2Epilogue = "f2.epilogue";

        public const string GasOffF2 = "GasOff_F2_7";
        public const string DogGone = "DogGone";

        public const string RecipesDir = DataRoot + "/Recipes";

        private static SceneNode f2ArrivalNode;

        /// <summary>Floor 2 arrival = strong aftershock placement + checkpoint (created early so Floor 3 can link to it).</summary>
        private static SceneNode Floor2Arrival()
        {
            if (f2ArrivalNode != null && result.Nodes.ContainsKey(F2Aftershock)) return f2ArrivalNode;
            f2ArrivalNode = Node<PlacementNode>("Floor2", F2Aftershock);
            return f2ArrivalNode;
        }

        private static void BuildFloor2Hazards()
        {
            Hazard("hz.f7_bookshelf", HazardEffect.Death, HazardEffect.Death, HazardMotion.Topple, "bookshelf", "bookshelf", "tip.wardrobe");
            Hazard("hz.f7_window", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Shatter, "f7_window", "window", "tip.no_window");
            Hazard("hz.f7_open", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "f7_ceiling", "open_room", "tip.drop_cover_hold");
        }

        /// <summary>Recipes (order-independent): cloth+rope → child harness, cloth+water → wet cloth, tape+gloves → insulated grip.</summary>
        private static void BuildRecipes()
        {
            var recipes = new List<Recipe>
            {
                Recipe("child_harness", "cloth", "rope", 6f, 5f),
                Recipe("wet_cloth", "cloth", "water", 3f, 0f),
                Recipe("insulated_grip", "tape", "gloves", 4f, 2f)
            };

            result.RecipeBook = Asset<RecipeBook>(RecipesDir + "/RecipeBook.asset");
            result.RecipeBook.Configure(recipes);
        }

        private static Recipe Recipe(string output, string a, string b, float seconds, float stamina)
        {
            Recipe recipe = Asset<Recipe>($"{RecipesDir}/recipe.{output}.asset");
            recipe.Configure($"item.{output}.name", Item(a), Item(b), Item(output), seconds, stamina);
            result.Recipes.Add(recipe);
            return recipe;
        }

        /// <summary>
        /// Floor 2: strong aftershock on arrival (placement in Flat 7, checkpoint) → Flat 7 (gas leak, poison) →
        /// corridor with the dog gate (food is consumed, the whistle is kept; walking past = bitten, injured and
        /// pushed back) → stairs → street epilogue.
        /// </summary>
        private static void BuildFloor2()
        {
            const string floor = "Floor2";
            var arrival = (PlacementNode)Floor2Arrival();
            SceneNode flat7 = Node<SceneNode>(floor, F2Flat7);
            SceneNode corridor = Node<SceneNode>(floor, F2CorridorNode);
            SceneNode flat8 = Node<SceneNode>(floor, F2Flat8);
            SceneNode exit = Node<SceneNode>(floor, F2Exit);
            EpilogueNode epilogue = Node<EpilogueNode>(floor, F2Epilogue);

            Setup(arrival, F2Aftershock, Flat7, 3, 9f);
            arrival.SetCheckpoint(true);
            arrival.SetAfadAlert(true);
            arrival.ConfigurePlacement(flat7, result.Hazards["hz.f7_open"], new DeathData("death.child_alone", Tip("tip.keep_child_close"), MemberType.Child), 3.5f);
            arrival.ConfigureQuake(6.2f, 1.1f);

            // Flat 7: gas leaking (poison until the valve is closed). The wet cloth halves the gain.
            Setup(flat7, F2Flat7, Flat7, 3);
            flat7.AddChoice(C(F2Flat7, "valve").Costs(4f)
                .ShowWhen(Condition.FlagFalse(GasOffF2))
                .Then(Outcome.SetFlag(GasOffF2)));
            flat7.AddChoice(C(F2Flat7, "lighter", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(GasOffF2))
                .Require(Condition.HasItem(Item("lighter")))
                .Kills(Death("death.lighter_explosion", "tip.no_flame")));
            flat7.AddChoice(C(F2Flat7, "corridor").Move(2f).Then(Outcome.GoTo(corridor)));

            // Corridor: the frightened dog blocks the stairs. No weapons; feed it or call it away.
            Setup(corridor, F2CorridorNode, F2CorridorRoom, 3);
            corridor.AddChoice(C(F2CorridorNode, "food").Costs(2f)
                .ShowWhen(Condition.FlagFalse(DogGone))
                .Require(Condition.HasItem(Item("food")))
                .WhenLocked(FailBehaviour.ShowLocked, Locked(F2CorridorNode, "food"))
                .Then(Outcome.RemoveItem(Item("food")))
                .Then(Outcome.SetFlag(DogGone))
                .Then(Outcome.Message($"{F2CorridorNode}.msg_fed")));
            corridor.AddChoice(C(F2CorridorNode, "whistle").Costs(2f)
                .ShowWhen(Condition.FlagFalse(DogGone))
                .Require(Condition.HasItem(Item("whistle")))
                .WhenLocked(FailBehaviour.ShowLocked, Locked(F2CorridorNode, "whistle"))
                .Then(Outcome.SetFlag(DogGone))
                .Then(Outcome.Message($"{F2CorridorNode}.msg_whistle")));
            corridor.AddChoice(C(F2CorridorNode, "pass").Costs(3f)
                .ShowWhen(Condition.FlagFalse(DogGone))
                .Then(Outcome.SetMember(MemberType.Player, MemberHealth.Injured))
                .Then(Outcome.SetFlag(FlagNames.PlayerInjured))
                .Then(Outcome.Message($"{F2CorridorNode}.msg_bitten"))
                .Then(Outcome.GoTo(flat7)));
            corridor.AddChoice(C(F2CorridorNode, "stairs").Move(4f)
                .ShowWhen(Condition.FlagTrue(DogGone))
                .Then(Outcome.GoTo(exit)));
            corridor.AddChoice(C(F2CorridorNode, "flat8").Move(2f).Then(Outcome.GoTo(flat8)));
            corridor.AddChoice(C(F2CorridorNode, "flat7").Move(2f).Then(Outcome.GoTo(flat7)));

            // Flat 8: empty placeholder flat (one hidden item at most).
            Setup(flat8, F2Flat8, Flat8, 3);
            flat8.AddChoice(C(F2Flat8, "back").Move(2f).Then(Outcome.GoTo(corridor)));

            // Stairs down to the street.
            Setup(exit, F2Exit, F2Stairs, 3);
            exit.AddChoice(C(F2Exit, "out").Move(5f).Then(Outcome.GoTo(epilogue)));
            exit.AddChoice(C(F2Exit, "elevator", ChoiceType.Trap).Kills(Death("death.elevator", "tip.no_elevator")));

            Setup(epilogue, F2Epilogue, Street, 3);
            epilogue.ConfigureEpilogue(F2Epilogue);
        }
    }
}
