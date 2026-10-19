using System;

namespace SixtyOne.Placement
{
    /// <summary>Which part of a room a transition piece belongs to (sets its in-phase order).</summary>
    public enum TransitionLayer
    {
        Floor,
        Walls,
        Props
    }

    /// <summary>Timing for one room transition (from TransitionConfig.Timing).</summary>
    public struct TransitionTiming
    {
        public float OutDuration;
        public float InDuration;
        public float Rise;
        public float Drop;
        public float Stagger;
        public float WallLag;
        public float PropLag;
        public float Bounce;
        public float CharacterPop;
    }

    /// <summary>
    /// Pure schedule for the tile transition (unit-tested). Depth runs from the far corner (0, top of the iso
    /// room) to the nearest point (1). Out: every piece starts after depth x stagger and all pieces finish exactly
    /// at OutDuration. In: floor, then walls (+WallLag), then props (+PropLag), each wave-staggered by depth; all
    /// pieces have landed exactly at InDuration, then the family pops in for CharacterPop seconds.
    /// </summary>
    public static class TileTransitionPlan
    {
        private const float MinPieceSeconds = 0.05f;

        /// <summary>0 for the far corner (highest local y), 1 for the nearest point; 0 when the room is flat.</summary>
        public static float Depth01(float localY, float minY, float maxY)
        {
            float range = maxY - minY;
            if (range <= 1e-5f) return 0f;
            return Clamp01((maxY - localY) / range);
        }

        public static float OutDelay(float depth01, TransitionTiming t) => Clamp01(depth01) * Wave(t.OutDuration, t.Stagger);

        /// <summary>Seconds each piece takes to rise (so the nearest piece ends exactly at OutDuration).</summary>
        public static float OutPieceDuration(TransitionTiming t) => Math.Max(MinPieceSeconds, t.OutDuration - Wave(t.OutDuration, t.Stagger));

        public static float LayerLag(TransitionLayer layer, TransitionTiming t)
        {
            switch (layer)
            {
                case TransitionLayer.Walls: return Math.Max(0f, t.WallLag);
                case TransitionLayer.Props: return Math.Max(0f, t.PropLag);
                default: return 0f;
            }
        }

        public static float InDelay(TransitionLayer layer, float depth01, TransitionTiming t)
        {
            return Math.Min(LayerLag(layer, t), MaxInLag(t)) + Clamp01(depth01) * InWave(t);
        }

        /// <summary>Seconds each piece takes to drop (so the last prop ends exactly at InDuration).</summary>
        public static float InPieceDuration(TransitionTiming t) => Math.Max(MinPieceSeconds, t.InDuration - MaxInLag(t) - InWave(t));

        /// <summary>Out-phase length (old room gone).</summary>
        public static float OutTotal(TransitionTiming t) => OutDelay(1f, t) + OutPieceDuration(t);

        /// <summary>In-phase length including the family pop.</summary>
        public static float InTotal(TransitionTiming t) => InDelay(TransitionLayer.Props, 1f, t) + InPieceDuration(t) + Math.Max(0f, t.CharacterPop);

        /// <summary>Whole transition (out + in + pop).</summary>
        public static float Total(TransitionTiming t) => OutTotal(t) + InTotal(t);

        /// <summary>0..1 progress of one piece at a time since the phase started.</summary>
        public static float Progress(float time, float delay, float duration)
        {
            if (duration <= 0f) return time >= delay ? 1f : 0f;
            return Clamp01((time - delay) / duration);
        }

        /// <summary>Out easing: slow start, fast exit.</summary>
        public static float EaseIn(float k)
        {
            k = Clamp01(k);
            return k * k;
        }

        /// <summary>
        /// In height as a fraction of the drop: 1 at the start, 0 at the end, briefly below 0 (overshoot) when bounce
        /// &gt; 0 (ease-out-back; bounce 0.1 dips about 10%).
        /// </summary>
        public static float DropOffset01(float k, float bounce)
        {
            k = Clamp01(k);
            float c1 = Math.Max(0f, bounce) * 17.0158f;
            float c3 = c1 + 1f;
            float u = k - 1f;
            float eased = 1f + c3 * u * u * u + c1 * u * u;
            return 1f - eased;
        }

        /// <summary>Family pop scale: 0.6 → slightly over 1 → 1.</summary>
        public static float PopScale(float k) => 0.6f + 0.4f * (1f - DropOffset01(k, 0.15f));

        // The wave may never eat the whole phase: pieces always get at least MinPieceSeconds.
        private static float Wave(float duration, float stagger) => Math.Max(0f, Math.Min(stagger, duration - MinPieceSeconds));

        private static float MaxInLag(TransitionTiming t) => Math.Max(0f, Math.Min(Math.Max(t.WallLag, t.PropLag), (t.InDuration - MinPieceSeconds) * 0.5f));

        private static float InWave(TransitionTiming t) => Math.Max(0f, Math.Min(t.Stagger, t.InDuration - MaxInLag(t) - MinPieceSeconds));

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
