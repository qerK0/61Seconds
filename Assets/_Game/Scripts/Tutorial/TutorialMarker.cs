using UnityEngine;

namespace SixtyOne.Tutorial
{
    /// <summary>The glowing floor marker of the walking step: pulses its sprite's alpha and scale (unscaled time).</summary>
    public sealed class TutorialMarker : MonoBehaviour
    {
        [Tooltip("Marker sprite (a floor diamond).")]
        [SerializeField] private SpriteRenderer sprite;

        [Tooltip("Pulses per second.")]
        [SerializeField] private float speed = 1.6f;

        [Tooltip("Alpha range of the pulse.")]
        [SerializeField] private Vector2 alpha = new Vector2(0.35f, 0.85f);

        [Tooltip("Scale range of the pulse.")]
        [SerializeField] private Vector2 scale = new Vector2(0.85f, 1f);

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float k = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(scale.x, scale.y, k);
            if (sprite == null) return;
            Color c = sprite.color;
            c.a = Mathf.Lerp(alpha.x, alpha.y, k);
            sprite.color = c;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(SpriteRenderer renderer) => sprite = renderer;
    }
}
