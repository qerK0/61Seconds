using SixtyOne.Core;
using SixtyOne.Prep;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Progress of the running prep action ("Gaz vanasını kapat" 3/5s). Input is blocked meanwhile.</summary>
    public sealed class ActionProgressBar : MonoBehaviour
    {
        [Tooltip("Prep driver (action source).")]
        [SerializeField] private PrepController prep;

        [Tooltip("Shown only while an action runs.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Fill rect; anchorMax.x = progress.")]
        [SerializeField] private RectTransform fill;

        [Tooltip("Action label.")]
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            UIVisibility.Set(group, false, blocksWhenVisible: false);
        }

        private void OnEnable()
        {
            GameEvents.ActionStarted += OnStarted;
        }

        private void OnDisable()
        {
            GameEvents.ActionStarted -= OnStarted;
        }

        private void OnStarted(string labelKey, float seconds)
        {
            if (label != null) label.text = string.IsNullOrEmpty(labelKey) ? string.Empty : TextDB.GetDisplay(labelKey);
        }

        private void Update()
        {
            bool running = prep != null && prep.Busy;
            UIVisibility.Set(group, running, blocksWhenVisible: false);
            if (running && fill != null) fill.anchorMax = new Vector2(prep.Action.Progress, fill.anchorMax.y);
        }
    }
}
