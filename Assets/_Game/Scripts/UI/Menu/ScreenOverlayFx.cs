using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Full-screen texture overlays on the menu / end screen: scanlines (a slow drift and a faint flicker) and film
    /// grain (the noise tile jumps to a new offset ~24 times a second). Reduce Effects freezes the flicker and the
    /// grain (the static overlay stays).
    /// </summary>
    public sealed class ScreenOverlayFx : MonoBehaviour
    {
        public enum Mode { Scanlines, Grain }

        [SerializeField] private RawImage image;
        [SerializeField] private Mode mode;

        [Tooltip("Base alpha of the overlay.")]
        [SerializeField] private float alpha = 0.12f;

        [Tooltip("Grain: offset jumps per second.")]
        [SerializeField] private float grainRate = 24f;

        private float next;

        public void Configure(RawImage target, Mode overlay, float baseAlpha)
        {
            image = target;
            mode = overlay;
            alpha = baseAlpha;
        }

        private void Update()
        {
            if (image == null) return;
            bool reduce = UserSettings.ReduceEffects;
            Color c = image.color;
            Rect r = image.uvRect;
            if (mode == Mode.Scanlines)
            {
                c.a = reduce ? alpha : alpha * (0.85f + 0.15f * Mathf.PerlinNoise(Time.unscaledTime * 6f, 0.3f));
                if (!reduce) r.y = Mathf.Repeat(r.y + Time.unscaledDeltaTime * 0.02f, 1f);
            }
            else
            {
                c.a = reduce ? 0f : alpha;
                if (!reduce && Time.unscaledTime >= next)
                {
                    next = Time.unscaledTime + 1f / Mathf.Max(1f, grainRate);
                    r.position = new Vector2(Random.value, Random.value);
                }
            }

            image.color = c;
            image.uvRect = r;
        }
    }
}
