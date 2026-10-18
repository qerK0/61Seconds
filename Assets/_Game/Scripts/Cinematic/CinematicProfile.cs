using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// Look and camera behaviour for one act (Act 1 = 6.0, Act 2 = 7.0, Act 3 = 8.0). Every strength is
    /// tunable here; CinematicDirector reads it every frame and never writes back to it.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Cinematic Profile", fileName = "CinematicActN")]
    public sealed class CinematicProfile : ScriptableObject
    {
        [Header("Base look")]
        [Tooltip("Post exposure in EV. Slightly negative = darker image.")]
        [SerializeField] private float postExposure = -0.25f;
        [Tooltip("Contrast (-100..100).")]
        [Range(-100f, 100f)] [SerializeField] private float contrast = 15f;
        [Tooltip("Saturation (-100..100). -20 = muted.")]
        [Range(-100f, 100f)] [SerializeField] private float saturation = -20f;
        [Tooltip("Multiplied over the image. A cool tint reads as night/tension.")]
        [SerializeField] private Color colorFilter = new Color(0.88f, 0.94f, 1f, 1f);
        [Tooltip("Split toning: shadow color (cold blue).")]
        [SerializeField] private Color splitShadows = new Color(0.32f, 0.42f, 0.62f, 1f);
        [Tooltip("Split toning: highlight color (warm).")]
        [SerializeField] private Color splitHighlights = new Color(1f, 0.84f, 0.64f, 1f);
        [Tooltip("Split toning balance (-100 = more shadows tone, 100 = more highlights tone).")]
        [Range(-100f, 100f)] [SerializeField] private float splitBalance = -15f;
        [Tooltip("Vignette intensity at rest (act 1 at most 0.3; later acts darker).")]
        [Range(0f, 1f)] [SerializeField] private float vignette = 0.28f;
        [Tooltip("Vignette smoothness (higher = softer edge).")]
        [Range(0.01f, 1f)] [SerializeField] private float vignetteSmoothness = 0.45f;
        [Tooltip("Film grain (Thin1) intensity: subtle, it ties the pixel-art bag, the characters and the painted props together.")]
        [Range(0f, 1f)] [SerializeField] private float grainIntensity = 0.12f;
        [Tooltip("Film grain luminance response (higher = less grain in bright areas).")]
        [Range(0f, 1f)] [SerializeField] private float grainResponse = 0.8f;
        [Tooltip("URP Color Lookup strip (32 = 1024x32): the shared 'dust' grade under every act. Null = no LUT.")]
        [SerializeField] private Texture lut;
        [Tooltip("Color Lookup contribution (0 = off, 1 = full LUT).")]
        [Range(0f, 1f)] [SerializeField] private float lutContribution = 0.6f;
        [Tooltip("Bloom threshold (gamma). Above 1 only bright spots (lamps, flash) bloom.")]
        [SerializeField] private float bloomThreshold = 1.1f;
        [Tooltip("Bloom intensity (keep low).")]
        [SerializeField] private float bloomIntensity = 0.3f;
        [Tooltip("Bloom scatter.")]
        [Range(0f, 1f)] [SerializeField] private float bloomScatter = 0.6f;

        [Header("2D lights")]
        [Tooltip("Global Light2D intensity before the quake (1 = unlit default).")]
        [SerializeField] private float globalLightIntensity = 0.75f;
        [Tooltip("Global Light2D color.")]
        [SerializeField] private Color globalLightColor = new Color(0.8f, 0.85f, 1f, 1f);
        [Tooltip("Intensity of practical lights (lamps, windows) while power is on.")]
        [SerializeField] private float practicalLightIntensity = 1.1f;

        [Header("Building tension (timer -> 0)")]
        [Tooltip("Shape of the build-up over the building timer (x = 0 at start .. 1 at collapse, y = amount 0..1).")]
        [SerializeField] private AnimationCurve prepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Vignette when the building timer reaches zero.")]
        [Range(0f, 1f)] [SerializeField] private float prepVignetteEnd = 0.55f;
        [Tooltip("Saturation when the building timer reaches zero.")]
        [Range(-100f, 100f)] [SerializeField] private float prepSaturationEnd = -45f;
        [Tooltip("Slow push-in: percent the orthographic size shrinks by the time the building collapses.")]
        [Range(0f, 30f)] [SerializeField] private float prepZoomPercent = 8f;

        [Header("Placement moments")]
        [Tooltip("Letterbox aspect ratio (2.35 = cinemascope).")]
        [SerializeField] private float letterboxAspect = 2.35f;
        [Tooltip("Seconds for the letterbox bars to slide in/out.")]
        [SerializeField] private float letterboxSlideSeconds = 0.4f;
        [Tooltip("Extra vignette on each heartbeat (placement-timer seconds and building alarm ticks).")]
        [Range(0f, 0.3f)] [SerializeField] private float heartbeatVignette = 0.08f;
        [Tooltip("Seconds a heartbeat pulse takes to fade.")]
        [SerializeField] private float heartbeatSeconds = 0.35f;
        [Tooltip("Low-pass cutoff (Hz) on all audio during placement moments. 0 = off.")]
        [SerializeField] private float placementLowPassHz = 1800f;

        [Header("Quake")]
        [Tooltip("Exposure added by the white flash at quake start (EV).")]
        [SerializeField] private float flashExposure = 2.5f;
        [Tooltip("Flash length in seconds.")]
        [SerializeField] private float flashSeconds = 0.08f;
        [Tooltip("Chromatic aberration peak at quake start.")]
        [Range(0f, 1f)] [SerializeField] private float chromaticPeak = 0.8f;
        [Tooltip("Chromatic aberration held for the rest of the quake.")]
        [Range(0f, 1f)] [SerializeField] private float chromaticSettle = 0.1f;
        [Tooltip("Seconds from peak to settle.")]
        [SerializeField] private float chromaticPulseSeconds = 0.6f;
        [Tooltip("Lens distortion wobble amplitude (-1..1 range in URP).")]
        [Range(0f, 0.6f)] [SerializeField] private float lensWobbleAmplitude = 0.2f;
        [Tooltip("Lens distortion wobble frequency (Hz).")]
        [SerializeField] private float lensWobbleFrequency = 5f;
        [Tooltip("Light flickers before the power dies (min).")]
        [SerializeField] private int flickerCountMin = 3;
        [Tooltip("Light flickers before the power dies (max).")]
        [SerializeField] private int flickerCountMax = 5;
        [Tooltip("Main lights go off after the flicker (power cut).")]
        [SerializeField] private bool lightsOffAfterQuake = true;
        [Tooltip("If this flag is true the lights stay on after the quake (e.g. emergency lighting). Empty = never.")]
        [SerializeField] private string keepLightsFlag = "";
        [Tooltip("Rotational camera shake amplitude in degrees (on top of CameraShake).")]
        [SerializeField] private float rotationalShakeDegrees = 0.5f;
        [Tooltip("Dutch tilt held during the quake (degrees). Act 3 only by default.")]
        [SerializeField] private float dutchTiltDegrees = 0f;
        [Tooltip("Dust particles emitted at quake start.")]
        [SerializeField] private int dustBurstCount = 80;

        [Header("Post-quake (power out)")]
        [Tooltip("Exposure offset after the quake (EV, negative = darker than prep).")]
        [SerializeField] private float postQuakeExposureOffset = -0.4f;
        [Tooltip("Saturation after the quake.")]
        [Range(-100f, 100f)] [SerializeField] private float postQuakeSaturation = -45f;
        [Tooltip("Vignette after the quake.")]
        [Range(0f, 1f)] [SerializeField] private float postQuakeVignette = 0.5f;
        [Tooltip("Global Light2D intensity after the power cut.")]
        [SerializeField] private float postQuakeGlobalLight = 0.3f;
        [Tooltip("Item id that turns on the player's flashlight after the quake.")]
        [SerializeField] private string flashlightItemId = "flashlight";
        [Tooltip("Flashlight Light2D intensity.")]
        [SerializeField] private float flashlightIntensity = 1.2f;
        [Tooltip("Flashlight radius (world units).")]
        [SerializeField] private float flashlightRadius = 2.4f;

        [Header("Poison (gas)")]
        [Tooltip("Exposure added at full poison (EV, negative = darker).")]
        [SerializeField] private float poisonExposure = -0.9f;
        [Tooltip("Saturation added at full poison.")]
        [SerializeField] private float poisonSaturation = -35f;
        [Tooltip("Vignette added at full poison.")]
        [Range(0f, 0.5f)] [SerializeField] private float poisonVignette = 0.25f;

        [Header("Death")]
        [Tooltip("Seconds to blend to near black and white before the death card.")]
        [SerializeField] private float deathBlendSeconds = 0.5f;
        [Tooltip("Saturation at death.")]
        [Range(-100f, 100f)] [SerializeField] private float deathSaturation = -100f;
        [Tooltip("Contrast at death.")]
        [Range(-100f, 100f)] [SerializeField] private float deathContrast = 40f;
        [Tooltip("Vignette at death.")]
        [Range(0f, 1f)] [SerializeField] private float deathVignette = 0.6f;

        [Header("Rewind")]
        [Tooltip("Chromatic aberration spike during the rewind.")]
        [Range(0f, 1f)] [SerializeField] private float rewindChromatic = 0.7f;
        [Tooltip("Film grain spike during the rewind.")]
        [Range(0f, 1f)] [SerializeField] private float rewindGrain = 0.9f;
        [Tooltip("Seconds the rewind spike lasts (match RewindFX).")]
        [SerializeField] private float rewindSeconds = 0.9f;

        [Header("Win / exit building")]
        [Tooltip("Color filter when the family gets out.")]
        [SerializeField] private Color winColorFilter = new Color(1f, 0.95f, 0.85f, 1f);
        [Tooltip("Saturation when the family gets out.")]
        [Range(-100f, 100f)] [SerializeField] private float winSaturation = -5f;
        [Tooltip("Vignette when the family gets out.")]
        [Range(0f, 1f)] [SerializeField] private float winVignette = 0.2f;
        [Tooltip("Seconds to blend to the win look.")]
        [SerializeField] private float winBlendSeconds = 2f;

        public float PostExposure => postExposure;
        public float Contrast => contrast;
        public float Saturation => saturation;
        public Color ColorFilter => colorFilter;
        public Color SplitShadows => splitShadows;
        public Color SplitHighlights => splitHighlights;
        public float SplitBalance => splitBalance;
        public float Vignette => vignette;
        public float VignetteSmoothness => vignetteSmoothness;
        public float GrainIntensity => grainIntensity;
        public float GrainResponse => grainResponse;
        public Texture Lut => lut;
        public float LutContribution => lutContribution;
        public float BloomThreshold => bloomThreshold;
        public float BloomIntensity => bloomIntensity;
        public float BloomScatter => bloomScatter;
        public float GlobalLightIntensity => globalLightIntensity;
        public Color GlobalLightColor => globalLightColor;
        public float PracticalLightIntensity => practicalLightIntensity;
        public float PrepVignetteEnd => prepVignetteEnd;
        public float PrepSaturationEnd => prepSaturationEnd;
        public float PrepZoomPercent => prepZoomPercent;
        public float LetterboxAspect => letterboxAspect;
        public float LetterboxSlideSeconds => letterboxSlideSeconds;
        public float HeartbeatVignette => heartbeatVignette;
        public float HeartbeatSeconds => heartbeatSeconds;
        public float PlacementLowPassHz => placementLowPassHz;
        public float FlashExposure => flashExposure;
        public float FlashSeconds => flashSeconds;
        public float ChromaticPeak => chromaticPeak;
        public float ChromaticSettle => chromaticSettle;
        public float ChromaticPulseSeconds => chromaticPulseSeconds;
        public float LensWobbleAmplitude => lensWobbleAmplitude;
        public float LensWobbleFrequency => lensWobbleFrequency;
        public int FlickerCountMin => flickerCountMin;
        public int FlickerCountMax => flickerCountMax;
        public bool LightsOffAfterQuake => lightsOffAfterQuake;
        public string KeepLightsFlag => keepLightsFlag;
        public float RotationalShakeDegrees => rotationalShakeDegrees;
        public float DutchTiltDegrees => dutchTiltDegrees;
        public int DustBurstCount => dustBurstCount;
        public float PostQuakeExposureOffset => postQuakeExposureOffset;
        public float PostQuakeSaturation => postQuakeSaturation;
        public float PostQuakeVignette => postQuakeVignette;
        public float PostQuakeGlobalLight => postQuakeGlobalLight;
        public string FlashlightItemId => flashlightItemId;
        public float FlashlightIntensity => flashlightIntensity;
        public float FlashlightRadius => flashlightRadius;
        public float PoisonExposure => poisonExposure;
        public float PoisonSaturation => poisonSaturation;
        public float PoisonVignette => poisonVignette;
        public float DeathBlendSeconds => deathBlendSeconds;
        public float DeathSaturation => deathSaturation;
        public float DeathContrast => deathContrast;
        public float DeathVignette => deathVignette;
        public float RewindChromatic => rewindChromatic;
        public float RewindGrain => rewindGrain;
        public float RewindSeconds => rewindSeconds;
        public Color WinColorFilter => winColorFilter;
        public float WinSaturation => winSaturation;
        public float WinVignette => winVignette;
        public float WinBlendSeconds => winBlendSeconds;

        /// <summary>Prep build-up amount (0..1) for a countdown progress (0..1).</summary>
        public float EvaluatePrep(float progress)
        {
            float x = Mathf.Clamp01(progress);
            return prepCurve != null && prepCurve.length > 0 ? Mathf.Clamp01(prepCurve.Evaluate(x)) : x;
        }

        /// <summary>
        /// Starting values for an act (1-3): each act is darker, more desaturated and harsher than the last.
        /// Used by the profile generator; tune the assets afterwards.
        /// </summary>
        /// <summary>Sets the shared Color Lookup (generator).</summary>
        public void ConfigureLut(Texture strip, float contribution)
        {
            lut = strip;
            lutContribution = Mathf.Clamp01(contribution);
        }

        public void ApplyActDefaults(int act)
        {
            int a = Mathf.Clamp(act, 1, 3) - 1;
            postExposure = -0.25f - 0.2f * a;
            contrast = 15f + 8f * a;
            saturation = -20f - 8f * a;
            colorFilter = new Color(0.88f - 0.04f * a, 0.94f - 0.03f * a, 1f, 1f);
            vignette = 0.28f + 0.05f * a;
            grainIntensity = 0.12f + 0.02f * a;
            globalLightIntensity = 0.75f - 0.12f * a;
            prepVignetteEnd = 0.55f + 0.05f * a;
            prepSaturationEnd = -45f - 8f * a;
            postQuakeSaturation = prepSaturationEnd;
            postQuakeVignette = Mathf.Max(0.5f, prepVignetteEnd - 0.05f);
            postQuakeGlobalLight = 0.3f - 0.06f * a;
            rotationalShakeDegrees = 0.5f + 0.35f * a;
            chromaticSettle = 0.1f + 0.05f * a;
            lensWobbleAmplitude = 0.2f + 0.06f * a;
            dustBurstCount = 80 + 60 * a;
            dutchTiltDegrees = a == 2 ? 2.5f : 0f;
        }
    }
}
