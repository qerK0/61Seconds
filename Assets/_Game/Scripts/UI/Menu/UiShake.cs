using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Micro-shake of a UI root (the menu / end screen content), kicked from anywhere with UiShake.Kick. Decays fast,
    /// unscaled time; Reduce Effects turns it off.
    /// </summary>
    public sealed class UiShake : MonoBehaviour
    {
        private static UiShake active;

        [Tooltip("Pixels at strength 1.")]
        [SerializeField] private float pixels = 10f;

        private RectTransform rect;
        private Vector2 origin;
        private float strength;
        private float duration;
        private float left;

        /// <summary>Shakes the active root (strength 0..1, seconds); a weaker kick never cuts a stronger one short.</summary>
        public static void Kick(float strength, float seconds)
        {
            if (active == null || UserSettings.ReduceEffects) return;
            float current = active.left > 0f ? active.strength * active.left / active.duration : 0f;
            if (strength < current) return;
            active.strength = strength;
            active.duration = Mathf.Max(0.01f, seconds);
            active.left = active.duration;
        }

        private void OnEnable()
        {
            rect = (RectTransform)transform;
            origin = rect.anchoredPosition;
            active = this;
        }

        private void OnDisable()
        {
            if (active == this) active = null;
            if (rect != null) rect.anchoredPosition = origin;
        }

        private void Update()
        {
            if (left <= 0f) return;
            left -= Time.unscaledDeltaTime;
            if (left <= 0f || UserSettings.ReduceEffects)
            {
                left = 0f;
                rect.anchoredPosition = origin;
                return;
            }

            float k = strength * pixels * (left / duration);
            rect.anchoredPosition = origin + new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * k;
        }
    }
}
