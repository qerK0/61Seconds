using SixtyOne.Dialogue;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Core;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string F4Intro = "f4.intro";
        public const string F4Quake = "f4.quake";
        public const string F4Salon = "f4.salon";
        public const string F4Kitchen = "f4.kitchen";
        public const string F4Hall = "f4.hall";
        public const string F4CorridorNode = "f4.corridor";
        public const string F4Neighbor = "f4.neighbor";
        public const string F4Flat12 = "f4.flat12";

        /// <summary>
        /// Floor 4 (quake floor). Flat 11: intro neighbour dialogue → opening quake placement → salon / kitchen
        /// (gas valve, traps) / hall (breaker, exit). Corridor: kind neighbour gift, find Flat 12, stairs down.
        /// Flat 12: borrow supplies (world pickups) + "Not bırak".
        /// </summary>
        private static void BuildFloor4()
        {
            const string floor = "Floor4";
            DialogueNode intro = Node<DialogueNode>(floor, F4Intro);
            PlacementNode quake = Node<PlacementNode>(floor, F4Quake);
            SceneNode salon = Node<SceneNode>(floor, F4Salon);
            SceneNode kitchen = Node<SceneNode>(floor, F4Kitchen);
            SceneNode hall = Node<SceneNode>(floor, F4Hall);
            SceneNode corridor = Node<SceneNode>(floor, F4CorridorNode);
            DialogueNode neighbor = Node<DialogueNode>(floor, F4Neighbor);
            SceneNode flat12 = Node<SceneNode>(floor, F4Flat12);
            SceneNode f3Arrival = Floor3Arrival();

            // Intro: the neighbour drops by. Every line costs building time; "Kısa kes" skips the info and the whistle.
            Setup(intro, F4Intro, Flat11Salon, 1);
            intro.SetCheckpoint(true);
            intro.SetLines(new[]
            {
                Line("npc.sevim.name", F4Intro, 1, 4f),
                Line("npc.sevim.name", F4Intro, 2, 4f),
                Line("npc.sevim.name", F4Intro, 3, 3f),
                Line("npc.sevim.name", F4Intro, 4, 3f)
            });
            intro.AddChoice(C(F4Intro, "hurry").AsDuringDialogue()
                .Then(Outcome.SetFlag(IntroCutShort))
                .Then(Outcome.GoTo(quake)));
            intro.AddChoice(C(F4Intro, "thanks")
                .Then(Outcome.AddItem(Item("whistle")))
                .Then(Outcome.SetFlag(KnowsFlat12))
                .Then(Outcome.GoTo(quake)));

            // Opening quake: placement in the salon.
            Setup(quake, F4Quake, Flat11Salon, 1, 10f);
            quake.SetAfadAlert(true);
            quake.ConfigurePlacement(salon, result.Hazards["hz.open_room"], new DeathData("death.child_alone", Tip("tip.keep_child_close"), MemberType.Child), 4f);
            quake.ConfigureQuake(7.1f, 1.3f);

            Setup(salon, F4Salon, Flat11Salon, 1);
            salon.AddChoice(C(F4Salon, "kitchen").Move(2f).Then(Outcome.GoTo(kitchen)));
            salon.AddChoice(C(F4Salon, "hall").Move(2f).Then(Outcome.GoTo(hall)));

            // Kitchen: the gas leaks (poison) until the valve is closed. Switches and flames with gas = death.
            Setup(kitchen, F4Kitchen, Flat11Kitchen, 1);
            kitchen.AddChoice(C(F4Kitchen, "valve").Costs(5f)
                .ShowWhen(Condition.FlagFalse(GasOffF4))
                .Then(Outcome.SetFlag(GasOffF4)));
            kitchen.AddChoice(C(F4Kitchen, "light", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(GasOffF4))
                .Kills(Death("death.switch_gas", "tip.no_switch_gas")));
            kitchen.AddChoice(C(F4Kitchen, "tea", ChoiceType.Trap)
                .Kills(Death("death.stove", "tip.no_flame")));
            kitchen.AddChoice(C(F4Kitchen, "back").Move(2f).Then(Outcome.GoTo(salon)));

            Setup(hall, F4Hall, Flat11Hall, 1);
            hall.AddChoice(C(F4Hall, "breaker").Costs(4f)
                .ShowWhen(Condition.FlagFalse(FlagNames.PowerOff))
                .Then(Outcome.SetFlag(FlagNames.PowerOff)));
            hall.AddChoice(C(F4Hall, "back").Move(2f).Then(Outcome.GoTo(salon)));
            hall.AddChoice(C(F4Hall, "exit").Move(3f).Then(Outcome.GoTo(corridor)));

            // Corridor hub.
            Setup(corridor, F4CorridorNode, F4Corridor, 1);
            corridor.AddChoice(C(F4CorridorNode, "talk")
                .ShowWhen(Condition.FlagFalse(GiftDone))
                .ShowWhen(Condition.FlagFalse(NeighborGone))
                .Then(Outcome.GoTo(neighbor)));
            corridor.AddChoice(C(F4CorridorNode, "search").Costs(8f)
                .ShowWhen(Condition.FlagFalse(KnowsFlat12))
                .Then(Outcome.SetFlag(KnowsFlat12))
                .Then(Outcome.Message($"{F4CorridorNode}.msg_found")));
            corridor.AddChoice(C(F4CorridorNode, "flat12").Move(2f)
                .ShowWhen(Condition.FlagTrue(KnowsFlat12))
                .Then(Outcome.GoTo(flat12)));
            corridor.AddChoice(C(F4CorridorNode, "elevator", ChoiceType.Trap)
                .Kills(Death("death.elevator", "tip.no_elevator")));
            corridor.AddChoice(C(F4CorridorNode, "home").Move(3f).Then(Outcome.GoTo(hall)));
            corridor.AddChoice(C(F4CorridorNode, "down").Move(6f).Then(Outcome.GoTo(f3Arrival)));

            // Kind neighbour: food + water (bag permitting). Hurrying = no gift.
            Setup(neighbor, F4Neighbor, F4Corridor, 1);
            neighbor.SetLines(new[]
            {
                Line("npc.hasan.name", F4Neighbor, 1, 3f),
                Line("npc.hasan.name", F4Neighbor, 2, 3f)
            });
            neighbor.AddChoice(C(F4Neighbor, "hurry").AsDuringDialogue()
                .Then(Outcome.SetFlag(NeighborGone))
                .Then(Outcome.GoTo(corridor)));
            neighbor.AddChoice(C(F4Neighbor, "thanks")
                .Then(Outcome.AddItem(Item("food")))
                .Then(Outcome.AddItem(Item("water")))
                .Then(Outcome.SetFlag(GiftDone))
                .Then(Outcome.GoTo(corridor)));

            // Flat 12: the neighbours are away and the door is open. Supplies are borrowed (world pickups).
            Setup(flat12, F4Flat12, Flat12, 1);
            flat12.AddChoice(C(F4Flat12, "note").Costs(2f)
                .ShowWhen(Condition.FlagFalse(FlagNames.LeftNote))
                .Then(Outcome.SetFlag(FlagNames.LeftNote))
                .Then(Outcome.Message($"{F4Flat12}.msg_note")));
            flat12.AddChoice(C(F4Flat12, "back").Move(2f).Then(Outcome.GoTo(corridor)));
        }
    }
}
