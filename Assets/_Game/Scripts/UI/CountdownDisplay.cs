using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Prep;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// The big "61" countdown. Shows the prep countdown (it keeps running through the placement window),
    /// or the placement timer when a placement runs without prep. Red and punching in the last seconds.
    /// </summary>
    public sealed class CountdownDisplay : MonoBehaviour
    {
        [Tooltip("Prep driver (countdown source).")]
        [SerializeField] private PrepController prep;

        [Tooltip("Fallback source: the placement node timer.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Shown only while a countdown runs.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("The number.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Normal color.")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Color inside the placement window / last seconds.")]
        [SerializeField] private Color urgentColor = new Color(1f, 0.25f, 0.2f, 1f);

        private void OnEnable()
        {
            GameEvents.PrepTick += OnTick;
        }

        private void OnDisable()
        {
            GameEvents.PrepTick -= OnTick;
        }

        private void Update()
        {
            float remaining;
            bool urgent;
            if (prep != null && prep.Countdown.Running)
            {
                remaining = prep.Countdown.Remaining;
                urgent = prep.Countdown.InPlacementWindow;
            }
            else if (nodePlayer != null && nodePlayer.Current is PlacementNode && nodePlayer.Timer.Running)
            {
                remaining = nodePlayer.Timer.Remaining;
                urgent = true;
            }
            else
            {
                UIVisibility.Set(group, false, blocksWhenVisible: false);
                return;
            }

            UIVisibility.Set(group, true, blocksWhenVisible: false);
            if (label == null) return;
            label.text = Mathf.CeilToInt(remaining).ToString();
            label.color = urgent ? urgentColor : normalColor;
        }

        private void OnTick(int secondsLeft)
        {
            if (label == null) return;
            StopAllCoroutines();
            label.transform.localScale = Vector3.one * 1.35f;
            StartCoroutine(Tween.Scale(label.transform, Vector3.one, 0.35f, Ease.OutBack, unscaled: true));
        }
    }
}
