using System;
using System.Collections;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Instant click feedback + the panic input delay. Every routed click shows a ring at the pointer right
    /// away; when panic adds a delay, the ring fills over the delay with a slight hand shake and the action runs
    /// when it is full. One pending click at a time; death cancels it.
    /// </summary>
    public sealed class ClickFeedback : MonoBehaviour
    {
        [Tooltip("Ring image (Filled, Radial 360).")]
        [SerializeField] private Image ring;

        [Tooltip("Seconds the ring shows for an instant (no delay) click.")]
        [SerializeField] private float instantFlashSeconds = 0.15f;

        [Tooltip("Hand-shake jitter in pixels at full delay.")]
        [SerializeField] private float shakePixels = 6f;

        private Coroutine pending;

        private void Awake()
        {
            if (ring != null)
            {
                ring.raycastTarget = false;
                ring.enabled = false;
            }
        }

        private void OnEnable()
        {
            InputDelayRouter.Scheduler = Schedule;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            if (InputDelayRouter.Scheduler == (Action<Action, Vector2, float>)Schedule) InputDelayRouter.Scheduler = null;
            GameEvents.Died -= OnDied;
        }

        private void Schedule(Action action, Vector2 screenPosition, float delay)
        {
            if (pending != null) return;
            if (delay <= 0f)
            {
                // Run synchronously: a coroutine that finishes before StartCoroutine returns would leave
                // `pending` assigned forever and swallow every later click.
                action();
                if (ring != null) StartCoroutine(Flash(screenPosition));
                return;
            }

            pending = StartCoroutine(Run(action, screenPosition, delay));
        }

        private IEnumerator Flash(Vector2 screenPosition)
        {
            ring.enabled = true;
            ring.transform.position = screenPosition;
            ring.fillAmount = 1f;
            yield return new WaitForSecondsRealtime(instantFlashSeconds);
            if (pending == null) ring.enabled = false;
        }

        private IEnumerator Run(Action action, Vector2 screenPosition, float delay)
        {
            if (ring != null)
            {
                ring.enabled = true;
                ring.transform.position = screenPosition;
                ring.fillAmount = 0f;
            }

            float time = 0f;
            while (time < delay)
            {
                time += Time.unscaledDeltaTime;
                if (ring != null)
                {
                    ring.fillAmount = Mathf.Clamp01(time / delay);
                    Vector2 jitter = UnityEngine.Random.insideUnitCircle * shakePixels * Mathf.Clamp01(delay / 0.3f);
                    ring.transform.position = screenPosition + jitter;
                }

                yield return null;
            }

            pending = null;
            if (ring != null) ring.enabled = false;
            action();
        }

        private void OnDied(DeathInfo info)
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;
            if (ring != null) ring.enabled = false;
        }
    }
}
