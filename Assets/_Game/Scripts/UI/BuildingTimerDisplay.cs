using System.Collections;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Building collapse timer (mm:ss). Pulses after the pulse threshold, turns red and punches on every alarm
    /// tick, and flashes "-Ns" whenever an action costs time. Hidden during the pre-quake prep.
    /// </summary>
    public sealed class BuildingTimerDisplay : MonoBehaviour
    {
        [Tooltip("Timer source.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("The mm:ss label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("'-Ns' label next to the timer.")]
        [SerializeField] private TMP_Text spentLabel;

        [Tooltip("Hidden until the quake starts the building timer (the prep countdown shows instead). Optional.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Normal color.")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Color after the pulse threshold.")]
        [SerializeField] private Color pulseColor = new Color(1f, 0.75f, 0.3f, 1f);

        [Tooltip("Color in the alarm window.")]
        [SerializeField] private Color alarmColor = new Color(1f, 0.25f, 0.2f, 1f);

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
            int total = Mathf.CeilToInt(remaining);
            label.text = $"{total / 60:00}:{total % 60:00}";

            float pulseAt = survival.Config != null ? survival.Config.PulseAtSeconds : 60f;
            float alarmAt = survival.Config != null ? survival.Config.AlarmAtSeconds : 30f;
            if (remaining <= alarmAt) label.color = alarmColor;
            else if (remaining <= pulseAt)
            {
                float k = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f) + 1f) * 0.5f;
                label.color = Color.Lerp(normalColor, pulseColor, k);
            }
            else label.color = normalColor;
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
