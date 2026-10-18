using System;
using System.Collections;
using SixtyOne.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SixtyOne.UI
{
    /// <summary>
    /// Main menu intro (unscaled): the fader fades in from black, the title slams in (1.15 → 1 with a short shake, a
    /// thud and a dust puff), then the buttons slide in from the left one after another. Any key or click skips to
    /// the final layout.
    /// </summary>
    public sealed class MenuIntro : MonoBehaviour
    {
        [SerializeField] private ScreenFader fader;
        [SerializeField] private RectTransform title;
        [SerializeField] private CanvasGroup titleGroup;

        [Tooltip("Dust puff behind the title (scaled out and faded).")]
        [SerializeField] private CanvasGroup dust;

        [Tooltip("Buttons and the hazard stripe, in slide-in order.")]
        [SerializeField] private RectTransform[] slides = new RectTransform[0];

        [SerializeField] private AudioCue slamCue;

        [Tooltip("Seconds before the title slams (the fade-in runs meanwhile).")]
        [SerializeField] private float titleDelay = 0.45f;

        [SerializeField] private float stagger = 0.09f;

        private Vector2[] slideTargets;
        private CanvasGroup[] slideGroups;
        private bool done;

        /// <summary>Raised when the intro ends (played through or skipped): the menu selects its first button.</summary>
        public event Action Finished;

        public bool Done => done;

        public void Configure(ScreenFader screenFader, RectTransform titleRect, CanvasGroup titleCanvas, CanvasGroup dustPuff, RectTransform[] slideIns, AudioCue slam)
        {
            fader = screenFader;
            title = titleRect;
            titleGroup = titleCanvas;
            dust = dustPuff;
            slides = slideIns;
            slamCue = slam;
        }

        private void Awake()
        {
            slideTargets = new Vector2[slides.Length];
            slideGroups = new CanvasGroup[slides.Length];
            for (int i = 0; i < slides.Length; i++)
            {
                slideTargets[i] = slides[i].anchoredPosition;
                slideGroups[i] = slides[i].GetComponent<CanvasGroup>();
                if (slideGroups[i] == null) slideGroups[i] = slides[i].gameObject.AddComponent<CanvasGroup>();
            }

            Prepare();
        }

        private void Start() => StartCoroutine(Play());

        private void Prepare()
        {
            if (titleGroup != null) titleGroup.alpha = 0f;
            if (dust != null) dust.alpha = 0f;
            for (int i = 0; i < slides.Length; i++)
            {
                slides[i].anchoredPosition = slideTargets[i] + new Vector2(-520f, 0f);
                slideGroups[i].alpha = 0f;
            }
        }

        private IEnumerator Play()
        {
            yield return null;
            float t = 0f;
            while (t < titleDelay)
            {
                if (SkipPressed()) { Finish(); yield break; }
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            AudioService.PlayUiCue(slamCue);
            UiShake.Kick(0.8f, 0.3f);
            const float slam = 0.22f;
            for (t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) { Finish(); yield break; }
                float u = Mathf.Clamp01(t / slam);
                float s = Mathf.Lerp(1.15f, 1f, 1f - (1f - u) * (1f - u));
                if (title != null) title.localScale = new Vector3(s, s, 1f);
                if (titleGroup != null) titleGroup.alpha = u;
                if (dust != null)
                {
                    float d = Mathf.Clamp01(t / 0.9f);
                    dust.alpha = d < 0.15f ? d / 0.15f * 0.6f : 0.6f * (1f - d);
                    dust.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.5f, d);
                }

                float since = t - 0.3f;
                for (int i = 0; i < slides.Length; i++) Slide(i, since - i * stagger);
                yield return null;
            }

            for (t = 0.6f; t < 0.6f + 0.35f + slides.Length * stagger; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) { Finish(); yield break; }
                for (int i = 0; i < slides.Length; i++) Slide(i, t - i * stagger);
                yield return null;
            }

            Finish();
        }

        private void Slide(int i, float t)
        {
            float u = Mathf.Clamp01(t / 0.35f);
            u = 1f - (1f - u) * (1f - u) * (1f - u);
            slides[i].anchoredPosition = slideTargets[i] + new Vector2(-520f * (1f - u), 0f);
            slideGroups[i].alpha = u;
        }

        private void Finish()
        {
            if (done) return;
            done = true;
            if (fader != null) fader.Clear();
            if (title != null) title.localScale = Vector3.one;
            if (titleGroup != null) titleGroup.alpha = 1f;
            if (dust != null) dust.alpha = 0f;
            for (int i = 0; i < slides.Length; i++)
            {
                slides[i].anchoredPosition = slideTargets[i];
                slideGroups[i].alpha = 1f;
            }

            Finished?.Invoke();
        }

        private static bool SkipPressed()
        {
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.anyKey.wasPressedThisFrame) || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame))
                || (g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
        }
    }
}
