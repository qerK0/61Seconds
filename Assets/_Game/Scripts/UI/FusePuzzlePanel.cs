using System;
using SixtyOne.Puzzles;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Fuse box UI: one toggle button per switch (I / O) and a "Şalteri kaldır" button.</summary>
    public sealed class FusePuzzlePanel : MonoBehaviour, IFusePanel
    {
        [Tooltip("Panel group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Switch buttons (up to 4).")]
        [SerializeField] private Button[] switchButtons = new Button[4];

        [Tooltip("Switch labels (I / O).")]
        [SerializeField] private TMP_Text[] switchLabels = new TMP_Text[4];

        [Tooltip("Attempt button.")]
        [SerializeField] private Button tryButton;

        [Tooltip("Color of a switch that is on.")]
        [SerializeField] private Color onColor = new Color(1f, 0.82f, 0.2f, 1f);

        [Tooltip("Color of a switch that is off.")]
        [SerializeField] private Color offColor = new Color(0.25f, 0.25f, 0.3f, 1f);

        private Action<int> toggle;
        private Action attempt;

        private void Awake()
        {
            UIVisibility.Set(group, false);
            for (int i = 0; i < switchButtons.Length; i++)
            {
                int index = i;
                if (switchButtons[i] != null) switchButtons[i].onClick.AddListener(() => toggle?.Invoke(index));
            }

            if (tryButton != null) tryButton.onClick.AddListener(() => attempt?.Invoke());
        }

        public void Show(bool[] switches, Action<int> onToggle, Action onTry)
        {
            toggle = onToggle;
            attempt = onTry;
            for (int i = 0; i < switchButtons.Length; i++)
            {
                if (switchButtons[i] != null) switchButtons[i].gameObject.SetActive(i < switches.Length);
            }

            Refresh(switches);
            UIVisibility.Set(group, true);
        }

        public void Refresh(bool[] switches)
        {
            for (int i = 0; i < switches.Length && i < switchButtons.Length; i++)
            {
                if (switchLabels.Length > i && switchLabels[i] != null) switchLabels[i].text = switches[i] ? "I" : "O";
                if (switchButtons[i] != null && switchButtons[i].targetGraphic != null) switchButtons[i].targetGraphic.color = switches[i] ? onColor : offColor;
            }
        }

        public void Hide()
        {
            toggle = null;
            attempt = null;
            UIVisibility.Set(group, false);
        }
    }
}
