using System.Collections;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Building collapse timer as a green LED alarm clock (LedClock). Amber and pulsing after the pulse threshold,
    /// red with a slight flicker (not with Reduce Effects) in the alarm window, punches on every alarm tick, and
    /// flashes "-Ns" (coloured to match) whenever an action costs time. Hidden during the pre-quake prep.
    /// </summary>
    public sealed class BuildingTimerDisplay : MonoBehaviour
    {
        [Tooltip("Timer source.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("The mm:ss label (the LED digits; punched on alarm ticks).")]
        [SerializeField] private TMP_Text label;

        [Tooltip("LED face that renders the digits, ghost, colon blink and glow.")]
        [SerializeField] private LedClock clock;

        [Tooltip("'-Ns' label next to the timer.")]
        [SerializeField] private TMP_Text spentLabel;

        [Tooltip("Hidden until the quake starts the building timer (the prep countdown shows instead). Optional.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Normal LED colour (#4CFF6A).")]
        [SerializeField] private Color normalColor = new Color(0.298f, 1f, 0.416f, 1f);

        [Tooltip("LED colour after the pulse threshold (#FFB020).")]
        [SerializeField] private Color pulseColor = new Color(1f, 0.69f, 0.125f, 1f);

        [Tooltip("LED colour in the alarm window (#FF3B30).")]
        [SerializeField] private Color alarmColor = new Color(1f, 0.231f, 0.188f, 1f);

        private Coroutine spentRoutine;

        private void OnEnable()
        {
            GameEvents.BuildingTick += OnTick;
            GameEvents.TimeSpent += OnSpent;
            if (spentLabel != null) spentLabel.text = string.Empty;
        }

        private void OnDisable()
        {
            GameEvents.BuildingTick -= OnTick;
            GameEvents.TimeSpent -= OnSpent;
        }

        private void Update()
        {
            if (survival == null || label == null) return;
            bool started = GameManager.Instance == null || GameManager.Instance.State.BuildingTimerStarted;
            if (group != null) group.alpha = started ? 1f : 0f;
            if (!started) return;
            float remaining = survival.Remaining;
            float pulseAt = survival.Config != null ? survival.Config.PulseAtSeconds : 60f;
            float alarmAt = survival.Config != null ? survival.Config.AlarmAtSeconds : 30f;
            Color color = normalColor;
            bool flicker = false;
            if (remaining <= alarmAt)
            {
                color = alarmColor;
                flicker = !LedClock.ReduceEffects;
            }
            else if (remaining <= pulseAt)
            {
                float k = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f;
                color = Color.Lerp(pulseColor, new Color(pulseColor.r * 0.6f, pulseColor.g * 0.6f, pulseColor.b * 0.6f, 1f), k);
            }

            if (clock != null)
            {
                clock.Show(remaining, color, LedClock.TimersRunning, flicker);
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
            label.transform.localScale = Vector3.one * 1.25f;
            StartCoroutine(Tween.Scale(label.transform, Vector3.one, 0.3f, Ease.OutBack, unscaled: true));
        }

        private void OnSpent(float seconds)
        {
            if (spentLabel == null || seconds < 0.5f) return;
            if (GameManager.Instance != null && !GameManager.Instance.State.BuildingTimerStarted) return;
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
