using System.Text;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Collected Deprem Notları, shown as x/total plus a list. Uncollected tips show as locked.</summary>
    public sealed class TipJournal : MonoBehaviour
    {
        [Tooltip("State owner (collected tips).")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Tip registry (total).")]
        [SerializeField] private GameContent content;

        [Tooltip("Button that opens the journal.")]
        [SerializeField] private Button openButton;

        [Tooltip("Label on the open button (shows x/total).")]
        [SerializeField] private TMP_Text openLabel;

        [Tooltip("Journal panel.")]
        [SerializeField] private CanvasGroup panel;

        [Tooltip("Panel header.")]
        [SerializeField] private TMP_Text header;

        [Tooltip("Tip list.")]
        [SerializeField] private TMP_Text body;

        [Tooltip("Close button.")]
        [SerializeField] private Button closeButton;

        [Tooltip("Close button label.")]
        [SerializeField] private TMP_Text closeLabel;

        private void Awake()
        {
            UIVisibility.Set(panel, false);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
            TextDB.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
            TextDB.Changed -= Refresh;
        }

        public void Open()
        {
            Refresh();
            UIVisibility.Set(panel, true);
        }

        public void Close()
        {
            UIVisibility.Set(panel, false);
        }

        public void Refresh()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            int total = content != null ? content.Tips.Count : 0;
            int collected = 0;
            var list = new StringBuilder();

            if (content != null)
            {
                foreach (Tip tip in content.Tips)
                {
                    if (tip == null) continue;
                    bool has = gm != null && gm.State.CollectedTips.Contains(tip.Id);
                    if (has)
                    {
                        collected++;
                        list.Append("<b>").Append(tip.TitleKey.Display).Append("</b>\n").Append(tip.TextKey.Display).Append("\n\n");
                    }
                    else
                    {
                        list.Append(TextDB.GetDisplay(UiKeys.JournalLocked)).Append("\n\n");
                    }
                }
            }

            string count = TextDB.Format(UiKeys.JournalCount, collected, total);
            if (openLabel != null) openLabel.text = TextDB.GetDisplay(UiKeys.JournalOpen) + " " + count;
            if (header != null) header.text = TextDB.GetDisplay(UiKeys.JournalTitle) + "  " + count;
            if (body != null) body.text = list.ToString();
            if (closeLabel != null) closeLabel.text = TextDB.GetDisplay(UiKeys.JournalClose);
        }

        private void OnDied(DeathInfo info) => Refresh();
        private void OnRewound(string nodeId) => Refresh();
    }
}
