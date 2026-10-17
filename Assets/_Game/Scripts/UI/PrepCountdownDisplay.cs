using System.Collections;
using SixtyOne.Core;
using SixtyOne.Prep;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// The pre-quake prep countdown ("Depreme kalan 00:42") as an amber LED clock (same LedClock face as the
    /// building timer), shown only while the building timer has not started. Red and punching on every second of
    /// the last ticks; flashes "-Ns" (coloured to match) when a prep action costs time.
    /// </summary>
    public sealed class PrepCountdownDisplay : MonoBehaviour
    {
        [Tooltip("Countdown source.")]
        [SerializeField] private PrepPhase prep;

        [Tooltip("Shown only during prep.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("The m:ss label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("'-Ns' label next to the countdown.")]
        [SerializeField] private TMP_Text spentLabel;

        [Tooltip("LED face that renders the digits, ghost, colon blink and glow.")]
        [SerializeField] private LedClock clock;

        [Tooltip("Normal LED colour (amber #FFB020).")]
        [SerializeField] private Color normalColor = new Color(1f, 0.69f, 0.125f, 1f);

        [Tooltip("LED colour in the last ticking seconds (#FF3B30).")]
        [SerializeField] private Color tickColor = new Color(1f, 0.231f, 0.188f, 1f);

        [Tooltip("Ticking starts at this many seconds (matches LevelConfig.prepTickFrom).")]
        [SerializeField] private int tickFrom = 10;

        private Coroutine spentRoutine;

        private static bool InPrep => GameManager.Instance != null && !GameManager.Instance.State.BuildingTimerStarted;

        private void OnEnable()
        {
            GameEvents.PrepTick += OnTick;
            GameEvents.TimeSpent += OnSpent;
            if (spentLabel != null) spentLabel.text = string.Empty;
        }

        private void OnDisable()
        {
            GameEvents.PrepTick -= OnTick;
            GameEvents.TimeSpent -= OnSpent;
        }

        private void Update()
        {
            bool show = InPrep;
            if (group != null) group.alpha = show ? 1f : 0f;
            if (!show || label == null || prep == null) return;

            float remaining = prep.Remaining;
            Color color = remaining <= tickFrom ? tickColor : normalColor;
            if (clock != null)
            {
                clock.Show(remaining, color, LedClock.TimersRunning, false);
            }
            else
            {
                int total = Mathf.CeilToInt(remaining);
                label.text = $"{total / 60:00}:{total % 60:00}";
                label.color = color;
            }

            if (spentLabel != null) spentLabel.color = new Color(color.r, color.g, color.b, spentLabel.alpha);
        }

        private void OnTick(int secondsLeft)
        {
            if (label == null) return;
            label.transform.localScale = Vector3.one * 1.3f;
            StartCoroutine(Tween.Scale(label.transform, Vector3.one, 0.3f, Ease.OutBack, unscaled: true));
        }

        private void OnSpent(float seconds)
        {
            if (spentLabel == null || seconds < 0.5f || !InPrep) return;
            if (spentRoutine != null) StopCoroutine(spentRoutine);
            spentRoutine = StartCoroutine(ShowSpent(seconds));
        }

        private IEnumerator ShowSpent(float seconds)
        {
            spentLabel.text = TextDB.Format(UiKeys.TimeSpent, Mathf.CeilToInt(seconds));
            spentLabel.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Tween.Run(0.4f, k => { if (spentLabel != null) spentLabel.alpha = 1f - k; }, Ease.Linear, unscaled: true);
            spentLabel.text = string.Empty;
            spentRoutine = null;
        }
    }
}
