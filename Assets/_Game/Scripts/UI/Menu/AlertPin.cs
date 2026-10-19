using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>A pulsing red alert pin over the map: the dot breathes and a ring expands and fades (staggered by phase).</summary>
    public sealed class AlertPin : MonoBehaviour
    {
        [SerializeField] private Image dot;
        [SerializeField] private Image ring;

        [Tooltip("Seconds per pulse.")]
        [SerializeField] private float period = 2.2f;

        [Tooltip("Start offset (0..1 of a pulse).")]
        [SerializeField] private float phase;

        [Tooltip("Ring scale at the end of a pulse.")]
        [SerializeField] private float ringScale = 3.2f;

        public void Configure(Image dotImage, Image ringImage, float seconds, float offset)
        {
            dot = dotImage;
            ring = ringImage;
            period = seconds;
            phase = offset;
        }

        private void Update()
        {
            float u = Mathf.Repeat(Time.unscaledTime / Mathf.Max(0.1f, period) + phase, 1f);
            if (ring != null)
            {
                float s = Mathf.Lerp(1f, ringScale, 1f - (1f - u) * (1f - u));
                ring.rectTransform.localScale = new Vector3(s, s, 1f);
                Color c = ring.color;
                c.a = (1f - u) * 0.85f;
                ring.color = c;
            }

            if (dot != null)
            {
                Color c = dot.color;
                c.a = 0.75f + 0.25f * Mathf.Cos(u * Mathf.PI * 2f);
                dot.color = c;
            }
        }
    }
}
