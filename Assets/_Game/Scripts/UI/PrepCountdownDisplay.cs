using System.Collections;
using SixtyOne.Core;
using SixtyOne.Prep;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// The pre-quake prep countdown ("Depreme kalan 0:42"), shown only while the building timer has not started.
    /// Red and punching on every second of the last ticks; flashes "-Ns" when a prep action costs time.
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

        [Tooltip("Normal color.")]
        [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.35f, 1f);

        [Tooltip("Color in the last ticking seconds.")]
        [SerializeField] private Color tickColor = new Color(1f, 0.25f, 0.2f, 1f);

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
            int total = Mathf.CeilToInt(remaining);
            label.text = $"{total / 60}:{total % 60:00}";
            label.color = remaining <= tickFrom ? tickColor : normalColor;
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
