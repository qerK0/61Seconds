using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>Pure survival math (time multipliers, stamina, poison, panic, input delay). Unit-tested.</summary>
    public static class SurvivalModel
    {
        /// <summary>Family members currently Injured (not yet treated).</summary>
        public static int InjuredCount(GameState state)
        {
            int count = 0;
            foreach (FamilyMemberState member in state.Members)
            {
                if (member.Health == MemberHealth.Injured) count++;
            }

            return count;
        }

        /// <summary>
        /// Multiplier on an action's time: moves get +extraPerInjured per injured member; everything is
        /// multiplied by exhaustedMultiplier while stamina is 0.
        /// </summary>
        public static float TimeMultiplier(int injuredMembers, bool isMove, float extraPerInjured, bool exhausted, float exhaustedMultiplier)
        {
            float m = 1f;
            if (isMove) m += Mathf.Max(0, injuredMembers) * Mathf.Max(0f, extraPerInjured);
            if (exhausted) m *= Mathf.Max(1f, exhaustedMultiplier);
            return m;
        }

        /// <summary>Stamina after draining for dt (clamped at 0).</summary>
        public static float DrainStamina(float stamina, float drainPerSecond, float dt)
        {
            return Mathf.Max(0f, stamina - Mathf.Max(0f, drainPerSecond) * dt);
        }

        /// <summary>Stamina after using an item (clamped at max).</summary>
        public static float RestoreStamina(float stamina, float amount, float max)
        {
            return Mathf.Clamp(stamina + Mathf.Max(0f, amount), 0f, max);
        }

        /// <summary>
        /// Poison after dt: rises at gainPerSecond in open gas (halved etc. by the wet cloth factor),
        /// otherwise decays. Clamped 0..1.
        /// </summary>
        public static float StepPoison(float poison, bool inOpenGas, float gainPerSecond, bool hasWetCloth, float wetClothFactor, float decayPerSecond, float dt)
        {
            float rate = inOpenGas ? gainPerSecond * (hasWetCloth ? wetClothFactor : 1f) : -decayPerSecond;
            return Mathf.Clamp01(poison + rate * dt);
        }

        /// <summary>Panic baseline: timer curve value plus poison contribution (0..1).</summary>
        public static float PanicBaseline(float timerPanic, float poison, float panicPerPoison)
        {
            return Mathf.Clamp01(timerPanic + poison * panicPerPoison);
        }

        /// <summary>Panic never drops below the baseline and decays toward it slowly when calm.</summary>
        public static float StepPanic(float panic, float baseline, float calmPerSecond, float dt)
        {
            float decayed = panic - Mathf.Max(0f, calmPerSecond) * dt;
            return Mathf.Clamp01(Mathf.Max(baseline, decayed));
        }

        /// <summary>
        /// Click delay: 0 at or below the threshold; above it lerp(0, maxDelay, inverseLerp(threshold, 1, panic)).
        /// Reduce Effects forces 0.
        /// </summary>
        public static float InputDelay(float panic, float threshold, float maxDelay, bool reduceEffects)
        {
            if (reduceEffects || panic <= threshold || threshold >= 1f) return 0f;
            float t = Mathf.Clamp01((panic - threshold) / (1f - threshold));
            return Mathf.Lerp(0f, Mathf.Max(0f, maxDelay), t);
        }
    }
}
