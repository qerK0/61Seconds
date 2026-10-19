using SixtyOne.Building;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// HUD stamina bar (fill anchored left). Turns red when low and pulses red at 0, so the player knows why
    /// the screen is darkening (exhaustion). Reduce Effects keeps it solid red.
    /// </summary>
    public sealed class StaminaBar : MonoBehaviour
    {
        [Tooltip("Max stamina source.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Fill rect; anchorMax.x = stamina fraction.")]
        [SerializeField] private RectTransform fill;

        [Tooltip("Fill image (recolored).")]
        [SerializeField] private Image fillImage;

        [Tooltip("Color with stamina left.")]
        [SerializeField] private Color normalColor = new Color(0.4f, 0.85f, 0.45f, 1f);

        [Tooltip("Color below 25%.")]
        [SerializeField] private Color lowColor = new Color(1f, 0.6f, 0.2f, 1f);

        [Tooltip("Color at 0 (heavy actions locked).")]
        [SerializeField] private Color emptyColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("Background image (flashes red at 0 stamina).")]
        [SerializeField] private Image background;

        [Tooltip("Pulses per second at 0 stamina.")]
        [SerializeField] private float emptyPulseRate = 2f;

        private Color backgroundColor;

        private void Awake()
        {
            if (background == null) background = GetComponent<Image>();
            if (background != null) backgroundColor = background.color;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || survival == null || survival.Config == null) return;
            float t = Mathf.Clamp01(gm.State.Stamina / Mathf.Max(1f, survival.Config.StaminaMax));
            if (fill != null) fill.anchorMax = new Vector2(t, fill.anchorMax.y);
            if (fillImage != null) fillImage.color = t <= 0.001f ? emptyColor : t < 0.25f ? lowColor : normalColor;
            if (background == null) return;
            bool reduce = gm.Settings != null && gm.Settings.ReduceEffects;
            if (t > 0.001f)
            {
                background.color = backgroundColor;
                return;
            }

            float wave = reduce ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * emptyPulseRate * 2f * Mathf.PI);
            Color flash = emptyColor;
            flash.a = Mathf.Lerp(0.25f, 0.85f, wave);
            background.color = flash;
        }
    }
}
