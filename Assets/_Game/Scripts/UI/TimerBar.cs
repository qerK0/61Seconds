using SixtyOne.Nodes;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Shows the current node timer as a shrinking bar.</summary>
    public sealed class TimerBar : MonoBehaviour
    {
        [Tooltip("Source of the timer.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Shown only while a timer runs.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Fill rect; its anchorMax.x is set to the remaining fraction.")]
        [SerializeField] private RectTransform fill;

        [Tooltip("Fill image (recolored when time is short).")]
        [SerializeField] private Image fillImage;

        [Tooltip("Color with plenty of time.")]
        [SerializeField] private Color normalColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Tooltip("Color in the last 30%.")]
        [SerializeField] private Color urgentColor = new Color(1f, 0.2f, 0.15f, 1f);

        private void Update()
        {
            bool running = nodePlayer != null && nodePlayer.Timer.Running;
            UIVisibility.Set(group, running, blocksWhenVisible: false);
            if (!running) return;

            float t = nodePlayer.Timer.Normalized;
            if (fill != null) fill.anchorMax = new Vector2(t, fill.anchorMax.y);
            if (fillImage != null) fillImage.color = t < 0.3f ? urgentColor : normalColor;
        }
    }
}
