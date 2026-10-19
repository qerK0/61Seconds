using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Scrolls a tiled RawImage (hazard stripes) at a constant speed in UV units per second (unscaled).</summary>
    public sealed class UvScroll : MonoBehaviour
    {
        [SerializeField] private RawImage image;

        [Tooltip("UV units per second.")]
        [SerializeField] private Vector2 speed = new Vector2(0.15f, 0f);

        public void Configure(RawImage target, Vector2 uvPerSecond)
        {
            image = target;
            speed = uvPerSecond;
        }

        private void Update()
        {
            if (image == null) return;
            Rect r = image.uvRect;
            r.position = new Vector2(Mathf.Repeat(r.x + speed.x * Time.unscaledDeltaTime, 1f), Mathf.Repeat(r.y + speed.y * Time.unscaledDeltaTime, 1f));
            image.uvRect = r;
        }
    }
}
