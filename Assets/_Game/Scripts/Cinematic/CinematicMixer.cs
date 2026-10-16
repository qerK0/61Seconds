using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>Layered cinematic state, each 0..1, produced by CinematicDirector from GameEvents.</summary>
    public struct CinematicState
    {
        /// <summary>Countdown progress (0 at prep start .. 1 at zero). 0 when no countdown runs.</summary>
        public float Prep;
        public float Heartbeat;
        public float PostQuake;
        public float Death;
        public float Rewind;
        public float Win;
        public float Flash;
        /// <summary>Current quake chromatic aberration (already shaped by the pulse).</summary>
        public float Chromatic;
        /// <summary>Current lens distortion (already signed and scaled).</summary>
        public float LensDistortion;
        public bool ReduceEffects;
    }

    /// <summary>Final values for the volume and camera this frame.</summary>
    public struct CinematicFrame
    {
        public float PostExposure;
        public float Contrast;
        public float Saturation;
        public Color ColorFilter;
        public float Vignette;
        public float Grain;
        public float Chromatic;
        public float LensDistortion;
        /// <summary>Fraction the orthographic size shrinks (0.08 = 8% push-in).</summary>
        public float Zoom;
    }

    /// <summary>Pure blend of a profile and a state into frame values (unit-testable).</summary>
    public static class CinematicMixer
    {
        public static CinematicFrame Mix(CinematicProfile p, CinematicState s)
        {
            float prep = p.EvaluatePrep(s.Prep) * (1f - s.PostQuake);
            var f = new CinematicFrame();

            float flash = s.ReduceEffects ? 0f : s.Flash;
            f.PostExposure = p.PostExposure + p.PostQuakeExposureOffset * s.PostQuake + p.FlashExposure * flash;

            float sat = Mathf.Lerp(p.Saturation, p.PrepSaturationEnd, prep);
            sat = Mathf.Lerp(sat, p.PostQuakeSaturation, s.PostQuake);
            sat = Mathf.Lerp(sat, p.WinSaturation, s.Win);
            f.Saturation = Mathf.Lerp(sat, p.DeathSaturation, s.Death);

            float vig = Mathf.Lerp(p.Vignette, p.PrepVignetteEnd, prep);
            vig = Mathf.Lerp(vig, p.PostQuakeVignette, s.PostQuake);
            vig += p.HeartbeatVignette * s.Heartbeat;
            vig = Mathf.Lerp(vig, p.WinVignette, s.Win);
            f.Vignette = Mathf.Clamp01(Mathf.Lerp(vig, p.DeathVignette, s.Death));

            f.Contrast = Mathf.Lerp(p.Contrast, p.DeathContrast, s.Death);
            f.ColorFilter = Color.Lerp(p.ColorFilter, p.WinColorFilter, s.Win);
            f.Grain = Mathf.Clamp01(Mathf.Lerp(p.GrainIntensity, p.RewindGrain, s.Rewind));
            f.Chromatic = s.ReduceEffects ? 0f : Mathf.Clamp01(Mathf.Max(s.Chromatic, p.RewindChromatic * s.Rewind));
            f.LensDistortion = s.ReduceEffects ? 0f : Mathf.Clamp(s.LensDistortion, -1f, 1f);
            f.Zoom = p.PrepZoomPercent * 0.01f * prep;
            return f;
        }

        /// <summary>Moves value toward target at a rate that covers 0..1 in seconds (instant when seconds &lt;= 0).</summary>
        public static float Approach(float value, float target, float seconds, float deltaTime)
        {
            if (seconds <= 0f) return target;
            float step = deltaTime / seconds;
            return value < target ? Mathf.Min(target, value + step) : Mathf.Max(target, value - step);
        }
    }
}
