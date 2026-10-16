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

            if (lockIcon != null) lockIcon.SetActive(view.Locked);
            if (reasonText != null)
            {
                reasonText.gameObject.SetActive(view.Locked);
                TextKey reason = view.Choice.LockedReasonKey;
                reasonText.text = reason.IsEmpty ? TextDB.GetDisplay(UiKeys.Locked) : reason.Display;
            }
        }
    }
}
