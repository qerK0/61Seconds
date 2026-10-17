using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.QTE;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string Flat9 = "flat9";
        public const string Flat10 = "flat10";
        public const string Flat9Living = "flat9_living";
        public const string Flat9Kitchen = "flat9_kitchen";
        public const string Flat10Kitchen = "flat10_kitchen";
        public const string Flat10Living = "flat10_living";

        public const string F3Aftershock = "f3.aftershock";
        public const string F3CorridorNode = "f3.corridor";
        public const string F3Trader = "f3.trader";
        public const string F3Flat9 = "f3.flat9";
        public const string F3Fuse = "f3.fuse";
        public const string F3Flat10 = "f3.flat10";

        public const string PowerCutF3 = "PowerCut_F3";
        public const string GasOffF3 = "GasOff_F3_10";
        public const string Flat10Lit = "Flat10Lit";
        public const string TraderDone = "TraderDone";
        public const string TraderLeft = "TraderLeft";

        private static SceneNode f3ArrivalNode;

        /// <summary>Floor 3 arrival = aftershock placement + checkpoint (created early so Floor 4 can link to it).</summary>
        private static SceneNode Floor3Arrival()
        {
            if (f3ArrivalNode != null && result.Nodes.ContainsKey(F3Aftershock)) return f3ArrivalNode;
            f3ArrivalNode = Node<PlacementNode>("Floor3", F3Aftershock);
            return f3ArrivalNode;
        }

        private static void BuildFloor3Hazards()
        {
            Hazard("hz.f3_stair_edge", HazardEffect.Death, HazardEffect.Death, HazardMotion.Drop, "stair_edge", "stair_edge", "tip.aftershock");
            Hazard("hz.f3_lamp", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "corridor_lamp", "lamp_fall", "tip.aftershock");
            Hazard("hz.f3_window", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Shatter, "corridor_window", "window", "tip.no_window");
            Hazard("hz.f3_open", HazardEffect.Death, HazardEffect.Injured, HazardMotion.Drop, "ceiling_debris", "open_room", "tip.drop_cover_hold");
        }

        /// <summary>
        /// Floor 3: aftershock on arrival (placement, checkpoint) → corridor (needs-aware trader, stairs below
        /// collapsed) → Flat 9 (fuse box wiring = WASD QTE; gloves/grip + tape or electrocution; cables need the
        /// child harness; balcony + bedsheet rope = RopeDescentInteractable) / Flat 10 (dark, gas leak, lighter = explosion, flashlight, collapsed floor = way down).
        /// </summary>
        private static void BuildFloor3()
        {
            const string floor = "Floor3";
            var arrival = (PlacementNode)Floor3Arrival();
            SceneNode corridor = Node<SceneNode>(floor, F3CorridorNode);
            DialogueNode trader = Node<DialogueNode>(floor, F3Trader);
            SceneNode flat9 = Node<SceneNode>(floor, F3Flat9);
            QTENode fuse = Node<QTENode>(floor, F3Fuse);
            SceneNode flat10 = Node<SceneNode>(floor, F3Flat10);
            SceneNode f2Arrival = Floor2Arrival();

            TraderProfile profile = Asset<TraderProfile>(DataRoot + "/Traders/trader.f3.asset");
            profile.Configure(
                new[] { Item("flashlight"), Item("gloves"), Item("tape"), Item("rope"), Item("cloth"), Item("whistle") },
                new[] { Item("food"), Item("water") });
            result.Trader = profile;

            // Aftershock on arrival: placement in the corridor, floor checkpoint (timer included).
            Setup(arrival, F3Aftershock, F3Corridor, 2, 10f);
            arrival.SetCheckpoint(true);
            arrival.SetAfadAlert(true);
            arrival.ConfigurePlacement(corridor, result.Hazards["hz.f3_open"], new DeathData("death.child_alone", Tip("tip.keep_child_close"), MemberType.Child), 3f);
            arrival.ConfigureQuake(5.4f, 0.8f);
            AddGasBomb(arrival);

            Setup(corridor, F3CorridorNode, F3Corridor, 2);
            corridor.AddChoice(C(F3CorridorNode, "trader")
                .ShowWhen(Condition.FlagFalse(TraderDone))
                .ShowWhen(Condition.FlagFalse(TraderLeft))
                .Then(Outcome.GoTo(trader)));
            corridor.AddChoice(C(F3CorridorNode, "flat9").Move(2f).Then(Outcome.GoTo(flat9)));
            corridor.AddChoice(C(F3CorridorNode, "flat10").Move(2f).Then(Outcome.GoTo(flat10)));

            // Trader: talking costs building time; gives the first thing you will need and lack.
            Setup(trader, F3Trader, F3Corridor, 2);
            trader.SetLines(new[]
            {
                Line("npc.cemil.name", F3Trader, 1, 3f),
                Line("npc.cemil.name", F3Trader, 2, 3f),
                Line("npc.cemil.name", F3Trader, 3, 2f)
            });
            trader.AddChoice(C(F3Trader, "hurry").AsDuringDialogue()
                .Then(Outcome.SetFlag(TraderLeft))
                .Then(Outcome.GoTo(corridor)));
            trader.AddChoice(C(F3Trader, "take")
                .Then(Outcome.GiveFromTrader(profile))
                .Then(Outcome.SetFlag(TraderDone))
                .Then(Outcome.GoTo(corridor)));

            // Flat 9: exposed cables + fuse box. Without (gloves or insulated grip) + tape: electrocution (micro rewind).
            Setup(flat9, F3Flat9, Flat9, 2);
            flat9.AddChoice(C(F3Flat9, "repair")
                .ShowWhen(Condition.FlagFalse(PowerCutF3))
                .ShowWhen(Condition.InRoom(Flat9))
                .Require(Condition.HasAnyItem(Item("gloves"), Item("insulated_grip")))
                .Require(Condition.HasItem(Item("tape")))
                .WhenLocked(FailBehaviour.ShowLockedFatal, Locked(F3Flat9, "repair"))
                .WithMissingText(Item("gloves"), "death.electrocuted")
                .FatalRewind(RewindTarget.LastNode)
                .WithDeath(Death("death.electrocuted", "tip.electric"))
                .Costs(4f)
                .Then(Outcome.GoTo(fuse)));
            flat9.AddChoice(C(F3Flat9, "cables").Costs(12f, 25f, isHeavy: true, move: true)
                .ShowWhen(Condition.FlagTrue(PowerCutF3))
                .ShowWhen(Condition.InRoom(Flat9Kitchen))
                .Require(Condition.HasItem(Item("child_harness")))
                .WhenLocked(FailBehaviour.ShowLocked, Locked(F3Flat9, "cables"))
                .Then(Outcome.GoTo(f2Arrival)));
            flat9.AddChoice(C(F3Flat9, "back").Move(2f).Then(Outcome.GoTo(corridor)));

            Setup(fuse, F3Fuse, Flat9, 2);
            fuse.ConfigureQte(QTEContext.BreakerWiring, result.QteBreaker, new[] { Outcome.SetFlag(PowerCutF3) }, null, flat9);
            fuse.AddChoice(C(F3Fuse, "back").Then(Outcome.GoTo(flat9)));

            // Flat 10: dark + gas leak. Lighter before the valve = explosion; flashlight is the safe light.
            Setup(flat10, F3Flat10, Flat10, 2);
            flat10.AddChoice(C(F3Flat10, "lighter", ChoiceType.Trap)
                .ShowWhen(Condition.FlagFalse(GasOffF3))
                .Require(Condition.HasItem(Item("lighter")))
                .Kills(Death("death.lighter_explosion", "tip.no_flame")));
            flat10.AddChoice(C(F3Flat10, "flashlight").Costs(1f)
                .ShowWhen(Condition.FlagFalse(Flat10Lit))
                .Require(Condition.HasItem(Item("flashlight")))
                .WhenLocked(FailBehaviour.ShowLocked, Locked(F3Flat10, "flashlight"))
                .Then(Outcome.SetFlag(Flat10Lit)));
            flat10.AddChoice(C(F3Flat10, "valve").Costs(5f)
                .ShowWhen(Condition.FlagTrue(Flat10Lit))
                .ShowWhen(Condition.InRoom(Flat10Kitchen))
                .ShowWhen(Condition.FlagFalse(GasOffF3))
                .Then(Outcome.SetFlag(GasOffF3)));
            flat10.AddChoice(C(F3Flat10, "hole").Costs(10f, 20f, isHeavy: true, move: true)
                .ShowWhen(Condition.FlagTrue(Flat10Lit))
                .ShowWhen(Condition.InRoom(Flat10Living))
                .Then(Outcome.GoTo(f2Arrival)));
            flat10.AddChoice(C(F3Flat10, "back").Move(2f).Then(Outcome.GoTo(corridor)));
        }

        /// <summary>
        /// Gas left open in Flat 11 → fire upstairs fills the stairwell with smoke on Floor 3. Macro rewind: the
        /// valve needs the wrench, which can only be taken during the prep, so a micro rewind would soft-lock.
        /// </summary>
        private static void AddGasBomb(SceneNode arrival)
        {
            arrival.AddBombCheck(new BombCheck()
                .When(Condition.FlagFalse(GasOffF4))
                .Kills(new DeathData("death.kitchen_fire", Tip("tip.gas_off"), MemberType.Player, null, result.FireCue, RewindTarget.Checkpoint)));
        }
    }
}
