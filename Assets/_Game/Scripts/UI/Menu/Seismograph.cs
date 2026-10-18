using SixtyOne.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// A scrolling seismograph trace drawn as a UI polyline: low noise that scrolls left, and every few seconds a
    /// spike (a short burst of big swings) with a soft thud and a screen micro-shake. Unscaled time.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class Seismograph : MaskableGraphic
    {
        [Tooltip("Points across the width.")]
        [SerializeField] private int samples = 160;

        [Tooltip("Line thickness (canvas pixels).")]
        [SerializeField] private float thickness = 2.5f;

        [Tooltip("Samples pushed per second (scroll speed).")]
        [SerializeField] private float samplesPerSecond = 45f;

        [Tooltip("Idle noise amplitude (0..1 of the half height).")]
        [SerializeField] private float noise = 0.08f;

        [Tooltip("Seconds between spikes (random in range).")]
        [SerializeField] private Vector2 spikeEvery = new Vector2(4f, 7f);

        [Tooltip("Plays on every spike.")]
        [SerializeField] private AudioCue thudCue;

        private float[] values;
        private int head;
        private float carry;
        private float nextSpike;
        private float spikeLeft;

        public void Configure(AudioCue thud, Color lineColor)
        {
            thudCue = thud;
            color = lineColor;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            values = new float[Mathf.Max(8, samples)];
            nextSpike = Time.unscaledTime + Random.Range(spikeEvery.x, spikeEvery.y);
        }

        private void Update()
        {
            if (values == null) return;
            if (Time.unscaledTime >= nextSpike)
            {
                nextSpike = Time.unscaledTime + Random.Range(spikeEvery.x, spikeEvery.y);
                spikeLeft = 0.45f;
                AudioService.PlayUiCue(thudCue);
                UiShake.Kick(0.35f, 0.25f);
            }

            carry += Time.unscaledDeltaTime * samplesPerSecond;
            bool changed = false;
            while (carry >= 1f)
            {
                carry -= 1f;
                float v = (Mathf.PerlinNoise(Time.unscaledTime * 3.1f, head * 0.17f) - 0.5f) * 2f * noise;
                v += Random.Range(-0.3f, 0.3f) * noise;
                if (spikeLeft > 0f)
                {
                    spikeLeft -= 1f / samplesPerSecond;
                    float k = Mathf.Clamp01(spikeLeft / 0.45f);
                    v += Random.Range(-1f, 1f) * (0.25f + 0.75f * k);
                }

                values[head] = Mathf.Clamp(v, -1f, 1f);
                head = (head + 1) % values.Length;
                changed = true;
            }

            if (changed) SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (values == null || values.Length < 2) return;
            Rect r = rectTransform.rect;
            int n = values.Length;
            Vector2 previous = Point(r, 0, n);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Point(r, i, n);
                Vector2 d = (p - previous).normalized;
                Vector2 normal = new Vector2(-d.y, d.x) * thickness * 0.5f;
                // Fade in from the left edge, full on the right (the newest samples).
                float a = Mathf.Clamp01(i / (n * 0.25f));
                Color32 c = new Color(color.r, color.g, color.b, color.a * a);
                int start = vh.currentVertCount;
                vh.AddVert(previous - normal, c, Vector2.zero);
                vh.AddVert(previous + normal, c, Vector2.zero);
                vh.AddVert(p + normal, c, Vector2.zero);
                vh.AddVert(p - normal, c, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
                previous = p;
            }
        }

        /// <summary>The i-th oldest sample as a point in the rect (oldest at the left).</summary>
        private Vector2 Point(Rect r, int i, int n)
        {
            float v = values[(head + i) % n];
            return new Vector2(Mathf.Lerp(r.xMin, r.xMax, i / (float)(n - 1)), r.center.y + v * r.height * 0.5f);
        }
    }
}
