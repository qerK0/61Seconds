using SixtyOne.Building;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>HUD stamina bar (fill anchored left). Turns red when exhausted.</summary>
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

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || survival == null || survival.Config == null) return;
            float t = Mathf.Clamp01(gm.State.Stamina / Mathf.Max(1f, survival.Config.StaminaMax));
            if (fill != null) fill.anchorMax = new Vector2(t, fill.anchorMax.y);
            if (fillImage != null) fillImage.color = t <= 0.001f ? emptyColor : t < 0.25f ? lowColor : normalColor;
        }
    }
}
