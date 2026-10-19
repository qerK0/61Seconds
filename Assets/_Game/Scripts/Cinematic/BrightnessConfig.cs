using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// The one place for the overall brightness of the game: an exposure lift on top of every CinematicProfile, a
    /// vignette scale, a slight shadow / midtone lift (Shadows Midtones Highlights: scales the darks, black stays
    /// black, so nothing washes out), a dust-LUT scale and the range of the
    /// player's "Parlaklık" slider. Loaded from Resources (BrightnessConfig) by CinematicDirector, so no scene needs
    /// rewiring; without the asset the defaults below apply.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Brightness Config", fileName = "BrightnessConfig")]
    public sealed class BrightnessConfig : ScriptableObject
    {
        /// <summary>Resources path (Data/Cinematic/Resources/BrightnessConfig.asset).</summary>
        public const string ResourcePath = "BrightnessConfig";

        [Tooltip("Post exposure (EV) added on top of every act profile. +0.35 = a notch brighter, still gloomy.")]
        [SerializeField] private float exposureBoost = 0.35f;

        [Tooltip("Multiplies every vignette value (rest, tension, poison, death). 0.7 = 30% lighter corners.")]
        [Range(0f, 1.5f)] [SerializeField] private float vignetteScale = 0.7f;

        [Tooltip("Shadows Midtones Highlights: shadows offset (w). Brightens the dark tones (black stays black); 0 = neutral.")]
        [Range(-0.5f, 0.5f)] [SerializeField] private float shadowLift = 0.15f;

        [Tooltip("Shadows Midtones Highlights: midtones offset (w). Opens the midtones a little; 0 = neutral.")]
        [Range(-0.5f, 0.5f)] [SerializeField] private float midtoneLift = 0.05f;

        [Tooltip("Multiplies the dust LUT contribution of every profile (it darkens and greys the image). 1 = as authored.")]
        [Range(0f, 1f)] [SerializeField] private float lutScale = 0.85f;

        [Tooltip("EV the Parlaklık slider adds at its ends (slider 0 = -range, 0.5 = the values above, 1 = +range).")]
        [Range(0f, 2f)] [SerializeField] private float userRange = 0.6f;

        private static BrightnessConfig current;

        public float ExposureBoost => exposureBoost;
        public float VignetteScale => vignetteScale;
        public float ShadowLift => shadowLift;
        public float MidtoneLift => midtoneLift;
        public float LutScale => lutScale;
        public float UserRange => userRange;

        /// <summary>The asset from Resources, else a default instance.</summary>
        public static BrightnessConfig Current
        {
            get
            {
                if (current == null) current = Resources.Load<BrightnessConfig>(ResourcePath);
                if (current == null)
                {
                    current = CreateInstance<BrightnessConfig>();
                    current.hideFlags = HideFlags.DontSave;
                }

                return current;
            }
        }

        /// <summary>Exposure offset (EV) for a Parlaklık slider value (0..1, 0.5 = default).</summary>
        public float ExposureFor(float user01) => exposureBoost + UserOffset(user01, userRange);

        /// <summary>Pure: the slider's EV offset (0.5 = 0, ends = ±range).</summary>
        public static float UserOffset(float user01, float range) => (Mathf.Clamp01(user01) - 0.5f) * 2f * range;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
        }
    }
}
