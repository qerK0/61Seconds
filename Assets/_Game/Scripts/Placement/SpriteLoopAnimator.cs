using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>Loops a sprite sequence (any frame count) on one renderer at a fixed fps, unscaled time. NPC idles.</summary>
    public sealed class SpriteLoopAnimator : MonoBehaviour
    {
        [Tooltip("Renderer to animate.")]
        [SerializeField] private SpriteRenderer target;

        [Tooltip("Frames in order.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Frames per second.")]
        [Min(0.1f)]
        [SerializeField] private float fps = 9f;

        private float time;

        private void OnEnable()
        {
            // Desync several NPCs that share the art.
            time = Random.Range(0f, 1f);
        }

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            time += Time.unscaledDeltaTime;
            Sprite sprite = frames[Mathf.FloorToInt(time * fps) % frames.Length];
            if (sprite != null && target.sprite != sprite) target.sprite = sprite;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(SpriteRenderer renderer, Sprite[] sequence, float framesPerSecond)
        {
            target = renderer;
            frames = sequence ?? new Sprite[0];
            fps = framesPerSecond;
            if (target != null && frames.Length > 0) target.sprite = frames[0];
        }
    }
}
