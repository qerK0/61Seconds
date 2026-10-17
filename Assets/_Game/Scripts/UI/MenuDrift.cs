using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Slow, subtle drift + breathing zoom of an oversized background image (unscaled time).</summary>
    public sealed class MenuDrift : MonoBehaviour
    {
        [Tooltip("Drift amplitude in canvas pixels.")]
        [SerializeField] private Vector2 amplitude = new Vector2(40f, 18f);

        [Tooltip("Seconds for one full drift cycle.")]
        [SerializeField] private float period = 40f;

        [Tooltip("Extra zoom at the peak of the breath.")]
        [SerializeField] private float zoom = 0.03f;

        private RectTransform rect;
        private Vector2 origin;

        private void Awake()
        {
            rect = (RectTransform)transform;
            origin = rect.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(1f, period);
            rect.anchoredPosition = origin + new Vector2(Mathf.Sin(t) * amplitude.x, Mathf.Sin(t * 0.7f + 1.3f) * amplitude.y);
            float s = 1.08f + zoom * (Mathf.Sin(t * 0.5f) + 1f) * 0.5f;
            rect.localScale = new Vector3(s, s, 1f);
        }
    }
}
