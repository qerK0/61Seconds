using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// A HUD column that slides off the right edge, leaving only its toggle tab (the bag + key items).
    /// Unscaled time; the open state is remembered per viewer in PlayerPrefs.
    /// </summary>
    public sealed class CollapsiblePanel : MonoBehaviour
    {
        private const string PrefKey = "SixtyOne.BagCollapsed";

        [Tooltip("The sliding content (right-anchored).")]
        [SerializeField] private RectTransform content;

        [Tooltip("Toggle tab button (stays on screen).")]
        [SerializeField] private Button toggle;

        [Tooltip("Tab label (collapse / expand arrow text).")]
        [SerializeField] private TMP_Text toggleLabel;

        [Tooltip("Slide time in seconds (unscaled).")]
        [SerializeField] private float slideSeconds = 0.2f;

        private Vector2 openPosition;
        private bool collapsed;
        private float velocity;

        private void Awake()
        {
            if (content != null) openPosition = content.anchoredPosition;
            collapsed = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            if (toggle != null) toggle.onClick.AddListener(Toggle);
            if (content != null) content.anchoredPosition = Target();
            RefreshLabel();
        }

        private void OnEnable()
        {
            TextDB.Changed += RefreshLabel;
        }

        private void OnDisable()
        {
            TextDB.Changed -= RefreshLabel;
        }

        /// <summary>Slides the panel open if it is collapsed (the tutorial points at the bag).</summary>
        public void Expand()
        {
            if (collapsed) Toggle();
        }

        public void Toggle()
        {
            collapsed = !collapsed;
            PlayerPrefs.SetInt(PrefKey, collapsed ? 1 : 0);
            RefreshLabel();
        }

        private void Update()
        {
            if (content == null) return;
            Vector2 position = content.anchoredPosition;
            position.x = Mathf.SmoothDamp(position.x, Target().x, ref velocity, Mathf.Max(0.01f, slideSeconds * 0.5f), Mathf.Infinity, Time.unscaledDeltaTime);
            content.anchoredPosition = position;
        }

        private Vector2 Target()
        {
            if (!collapsed || content == null) return openPosition;
            return openPosition + new Vector2(content.rect.width + 24f, 0f);
        }

        private void RefreshLabel()
        {
            if (toggleLabel != null) toggleLabel.text = TextDB.GetDisplay(collapsed ? UiKeys.BagExpand : UiKeys.BagCollapse);
        }
    }
}
