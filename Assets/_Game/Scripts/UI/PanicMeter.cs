using SixtyOne.Building;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// HUD "Panik" meter under the building timer (fill anchored left, panic 0..1). The fill turns from amber to
    /// red as panic rises; a tick marks the click-delay threshold, and above it the bar pulses so the player sees
    /// why clicks lag. Reduce Effects (no delay) keeps it steady.
    /// </summary>
    public sealed class PanicMeter : MonoBehaviour
    {
        [Tooltip("Panic source (SurvivalController.Panic01).")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Fill rect; anchorMax.x = panic.")]
        [SerializeField] private RectTransform fill;

        [Tooltip("Fill image (recolored).")]
        [SerializeField] private Image fillImage;

        [Tooltip("Thin marker at the click-delay threshold (anchored by x).")]
        [SerializeField] private RectTransform thresholdTick;

        [Tooltip("Background image (flashes while above the threshold).")]
        [SerializeField] private Image background;

        [Tooltip("Fill color at low panic.")]
        [SerializeField] private Color calmColor = new Color(1f, 0.72f, 0.3f, 1f);

        [Tooltip("Fill color at full panic.")]
        [SerializeField] private Color panicColor = new Color(1f, 0.18f, 0.15f, 1f);

        [Tooltip("Pulses per second above the threshold.")]
        [SerializeField] private float pulseRate = 2.5f;

        [Tooltip("Seconds to ease the shown value toward the real panic.")]
        [SerializeField] private float smoothing = 0.15f;

        private Color backgroundColor;
        private float shown;
        private float velocity;

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null) backgroundColor = background.color;
            if (survival == null) survival = FindFirstObjectByType<SurvivalController>();
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || survival == null) return;
            if (PauseState.IsPaused) return;

            shown = Mathf.SmoothDamp(shown, Mathf.Clamp01(survival.Panic01), ref velocity, smoothing, Mathf.Infinity, Time.unscaledDeltaTime);
            float threshold = gm.Settings != null ? gm.Settings.PanicDelayThreshold : 0.7f;
            bool reduce = gm.Settings != null && gm.Settings.ReduceEffects;
            bool delaying = !reduce && shown > threshold;

            if (fill != null) fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
            if (thresholdTick != null)
            {
                thresholdTick.anchorMin = new Vector2(threshold, thresholdTick.anchorMin.y);
                thresholdTick.anchorMax = new Vector2(threshold, thresholdTick.anchorMax.y);
            }

            float wave = delaying ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseRate * 2f * Mathf.PI) : 0f;
            if (fillImage != null)
            {
                Color c = Color.Lerp(calmColor, panicColor, shown);
                fillImage.color = delaying ? Color.Lerp(c, Color.white, wave * 0.35f) : c;
            }

            if (background == null) return;
            if (!delaying)
            {
                background.color = backgroundColor;
                return;
            }

            Color flash = panicColor;
            flash.a = Mathf.Lerp(0.2f, 0.6f, wave);
            background.color = flash;
        }
    }
}
