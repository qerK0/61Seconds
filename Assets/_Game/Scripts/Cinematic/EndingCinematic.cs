using System;
using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// The ending, played when the family reaches the Acil Toplanma Alanı sign (before the epilogue): the config's
    /// frames full-screen (aspect kept, black letterbox) at its fps with cue_ending (else the outro music); a click /
    /// Space / Enter skips after 1 s. Without frames: fade to black, one centred line (ui.ending.fallback), 3 s.
    /// While it plays nothing can kill (GameManager ignores deaths), so no rewind can start. Unscaled time.
    /// </summary>
    public sealed class EndingCinematic : MonoBehaviour
    {
        /// <summary>True while the ending plays.</summary>
        public static bool IsPlaying { get; private set; }

        [SerializeField] private EndingCinematicConfig config;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text fallbackText;

        [Tooltip("Played when cue_ending has no clip.")]
        [SerializeField] private AudioCue outroMusic;

        [SerializeField] private float skipGuardSeconds = 1f;
        [SerializeField] private float fadeSeconds = 1f;
        [SerializeField] private float fallbackHoldSeconds = 3f;

        private void Awake()
        {
            IsPlaying = false;
            Show(0f);
        }

        private void OnDestroy() => IsPlaying = false;

        /// <summary>Plays the ending, then calls done.</summary>
        public void Play(Action done)
        {
            StopAllCoroutines();
            StartCoroutine(Run(done));
        }

        private IEnumerator Run(Action done)
        {
            IsPlaying = true;
            Sprite[] frames = config != null ? config.Frames : null;
            bool hasFrames = frames != null && frames.Length > 0;
            if (frame != null) frame.enabled = false;
            if (fallbackText != null) fallbackText.alpha = 0f;

            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                Show(t / fadeSeconds);
                yield return null;
            }

            Show(1f);
            if (hasFrames)
            {
                AudioCue cue = config.Cue != null && config.Cue.HasClips ? config.Cue : outroMusic;
                if (cue != null && AudioService.Instance != null)
                {
                    if (cue == outroMusic) AudioService.Instance.PlayMusic(cue);
                    else AudioService.PlayCue(cue);
                }

                frame.enabled = true;
                float elapsed = 0f;
                float length = frames.Length / config.Fps;
                while (elapsed < length)
                {
                    int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(elapsed * config.Fps));
                    frame.sprite = frames[index];
                    if (elapsed >= skipGuardSeconds && Pressed()) break;
                    yield return null;
                    if (!PauseState.IsPaused) elapsed += Time.unscaledDeltaTime;
                }
            }
            else
            {
                if (fallbackText != null) fallbackText.text = TextDB.GetDisplay(UiKeys.EndingFallback);
                for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
                {
                    if (fallbackText != null) fallbackText.alpha = t / 0.5f;
                    yield return null;
                }

                float held = 0f;
                while (held < fallbackHoldSeconds && !(held >= skipGuardSeconds && Pressed()))
                {
                    if (fallbackText != null) fallbackText.alpha = 1f;
                    yield return null;
                    if (!PauseState.IsPaused) held += Time.unscaledDeltaTime;
                }
            }

            // Back to the street for the epilogue text; the end screen follows it.
            if (frame != null) frame.enabled = false;
            if (fallbackText != null) fallbackText.alpha = 0f;
            IsPlaying = false;
            done?.Invoke();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                Show(1f - t / 0.4f);
                yield return null;
            }

            Show(0f);
        }

        private void Show(float alpha)
        {
            if (root == null) return;
            root.alpha = alpha;
            root.blocksRaycasts = alpha > 0.01f;
            root.interactable = false;
        }

        private static bool Pressed()
        {
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (m != null && m.leftButton.wasPressedThisFrame);
        }
    }
}
