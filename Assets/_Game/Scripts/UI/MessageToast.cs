using System.Collections;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Short non-blocking message (bag full, placement hints, quake banner). Queued messages (the items the robber
    /// takes) show one after another instead of replacing each other.
    /// </summary>
    public sealed class MessageToast : MonoBehaviour
    {
        [Tooltip("Toast group (never blocks clicks).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Message label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Seconds visible.")]
        [SerializeField] private float showSeconds = 2.2f;

        [Tooltip("Seconds each queued message stays.")]
        [SerializeField] private float queuedSeconds = 1.4f;

        private readonly Queue<string> queue = new Queue<string>();
        private bool showingQueue;

        private void Awake()
        {
            UIVisibility.Set(group, false);
        }

        private void OnEnable()
        {
            GameEvents.Message += OnMessage;
            GameEvents.QueuedMessage += OnQueued;
            GameEvents.QuakeStarted += OnQuake;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Message -= OnMessage;
            GameEvents.QueuedMessage -= OnQueued;
            GameEvents.QuakeStarted -= OnQuake;
            GameEvents.Died -= OnDied;
        }

        /// <summary>Shows already-resolved text.</summary>
        public void Show(string text)
        {
            // A running queue (robber items) is not cut short by ordinary messages.
            if (showingQueue) return;
            StopAllCoroutines();
            StartCoroutine(ShowRoutine(text, showSeconds));
        }

        private void OnQueued(string text)
        {
            queue.Enqueue(text);
            if (showingQueue) return;
            StopAllCoroutines();
            StartCoroutine(ShowQueue());
        }

        private IEnumerator ShowQueue()
        {
            showingQueue = true;
            while (queue.Count > 0)
            {
                string text = queue.Dequeue();
                if (label != null) label.text = text;
                group.alpha = 1f;
                group.blocksRaycasts = false;
                group.interactable = false;
                yield return new WaitForSecondsRealtime(queuedSeconds);
            }

            showingQueue = false;
            yield return Tween.Fade(group, 0f, 0.3f);
        }

        private void OnMessage(string key) => Show(TextDB.GetDisplay(key));
        private void OnQuake(QuakeInfo info) => Show(TextDB.Format(UiKeys.Quake, info.Magnitude.ToString("0.0")));

        private void OnDied(DeathInfo info)
        {
            queue.Clear();
            showingQueue = false;
            StopAllCoroutines();
            UIVisibility.Set(group, false);
        }

        private IEnumerator ShowRoutine(string text, float seconds)
        {
            if (label != null) label.text = text;
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;
            yield return new WaitForSecondsRealtime(seconds);
            yield return Tween.Fade(group, 0f, 0.3f);
        }
    }
}
