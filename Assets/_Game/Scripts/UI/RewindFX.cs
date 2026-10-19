using System.Collections;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// VHS-style rewind overlay: scrolling scanline texture (generated in code), flicker,
    /// and a reverse shake of the UI/camera. Never blocks clicks.
    /// </summary>
    public sealed class RewindFX : MonoBehaviour
    {
        [Tooltip("Full-screen overlay group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Full-screen RawImage that shows the scanlines.")]
        [SerializeField] private RawImage scanlines;

        [Tooltip("Shaken during the rewind (e.g. the main camera).")]
        [SerializeField] private Transform shakeTarget;

        [Tooltip("Effect length in seconds.")]
        [SerializeField] private float duration = 0.9f;

        [Tooltip("Shake magnitude (world units for a camera).")]
        [SerializeField] private float shakeMagnitude = 0.2f;

        [Tooltip("Number of scanlines across the screen height.")]
        [SerializeField] private float lineCount = 90f;

        private Texture2D texture;

        private void Awake()
        {
            UIVisibility.Set(group, false, blocksWhenVisible: false);
            if (scanlines != null)
            {
                scanlines.raycastTarget = false;
                if (scanlines.texture == null) scanlines.texture = CreateScanlineTexture();
            }
        }

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
        }

        private void OnRewound(string nodeId)
        {
            StopAllCoroutines();
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            // Macro rewinds (back to the prep start) run longer so the jump reads as bigger.
            GameManager gm = GameManager.Instance;
            float length = gm != null && gm.Session.LastRewindWasMacro ? duration * 1.8f : duration;

            UIVisibility.Set(group, true, blocksWhenVisible: false);
            if (shakeTarget != null) StartCoroutine(Tween.Shake(shakeTarget, length, shakeMagnitude, grow: false, unscaled: true));

            float time = 0f;
            while (time < length)
            {
                time += Time.unscaledDeltaTime;
                float k = time / length;
                if (scanlines != null)
                {
                    // Lines roll upward fast (rewinding), slowing down at the end.
                    scanlines.uvRect = new Rect(0f, time * 6f * (1f - k), 1f, lineCount / 2f);
                }

                group.alpha = Mathf.Lerp(0.95f, 0f, k * k) * Random.Range(0.75f, 1f);
                yield return null;
            }

            UIVisibility.Set(group, false, blocksWhenVisible: false);
        }

        private Texture2D CreateScanlineTexture()
        {
            texture = new Texture2D(1, 4, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                name = "RewindScanlines"
            };

            texture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            texture.SetPixel(0, 1, new Color(0.6f, 0.9f, 1f, 0.12f));
            texture.SetPixel(0, 2, new Color(0f, 0f, 0f, 0.35f));
            texture.SetPixel(0, 3, new Color(1f, 1f, 1f, 0.05f));
            texture.Apply();
            return texture;
        }
    }
}
