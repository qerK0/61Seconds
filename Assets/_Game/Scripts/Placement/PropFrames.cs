using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A frame-animated prop (the chandelier): frames 0..n-2 loop while a quake shakes, the last frame is the prop
    /// lying fallen on the floor. HazardProp's Drop motion ends on the fallen frame instead of moving the prop.
    /// Any frame count works.
    /// </summary>
    public sealed class PropFrames : MonoBehaviour
    {
        [Tooltip("Renderer to animate.")]
        [SerializeField] private SpriteRenderer target;

        [Tooltip("Swing frames, then the fallen frame last.")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Swing frames per second.")]
        [Min(0.1f)]
        [SerializeField] private float fps = 10f;

        private bool swinging;
        private bool fallen;
        private float time;

        public bool HasFallenFrame => frames != null && frames.Length >= 2;

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.QuakeEnded += OnQuakeEnded;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.QuakeEnded -= OnQuakeEnded;
            swinging = false;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(SpriteRenderer renderer, Sprite[] sequence, float framesPerSecond)
        {
            target = renderer;
            frames = sequence ?? new Sprite[0];
            fps = framesPerSecond;
            ShowFrame(0);
        }

        /// <summary>Back to hanging still (ResetPose).</summary>
        public void ResetFrames()
        {
            fallen = false;
            swinging = false;
            ShowFrame(0);
        }

        /// <summary>Shows the fallen frame and stops swinging.</summary>
        public void Fall()
        {
            if (!HasFallenFrame) return;
            fallen = true;
            swinging = false;
            ShowFrame(frames.Length - 1);
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            if (fallen) return;
            swinging = true;
            time = 0f;
        }

        private void OnQuakeEnded(QuakeInfo info)
        {
            swinging = false;
            if (!fallen) ShowFrame(0);
        }

        private void Update()
        {
            if (!swinging || fallen || frames == null) return;
            int swingFrames = HasFallenFrame ? frames.Length - 1 : frames.Length;
            if (swingFrames <= 0) return;
            time += Time.deltaTime;
            // Ping-pong through the swing frames.
            int cycle = Mathf.Max(1, swingFrames * 2 - 2);
            int step = Mathf.FloorToInt(time * fps) % cycle;
            ShowFrame(step < swingFrames ? step : cycle - step);
        }

        private void ShowFrame(int index)
        {
            if (target == null || frames == null || frames.Length == 0) return;
            Sprite sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
            if (sprite != null) target.sprite = sprite;
        }
    }
}
