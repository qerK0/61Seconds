using SixtyOne.Core;

namespace SixtyOne.Prep
{
    /// <summary>Pure prep-phase rules on GameState (unit-tested): who pays action costs, and the quake hand-over.</summary>
    public static class PrepRules
    {
        /// <summary>Before the quake, action costs come out of the prep countdown, not the building timer.</summary>
        public static bool SpendsPrep(GameState state) => state != null && !state.BuildingTimerStarted;

        /// <summary>Prep zero = the quake: the prep is over and the building timer starts now.</summary>
        public static void StartBuildingTimer(GameState state)
        {
            state.PrepTime = 0f;
            state.BuildingTimerStarted = true;
        }

        /// <summary>A new run / floor-4 checkpoint state: full prep, full building timer, not running.</summary>
        public static void ResetForPrep(GameState state, float prepSeconds, float buildingSeconds)
        {
            state.PrepTime = prepSeconds;
            state.BuildingTime = buildingSeconds;
            state.BuildingTimerStarted = prepSeconds <= 0f;
        }
    }
}
