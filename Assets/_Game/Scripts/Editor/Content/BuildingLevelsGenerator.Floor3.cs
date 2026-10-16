using SixtyOne.Nodes;

namespace SixtyOne.Editor
{
    public static partial class BuildingLevelsGenerator
    {
        public const string F3Aftershock = "f3.aftershock";

        /// <summary>Floor 3 arrival. Placeholder end node until Floor 3 content lands (delayed gas bomb already active).</summary>
        private static SceneNode Floor3Arrival()
        {
            SceneNode arrival = Node<SceneNode>("Floor3", F3Aftershock);
            Setup(arrival, F3Aftershock, F3Corridor, 2);
            arrival.SetCheckpoint(true);
            AddGasBomb(arrival);
            return arrival;
        }

        private static void BuildFloor3()
        {
        }

        /// <summary>Gas left open in Flat 11 → fire upstairs fills the stairwell with smoke on Floor 3.</summary>
        private static void AddGasBomb(SceneNode arrival)
        {
            arrival.AddBombCheck(new BombCheck()
                .When(Condition.FlagFalse(GasOffF4))
                .Kills(Death("death.kitchen_fire", "tip.gas_off")));
        }
    }
}
