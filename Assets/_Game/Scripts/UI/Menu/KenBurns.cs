using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Slow Ken Burns push on a full-screen image: scale and a small pan, ping-pong (unscaled time).</summary>
    public sealed class KenBurns : MonoBehaviour
    {
        [Tooltip("Scale at the start and the end of the move.")]
        [SerializeField] private Vector2 scaleRange = new Vector2(1f, 1.06f);

        [Tooltip("Pan at the end of the move (canvas pixels, from the start position).")]
        [SerializeField] private Vector2 pan = new Vector2(-40f, 14f);

        [Tooltip("Seconds for one way; then it eases back.")]
        [SerializeField] private float seconds = 40f;

        private RectTransform rect;
        private Vector2 origin;

        public void Configure(Vector2 scales, Vector2 panPixels, float oneWaySeconds)
        {
            scaleRange = scales;
            pan = panPixels;
            seconds = oneWaySeconds;
        }

        private void Awake()
        {
            rect = (RectTransform)transform;
            origin = rect.anchoredPosition;
        }

        private void Update()
        {
            float u = Mathf.PingPong(Time.unscaledTime / Mathf.Max(1f, seconds), 1f);
            u = u * u * (3f - 2f * u);
            float s = Mathf.Lerp(scaleRange.x, scaleRange.y, u);
            rect.localScale = new Vector3(s, s, 1f);
            rect.anchoredPosition = origin + pan * u;
        }
    }
}
