using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Narration with no speaker (node bodies, placement instructions, street hints, the epilogue): a dark card at
    /// the top centre under the timer, never in the middle of the scene. It sizes to its text (max width), fades
    /// out after a reading delay (unless persistent) or when the node changes, and never blocks clicks.
    /// </summary>
    public sealed class NarrationCard : MonoBehaviour
    {
        [Tooltip("Card group (alpha only; never blocks raycasts).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("The card background (top-centre pivot); sized to the text.")]
        [SerializeField] private RectTransform card;

        [Tooltip("Text label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Max card width (px) and inner padding (px).")]
        [SerializeField] private Vector2 maxWidthAndPadding = new Vector2(720f, 18f);

        [Tooltip("Seconds visible plus reading time (characters / 18 per second).")]
        [SerializeField] private float readSeconds = 3f;

        [Tooltip("Fade seconds.")]
        [SerializeField] private float fadeSeconds = 0.15f;

        private bool wanted;
        private float hideAt = -1f;
        private string shown;

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        private void OnEnable()
        {
            GameEvents.Message += OnMessage;
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Message -= OnMessage;
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.Died -= OnDied;
        }

        /// <summary>Shows resolved text; persistent = stays until Hide or the next node.</summary>
        public void Show(string text, bool persistent = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            wanted = true;
            hideAt = persistent ? -1f : Time.unscaledTime + readSeconds + text.Length / 18f;
            if (text == shown) return;
            shown = text;
            if (label != null) label.text = text;
            Fit();
        }

        public void Hide()
        {
            wanted = false;
            hideAt = -1f;
            shown = null;
        }

        private void Fit()
        {
            if (label == null || card == null) return;
            float pad = maxWidthAndPadding.y;
            float maxInner = maxWidthAndPadding.x - pad * 2f;
            Vector2 one = label.GetPreferredValues(label.text, 99999f, 0f);
            float inner = Mathf.Min(maxInner, one.x + 2f);
            float height = label.GetPreferredValues(label.text, inner, 0f).y;
            card.sizeDelta = new Vector2(inner + pad * 2f, height + pad * 1.4f);
        }

        private void OnMessage(string key)
        {
            if (UiKeys.IsNarration(key)) Show(TextDB.GetDisplay(key));
        }

        private void OnNodeEntered(string nodeId) => Hide();

        private void OnDied(DeathInfo info) => Hide();

        private void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt) Hide();
            if (group == null) return;
            float target = wanted && !PauseState.IsPaused ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
        }
    }
}
