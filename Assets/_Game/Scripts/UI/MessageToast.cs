using System.Collections;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Short non-blocking message (bag full, placement hints, quake banner).</summary>
    public sealed class MessageToast : MonoBehaviour
    {
        [Tooltip("Toast group (never blocks clicks).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Message label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Seconds visible.")]
        [SerializeField] private float showSeconds = 2.2f;

        private void Awake()
        {
            UIVisibility.Set(group, false);
        }

        private void OnEnable()
        {
            GameEvents.Message += OnMessage;
            GameEvents.QuakeStarted += OnQuake;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Message -= OnMessage;
            GameEvents.QuakeStarted -= OnQuake;
            GameEvents.Died -= OnDied;
        }

        /// <summary>Shows already-resolved text.</summary>
        public void Show(string text)
        {
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(text));
        }

        private void OnMessage(string key) => Show(TextDB.GetDisplay(key));
        private void OnQuake(QuakeInfo info) => Show(TextDB.Format(UiKeys.Quake, info.Magnitude.ToString("0.0")));

        private void OnDied(DeathInfo info)
        {
            StopAllCoroutines();
            UIVisibility.Set(group, false);
        }

        private IEnumerator ShowRoutine(string text)
        {
            if (label != null) label.text = text;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;
            yield return new WaitForSecondsRealtime(showSeconds);
            yield return Tween.Fade(group, 0f, 0.3f);
        }
    }
}
