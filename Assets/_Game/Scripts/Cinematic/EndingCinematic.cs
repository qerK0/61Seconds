using System;
using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// The ending, played when the family reaches the Acil Toplanma Alanı sign (before the epilogue). With the config's
    /// video: prepared during the fade to black while music and ambience fade out (0.5 s), then played full-screen on a
    /// RawImage (aspect kept, black letterbox) with its own audio at full volume, then a 0.5 s fade to black. Without a
    /// video: the config's frames at its fps with cue_ending (else the outro music); without frames: one centred line
    /// (ui.ending.fallback), 3 s. Click / Space / Enter / Esc skips after 1 s. A ScreenHold stops every timer and blocks
    /// all other input; nothing can kill (GameManager ignores deaths), so no rewind can start. Unscaled time.
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

        [Tooltip("Music and ambience fade-out before the video, and the video's fade to black at its end (seconds).")]
        [SerializeField] private float videoFadeSeconds = 0.5f;

        [Tooltip("Longest wait for the video to prepare before the frames / text fallback plays instead (seconds).")]
        [SerializeField] private float prepareTimeoutSeconds = 10f;

        private VideoPlayer player;
        private RawImage screen;
        private AspectRatioFitter fitter;
        private AudioSource videoAudio;
        private RenderTexture target;
        private bool held;
        private bool silenced;
        private bool prepared;
        private bool failed;
        private bool ended;

        private void Awake()
        {
            IsPlaying = false;
            Show(0f);
        }

        private void OnDestroy()
        {
            IsPlaying = false;
            if (silenced) AudioListener.pause = false;
            if (held) ScreenHold.End();
            ReleaseVideo();
        }

        /// <summary>Plays the ending, then calls done.</summary>
        public void Play(Action done)
        {
            StopAllCoroutines();
            StartCoroutine(Run(done));
        }

        private IEnumerator Run(Action done)
        {
            IsPlaying = true;
            if (!held)
            {
                held = true;
                ScreenHold.Begin();
            }

            if (frame != null) frame.enabled = false;
            if (fallbackText != null) fallbackText.alpha = 0f;

            VideoClip clip = config != null ? config.Video : null;
            bool played = false;
            if (clip != null) yield return PlayVideo(clip, result => played = result);

            if (!played)
            {
                for (float t = root != null ? root.alpha * fadeSeconds : 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
                {
                    Show(t / fadeSeconds);
                    yield return null;
                }

                Show(1f);
                yield return PlayFramesOrText();
            }

            // Back to the street for the epilogue text; the end screen follows it.
            if (frame != null) frame.enabled = false;
            if (fallbackText != null) fallbackText.alpha = 0f;
            IsPlaying = false;
            Release();
            done?.Invoke();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                Show(1f - t / 0.4f);
                yield return null;
            }

            Show(0f);
        }

        private IEnumerator PlayVideo(VideoClip clip, Action<bool> result)
        {
            SetUpVideo(clip);
            prepared = failed = ended = false;
            player.Prepare();

            // Fade to black (and the other audio out) while the clip prepares, so it starts without a black flash.
            List<AudioSource> others = PlayingSources();
            var volumes = new float[others.Count];
            for (int i = 0; i < others.Count; i++) volumes[i] = others[i].volume;
            float fade = Mathf.Max(fadeSeconds, videoFadeSeconds);
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                Show(t / Mathf.Max(0.01f, fadeSeconds));
                float k = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, videoFadeSeconds));
                for (int i = 0; i < others.Count; i++) if (others[i] != null) others[i].volume = volumes[i] * k;
                yield return null;
            }

            Show(1f);
            SilenceOthers(true);
            for (float t = 0f; !prepared && !failed && t < prepareTimeoutSeconds; t += Time.unscaledDeltaTime) yield return null;

            if (!prepared || failed)
            {
                Debug.LogWarning($"[Ending] Video '{clip.name}' could not be prepared; playing the fallback instead.", this);
                ReleaseVideo();
                SilenceOthers(false);
                result(false);
                yield break;
            }

            fitter.aspectRatio = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;
            screen.color = Color.white;
            screen.enabled = true;
            videoAudio.volume = 1f;
            player.Play();

            float elapsed = 0f;
            float limit = (float)clip.length + 2f;
            while (!ended && elapsed < limit)
            {
                if (elapsed >= skipGuardSeconds && Pressed()) break;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            for (float t = 0f; t < videoFadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / Mathf.Max(0.01f, videoFadeSeconds);
                screen.color = new Color(1f, 1f, 1f, k);
                videoAudio.volume = k;
                yield return null;
            }

            ReleaseVideo();
            result(true);
        }

        private IEnumerator PlayFramesOrText()
        {
            Sprite[] frames = config != null ? config.Frames : null;
            if (frames != null && frames.Length > 0)
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
                    elapsed += Time.unscaledDeltaTime;
                }

                yield break;
            }

            if (fallbackText != null) fallbackText.text = TextDB.GetDisplay(UiKeys.EndingFallback);
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                if (fallbackText != null) fallbackText.alpha = t / 0.5f;
                yield return null;
            }

            float shown = 0f;
            while (shown < fallbackHoldSeconds && !(shown >= skipGuardSeconds && Pressed()))
            {
                if (fallbackText != null) fallbackText.alpha = 1f;
                yield return null;
                shown += Time.unscaledDeltaTime;
            }
        }

        /// <summary>Builds the RawImage + VideoPlayer + AudioSource once (on the black overlay) and loads the clip.</summary>
        private void SetUpVideo(VideoClip clip)
        {
            if (player == null)
            {
                var go = new GameObject("Video", typeof(RectTransform));
                go.transform.SetParent(root != null ? root.transform : transform, false);
                go.transform.SetSiblingIndex(frame != null ? frame.transform.GetSiblingIndex() + 1 : 0);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                screen = go.AddComponent<RawImage>();
                screen.raycastTarget = false;
                fitter = go.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                videoAudio = go.AddComponent<AudioSource>();
                videoAudio.playOnAwake = false;
                videoAudio.spatialBlend = 0f;
                videoAudio.ignoreListenerPause = true;
                player = go.AddComponent<VideoPlayer>();
                player.playOnAwake = false;
                player.prepareCompleted += _ => prepared = true;
                player.errorReceived += (_, message) =>
                {
                    failed = true;
                    Debug.LogWarning("[Ending] Video error: " + message, this);
                };
                player.loopPointReached += _ => ended = true;
            }

            screen.enabled = false;
            target = new RenderTexture(Mathf.Max(16, (int)clip.width), Mathf.Max(16, (int)clip.height), 0);
            target.Create();
            screen.texture = target;
            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.isLooping = false;
            player.skipOnDrop = true;
            player.waitForFirstFrame = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, videoAudio);
            videoAudio.volume = 1f;
        }

        private void ReleaseVideo()
        {
            if (player != null) player.Stop();
            if (screen != null)
            {
                screen.enabled = false;
                screen.texture = null;
            }

            if (target != null)
            {
                target.Release();
                Destroy(target);
                target = null;
            }
        }

        /// <summary>Every other audio source that is playing now (music, ambience, loops).</summary>
        private List<AudioSource> PlayingSources()
        {
            var list = new List<AudioSource>();
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                if (source != videoAudio && source.isPlaying && !source.ignoreListenerPause) list.Add(source);
            }

            return list;
        }

        /// <summary>Pauses all other audio for the video (its own source ignores the listener pause).</summary>
        private void SilenceOthers(bool on)
        {
            if (!on)
            {
                // Faded music and ambience stay off; the epilogue and end screen start their own sounds.
                foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                {
                    if (source != videoAudio && !source.ignoreListenerPause) source.Stop();
                }
            }

            silenced = on;
            AudioListener.pause = on;
        }

        private void Release()
        {
            if (silenced) SilenceOthers(false);
            if (!held) return;
            held = false;
            ScreenHold.End();
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
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame);
        }
    }
}
