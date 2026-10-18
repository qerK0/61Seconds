using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// After a rewind the restored snapshot must never kill again on its own: a checkpoint taken with no stamina,
    /// high exhaustion or poison, or almost no building time would loop the same death (and its death card and
    /// info card) for the rest of the run. Pure, unit-tested.
    /// </summary>
    public static class RewindRecovery
    {
        /// <summary>Poison / exhaustion kept after a micro rewind (0..1): far from the death line.</summary>
        public const float MicroCap = 0.35f;

        /// <summary>Stamina floor after a rewind, as a fraction of the maximum.</summary>
        public const float StaminaFloor = 0.4f;

        /// <summary>Clamps the restored survival values so the same death cannot fire right away.</summary>
        public static void Apply(GameState state, bool macro, float staminaMax, float minBuildingSeconds)
        {
            if (state == null) return;
            state.Poison = macro ? 0f : System.Math.Min(state.Poison, MicroCap);
            state.Exhaustion = macro ? 0f : System.Math.Min(state.Exhaustion, MicroCap);
            if (state.Stamina < staminaMax * StaminaFloor) state.Stamina = staminaMax * StaminaFloor;
            if (state.BuildingTimerStarted && state.BuildingTime < minBuildingSeconds) state.BuildingTime = minBuildingSeconds;
        }
    }

    /// <summary>
    /// The info card a macro rewind may show: once per checkpoint and death cause, so repeated deaths on a floor
    /// never bring the same card back. Lives for the scene (a new run starts with a fresh gate).
    /// </summary>
    public sealed class RewindCardGate
    {
        private readonly HashSet<string> shown = new HashSet<string>();

        /// <summary>True (and remembered) the first time this checkpoint shows a card for this death cause.</summary>
        public bool Allow(string checkpointNodeId, string cause) => shown.Add((checkpointNodeId ?? "") + "#" + (cause ?? ""));
    }
}
