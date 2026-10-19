using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// Black bars that crop the view to a cinematic aspect (2.35:1). Place them first in the canvas so
    /// gameplay UI draws on top and stays readable. Slides with unscaled time.
    /// </summary>
    public sealed class LetterboxBars : MonoBehaviour
    {
        [Tooltip("Top bar (anchored to the top edge).")]
        [SerializeField] private RectTransform top;

        [Tooltip("Bottom bar (anchored to the bottom edge).")]
        [SerializeField] private RectTransform bottom;

        private float shown;
        private float target;
        private float aspect = 2.35f;
        private float slideSeconds = 0.4f;

        public bool IsShown => target > 0.5f;

        /// <summary>Slides the bars in or out.</summary>
        public void Show(bool on, float targetAspect, float seconds)
        {
            target = on ? 1f : 0f;
            aspect = targetAspect;
            slideSeconds = seconds;
        }

        private void Update()
        {
            shown = CinematicMixer.Approach(shown, target, slideSeconds, Time.unscaledDeltaTime);
            float screenAspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float bar = aspect > 0f ? Mathf.Clamp01((1f - screenAspect / aspect) * 0.5f) : 0f;
            float h = bar * Easing(shown);
            if (top != null)
            {
                top.anchorMin = new Vector2(0f, 1f - h);
                top.anchorMax = Vector2.one;
            }

            if (bottom != null)
            {
                bottom.anchorMin = Vector2.zero;
                bottom.anchorMax = new Vector2(1f, h);
            }
        }

        private static float Easing(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
