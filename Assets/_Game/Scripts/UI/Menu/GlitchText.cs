using SixtyOne.Core;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// A short glitch on a title every few seconds (or on demand): red / cyan copies split sideways (RGB split), a
    /// horizontal slice of the text jumps aside, and the text flickers. Unscaled time; Reduce Effects turns it off.
    /// </summary>
    public sealed class GlitchText : MonoBehaviour
    {
        [Tooltip("The real text.")]
        [SerializeField] private TMP_Text main;

        [Tooltip("Red copy behind the text (hidden between glitches).")]
        [SerializeField] private TMP_Text red;

        [Tooltip("Cyan copy behind the text (hidden between glitches).")]
        [SerializeField] private TMP_Text cyan;

        [Tooltip("A masked band (RectMask2D) holding a copy of the text: moved and offset for the slice.")]
        [SerializeField] private RectTransform sliceBand;

        [Tooltip("The text copy inside the band.")]
        [SerializeField] private RectTransform sliceText;

        [Tooltip("Seconds between glitches (random in range); y = 0 means only on Trigger.")]
        [SerializeField] private Vector2 interval = new Vector2(5f, 8f);

        [Tooltip("Glitch length (seconds).")]
        [SerializeField] private float length = 0.15f;

        [Tooltip("RGB split / slice offset in pixels.")]
        [SerializeField] private float split = 9f;

        private float next;
        private float left;

        public void Configure(TMP_Text mainText, TMP_Text redCopy, TMP_Text cyanCopy, RectTransform band, RectTransform bandText, Vector2 seconds, float glitchLength, float pixels)
        {
            main = mainText;
            red = redCopy;
            cyan = cyanCopy;
            sliceBand = band;
            sliceText = bandText;
            interval = seconds;
            length = glitchLength;
            split = pixels;
        }

        /// <summary>One glitch now (seconds &lt;= 0 = the configured length).</summary>
        public void Trigger(float seconds = -1f)
        {
            if (UserSettings.ReduceEffects) return;
            left = seconds > 0f ? seconds : length;
        }

        private void OnEnable()
        {
            Schedule();
            Show(false);
        }

        private void Schedule()
        {
            next = interval.y > 0f ? Time.unscaledTime + Random.Range(interval.x, interval.y) : float.MaxValue;
        }

        private void Update()
        {
            if (Time.unscaledTime >= next)
            {
                Trigger();
                Schedule();
            }

            if (left <= 0f) return;
            left -= Time.unscaledDeltaTime;
            if (left <= 0f || UserSettings.ReduceEffects)
            {
                left = 0f;
                Show(false);
                return;
            }

            Show(true);
            float jitter = Random.Range(0.4f, 1f) * split;
            if (red != null)
            {
                red.text = main != null ? main.text : red.text;
                red.rectTransform.anchoredPosition = new Vector2(-jitter, Random.Range(-1f, 1f));
            }

            if (cyan != null)
            {
                cyan.text = main != null ? main.text : cyan.text;
                cyan.rectTransform.anchoredPosition = new Vector2(jitter, Random.Range(-1f, 1f));
            }

            if (sliceBand != null && sliceText != null)
            {
                // A band at a random height; the copy inside counter-moves so it lines up, plus the sideways jump.
                if (main != null && sliceText.TryGetComponent(out TMP_Text copy)) copy.text = main.text;
                float h = ((RectTransform)sliceBand.parent).rect.height;
                float y = Random.Range(-0.3f, 0.3f) * h;
                sliceBand.anchoredPosition = new Vector2(0f, y);
                sliceText.anchoredPosition = new Vector2(Random.Range(-2.2f, 2.2f) * split, -y);
            }

            if (main != null) main.alpha = Random.value < 0.35f ? 0.55f : 1f;
        }

        private void Show(bool on)
        {
            if (red != null) red.enabled = on;
            if (cyan != null) cyan.enabled = on;
            if (sliceBand != null) sliceBand.gameObject.SetActive(on);
            if (main != null && !on) main.alpha = 1f;
        }
    }
}
