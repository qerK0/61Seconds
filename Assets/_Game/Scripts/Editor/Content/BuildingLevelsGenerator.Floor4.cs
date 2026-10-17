using SixtyOne.Dialogue;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Core;
using SixtyOne.Prep;
using SixtyOne.Text;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string F4PrepSalon = "f4.prep.salon";
        public const string F4PrepKitchen = "f4.prep.kitchen";
        public const string F4PrepHall = "f4.prep.hall";
        public const string F4Quake = "f4.quake";
        public const string F4Salon = "f4.salon";
        public const string F4Kitchen = "f4.kitchen";
        public const string F4Hall = "f4.hall";
        public const string F4Sevim = "f4.sevim";
        public const string F4Bedroom = "f4.bedroom";
        public const string F4CorridorNode = "f4.corridor";
        public const string F4Neighbor = "f4.neighbor";
        public const string F4Flat12 = "f4.flat12";

        /// <summary>Prep seconds between the AFAD early warning and the quake (LevelConfig).</summary>
        public const float PrepSeconds = 60f;

        /// <summary>
        /// Floor 4 (quake floor), in order:
        /// 1. Prep (checkpoint): AFAD early warning, 60 s to loot our own flat (salon / kitchen / hall); costs come out
        ///    of the prep countdown, the building timer is not running yet.
        /// 2. Prep zero = quake: the building timer starts. 3. Opening quake placement in the salon.
        /// 4. Post-quake flat (gas valve needs the wrench, breaker, bedroom with bedsheets for the rope, exit) → corridor:
        ///    Sevim Teyze's dialogue on the way out ("Kısa kes" = no whistle, no Flat 12 info) → Hasan's gift, Flat 12
        ///    borrow + "Not bırak", stairs down.
        /// </summary>
        private static void BuildFloor4()
        {
            const string floor = "Floor4";
            PrepPhaseNode prepSalon = Node<PrepPhaseNode>(floor, F4PrepSalon);
            PrepPhaseNode prepKitchen = Node<PrepPhaseNode>(floor, F4PrepKitchen);
            PrepPhaseNode prepHall = Node<PrepPhaseNode>(floor, F4PrepHall);
            PlacementNode quake = Node<PlacementNode>(floor, F4Quake);
            SceneNode salon = Node<SceneNode>(floor, F4Salon);
            SceneNode kitchen = Node<SceneNode>(floor, F4Kitchen);
            SceneNode hall = Node<SceneNode>(floor, F4Hall);
            DialogueNode sevim = Node<DialogueNode>(floor, F4Sevim);
            SceneNode bedroom = Node<SceneNode>(floor, F4Bedroom);
            SceneNode corridor = Node<SceneNode>(floor, F4CorridorNode);
            DialogueNode neighbor = Node<DialogueNode>(floor, F4Neighbor);
            SceneNode flat12 = Node<SceneNode>(floor, F4Flat12);
            SceneNode f3Arrival = Floor3Arrival();

            // 1. Prep: the floor checkpoint is its start (empty bag, prep 60, building timer full and stopped).
            Setup(prepSalon, F4PrepSalon, Flat11Salon, 1);
            prepSalon.SetCheckpoint(true);
            prepSalon.ConfigurePrep(quake, true, "prep.alert.title", "prep.alert.body");
            prepSalon.AddChoice(C(F4PrepSalon, "kitchen").Move(1f).Then(Outcome.GoTo(prepKitchen)));
            prepSalon.AddChoice(C(F4PrepSalon, "hall").Move(1f).Then(Outcome.GoTo(prepHall)));

            Setup(prepKitchen, F4PrepKitchen, Flat11Kitchen, 1);
            prepKitchen.ConfigurePrep(quake, false, default(TextKey), default(TextKey));
            prepKitchen.AddChoice(ValveChoice(F4PrepKitchen));
            prepKitchen.AddChoice(C(F4PrepKitchen, "back").Move(1f).Then(Outcome.GoTo(prepSalon)));

            Setup(prepHall, F4PrepHall, Flat11Hall, 1);
            prepHall.ConfigurePrep(quake, false, default(TextKey), default(TextKey));
            prepHall.AddChoice(BreakerChoice(F4PrepHall));
            prepHall.AddChoice(C(F4PrepHall, "back").Move(1f).Then(Outcome.GoTo(prepSalon)));

            // 2-3. Prep zero → quake onset (PrepPhase) → opening quake placement in the salon.
            Setup(quake, F4Quake, Flat11Salon, 1, 10f);
            quake.SetAfadAlert(true);
            quake.ConfigurePlacement(salon, result.Hazards["hz.open_room"], new DeathData("death.child_alone", Tip("tip.keep_child_close"), MemberType.Child), 4f);
            quake.ConfigureQuake(7.1f, 1.3f);

            Setup(salon, F4Salon, Flat11Salon, 1);
            salon.AddChoice(C(F4Salon, "kitchen").Move(2f).Then(Outcome.GoTo(kitchen)));
            salon.AddChoice(C(F4Salon, "hall").Move(2f).Then(Outcome.GoTo(hall)));
            salon.AddChoice(C(F4Salon, "bedroom").Move(2f).Then(Outcome.GoTo(bedroom)));

            // Bedroom: bedsheets (world pickups) for the rope; nothing else to do here.
            Setup(bedroom, F4Bedroom, Flat11Bedroom, 1);
            bedroom.AddChoice(C(F4Bedroom, "back").Move(2f).Then(Outcome.GoTo(salon)));

            // Kitchen after the quake: the gas leaks (poison) until the valve is closed (wrench). Switches and flames = death.
            Setup(kitchen, F4Kitchen, Flat11Kitchen, 1);
            kitchen.AddChoice(ValveChoice(F4Kitchen));
            kitchen.AddChoice(C(F4Kitchen, "light", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(GasOffF4))
                .Kills(Death("death.switch_gas", "tip.no_switch_gas")));
            kitchen.AddChoice(C(F4Kitchen, "tea", ChoiceType.Trap)
                .Kills(Death("death.stove", "tip.no_flame")));
            kitchen.AddChoice(C(F4Kitchen, "back").Move(2f).Then(Outcome.GoTo(salon)));

            Setup(hall, F4Hall, Flat11Hall, 1);
            hall.AddChoice(BreakerChoice(F4Hall));
            hall.AddChoice(C(F4Hall, "back").Move(2f).Then(Outcome.GoTo(salon)));
            hall.AddChoice(C(F4Hall, "exit").Move(3f)
                .ShowWhen(Condition.FlagFalse(SevimDone))
                .Then(Outcome.GoTo(sevim)));
            hall.AddChoice(C(F4Hall, "exit_again").Move(3f)
                .ShowWhen(Condition.FlagTrue(SevimDone))
                .Then(Outcome.GoTo(corridor)));

            // 4. Sevim Teyze meets us in the corridor on the way out. Every line costs building time;
            // "Kısa kes" skips the Flat 12 info and the whistle.
            Setup(sevim, F4Sevim, F4Corridor, 1);
            sevim.SetLines(new[]
            {
                Line("npc.sevim.name", F4Sevim, 1, 4f),
                Line("npc.sevim.name", F4Sevim, 2, 4f),
                Line("npc.sevim.name", F4Sevim, 3, 3f),
                Line("npc.sevim.name", F4Sevim, 4, 3f)
            });
            sevim.AddChoice(C(F4Sevim, "hurry").AsDuringDialogue()
                .Then(Outcome.SetFlag(IntroCutShort))
                .Then(Outcome.SetFlag(SevimDone))
                .Then(Outcome.GoTo(corridor)));
            sevim.AddChoice(C(F4Sevim, "thanks")
                .Then(Outcome.AddItem(Item("whistle")))
                .Then(Outcome.SetFlag(KnowsFlat12))
                .Then(Outcome.SetFlag(SevimDone))
                .Then(Outcome.GoTo(corridor)));

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

        /// <summary>Gas valve: 5 s, needs the wrench. Costs prep time before the quake, building time after.</summary>
        private static Choice ValveChoice(string nodeId)
        {
            return C(nodeId, "valve").Costs(5f)
                .ShowWhen(Condition.FlagFalse(GasOffF4))
                .Require(Condition.HasItem(Item("wrench")))
                .WhenLocked(FailBehaviour.ShowLocked, Locked(nodeId, "valve"))
                .Then(Outcome.SetFlag(GasOffF4));
        }

        /// <summary>Main breaker: 4 s. Costs prep time before the quake, building time after.</summary>
        private static Choice BreakerChoice(string nodeId)
        {
            return C(nodeId, "breaker").Costs(4f)
                .ShowWhen(Condition.FlagFalse(FlagNames.PowerOff))
                .Then(Outcome.SetFlag(FlagNames.PowerOff));
        }
    }
}
