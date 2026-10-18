using System;
using System.Collections;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>A full-screen black CanvasGroup: fade from black on start, fade to black before a scene change (unscaled).</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        [Tooltip("Fade-in seconds when the scene starts (0 = start clear).")]
        [SerializeField] private float fadeInSeconds = 0.6f;

        public bool Busy { get; private set; }

        public void Configure(CanvasGroup black, float fadeIn)
        {
            group = black;
            fadeInSeconds = fadeIn;
        }

        private void Start()
        {
            if (group == null) return;
            group.blocksRaycasts = false;
            if (fadeInSeconds > 0f)
            {
                group.alpha = 1f;
                StartCoroutine(Fade(1f, 0f, fadeInSeconds, null));
            }
            else
            {
                group.alpha = 0f;
            }
        }

        /// <summary>Fades to black, then runs the action (once; later calls while busy are ignored).</summary>
        public void FadeOut(float seconds, Action then)
        {
            if (Busy) return;
            StopAllCoroutines();
            StartCoroutine(Fade(group != null ? group.alpha : 0f, 1f, seconds, then));
        }

        /// <summary>Skips the fade-in (the intro was skipped).</summary>
        public void Clear()
        {
            if (Busy || group == null) return;
            StopAllCoroutines();
            group.alpha = 0f;
        }

        private IEnumerator Fade(float from, float to, float seconds, Action then)
        {
            Busy = then != null;
            if (group != null) group.blocksRaycasts = to > 0.5f;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                if (group != null) group.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }

            if (group != null) group.alpha = to;
            then?.Invoke();
        }
    }
}
