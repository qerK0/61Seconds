using System.Collections.Generic;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Drifting smoke / ash as UI images (no particle system on an overlay canvas): soft sprites that drift slowly
    /// sideways and up/down, wrap around the rect and fade near its edges. Reduce Effects keeps them, at lower density.
    /// </summary>
    public sealed class UiParticles : MonoBehaviour
    {
        [Tooltip("Soft round sprite.")]
        [SerializeField] private Sprite sprite;

        [SerializeField] private int count = 36;

        [Tooltip("Size range in pixels.")]
        [SerializeField] private Vector2 size = new Vector2(6f, 22f);

        [Tooltip("Drift velocity range (pixels per second); x and y are each picked in range.")]
        [SerializeField] private Vector2 velocityX = new Vector2(-30f, -8f);

        [SerializeField] private Vector2 velocityY = new Vector2(-6f, 10f);

        [SerializeField] private Color tint = new Color(0.85f, 0.82f, 0.8f, 0.18f);

        [Tooltip("Share of the particles kept with Reduce Effects.")]
        [Range(0f, 1f)] [SerializeField] private float reducedShare = 0.4f;

        private readonly List<RectTransform> items = new List<RectTransform>();
        private readonly List<Vector2> velocities = new List<Vector2>();
        private readonly List<Image> images = new List<Image>();
        private readonly List<float> alphas = new List<float>();

        public void Configure(Sprite dot, int particles, Vector2 sizes, Vector2 vx, Vector2 vy, Color color)
        {
            sprite = dot;
            count = particles;
            size = sizes;
            velocityX = vx;
            velocityY = vy;
            tint = color;
        }

        private void Start()
        {
            Rect r = ((RectTransform)transform).rect;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Mote", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                float s = Random.Range(size.x, size.y);
                rect.sizeDelta = new Vector2(s, s);
                rect.anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));
                Image img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                float a = tint.a * Random.Range(0.35f, 1f);
                img.color = new Color(tint.r, tint.g, tint.b, a);
                items.Add(rect);
                images.Add(img);
                alphas.Add(a);
                velocities.Add(new Vector2(Random.Range(velocityX.x, velocityX.y), Random.Range(velocityY.x, velocityY.y)));
            }
        }

        private void Update()
        {
            Rect r = ((RectTransform)transform).rect;
            int active = UserSettings.ReduceEffects ? Mathf.CeilToInt(items.Count * reducedShare) : items.Count;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < items.Count; i++)
            {
                bool on = i < active;
                if (images[i].enabled != on) images[i].enabled = on;
                if (!on) continue;
                Vector2 p = items[i].anchoredPosition + velocities[i] * dt;
                p.y += Mathf.Sin(Time.unscaledTime * 0.6f + i) * 4f * dt;
                if (p.x < r.xMin - 30f) p.x = r.xMax + 20f;
                if (p.x > r.xMax + 30f) p.x = r.xMin - 20f;
                if (p.y < r.yMin - 30f) p.y = r.yMax + 20f;
                if (p.y > r.yMax + 30f) p.y = r.yMin - 20f;
                items[i].anchoredPosition = p;
                // Fade near the left / right edges so wrapping never pops.
                float edge = Mathf.Clamp01(Mathf.Min(p.x - r.xMin, r.xMax - p.x) / 120f);
                Color c = images[i].color;
                c.a = alphas[i] * edge;
                images[i].color = c;
            }
        }
    }
}
