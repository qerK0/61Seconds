using System;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// One choice button. Locked choices show a lock icon and a reason line. Traps look normal on purpose,
    /// and ShowLockedFatal choices look locked but stay clickable.
    /// </summary>
    public sealed class ChoiceButton : MonoBehaviour
    {
        [Tooltip("The clickable button.")]
        [SerializeField] private Button button;

        [Tooltip("Choice label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Lock icon shown on locked choices.")]
        [SerializeField] private GameObject lockIcon;

        [Tooltip("Why it is locked.")]
        [SerializeField] private TMP_Text reasonText;

        [Tooltip("Optional '-Ns' building-time cost label.")]
        [SerializeField] private TMP_Text costText;

        [Tooltip("Thin accent bar on the left edge (accent when usable, muted when locked). Optional.")]
        [SerializeField] private Graphic accentBar;

        [Tooltip("Accent bar colour when the choice is usable.")]
        [SerializeField] private Color accentColor = new Color(1f, 0.76f, 0.29f, 1f);

        [Tooltip("Accent bar colour when locked.")]
        [SerializeField] private Color lockedAccentColor = new Color(0.45f, 0.45f, 0.5f, 1f);

        private Vector2 labelMin;
        private Vector2 labelMax;
        private bool labelAnchorsCached;

        /// <summary>Fills the button from a choice view.</summary>
        public void Bind(ChoiceView view, Action onClick)
        {
            if (label != null) label.text = view.Choice.LabelKey.Display;

            button.onClick.RemoveAllListeners();
            button.interactable = view.Clickable;
            if (view.Clickable && onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            if (accentBar != null) accentBar.color = view.Locked ? lockedAccentColor : accentColor;
            if (lockIcon != null) lockIcon.SetActive(view.Locked);
            SetLabelLine(view.Locked);
            if (reasonText != null)
            {
                reasonText.gameObject.SetActive(view.Locked);
                TextKey reason = view.Choice.LockedReasonKey;
                reasonText.text = view.ReasonOverride != null ? TextDB.GetDisplay(view.ReasonOverride)
                    : reason.IsEmpty ? TextDB.GetDisplay(UiKeys.Locked) : reason.Display;
            }

            if (costText != null)
            {
                float seconds = view.Choice.TimeCost;
                costText.gameObject.SetActive(seconds > 0f);
                if (seconds > 0f) costText.text = TextDB.Format(UiKeys.TimeSpent, Mathf.CeilToInt(seconds));
            }
        }

        /// <summary>A plain button (dialogue "Devam") with a resolved label.</summary>
        public void BindCustom(string text, Action onClick)
        {
            if (label != null) label.text = text;
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            if (accentBar != null) accentBar.color = accentColor;
            if (lockIcon != null) lockIcon.SetActive(false);
            if (reasonText != null) reasonText.gameObject.SetActive(false);
            if (costText != null) costText.gameObject.SetActive(false);
            SetLabelLine(false);
        }

        /// <summary>Locked: the label moves to the top line so the reason gets the full-width bottom line.</summary>
        private void SetLabelLine(bool locked)
        {
            if (label == null) return;
            RectTransform rect = label.rectTransform;
            if (!labelAnchorsCached)
            {
                labelMin = rect.anchorMin;
                labelMax = rect.anchorMax;
                labelAnchorsCached = true;
            }

            rect.anchorMin = locked ? new Vector2(labelMin.x, 0.46f) : labelMin;
            rect.anchorMax = labelMax;
        }
    }
}
