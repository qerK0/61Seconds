using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// Pooled sound playback plus one music layer. Missing clips log a warning once and stay silent.
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class AudioService : MonoBehaviour
    {
        [Tooltip("Number of pooled one-shot/loop sources.")]
        [SerializeField] private int poolSize = 12;

        [Tooltip("Cues playable by id (PlayById).")]
        [SerializeField] private List<AudioCue> library = new List<AudioCue>();

        /// <summary>Active instance in the scene.</summary>
        public static AudioService Instance { get; private set; }

        private readonly List<AudioSource> pool = new List<AudioSource>();
        private readonly Dictionary<AudioSource, AudioCue> playing = new Dictionary<AudioSource, AudioCue>();
        private readonly HashSet<string> warned = new HashSet<string>();
        private AudioSource music;
        private int nextIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioService] Second instance found; ignoring it.", this);
                return;
            }

            Instance = this;
            for (int i = 0; i < Mathf.Max(1, poolSize); i++)
            {
                pool.Add(CreateSource("Sfx " + i));
            }

            music = CreateSource("Music");
            music.loop = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Plays a cue if an AudioService exists. Safe with null cues.</summary>
        public static void PlayCue(AudioCue cue)
        {
            if (Instance != null && cue != null)
            {
                Instance.Play(cue);
            }
        }

        /// <summary>Stops a cue if an AudioService exists.</summary>
        public static void StopCue(AudioCue cue)
        {
            if (Instance != null && cue != null)
            {
                Instance.Stop(cue);
            }
        }

        /// <summary>Adds cues to the id library.</summary>
        public void Register(IEnumerable<AudioCue> cues)
        {
            foreach (AudioCue cue in cues)
            {
                if (cue != null && !library.Contains(cue)) library.Add(cue);
            }
        }

        /// <summary>Plays a cue on a pooled source. Returns the source or null when silent.</summary>
        public AudioSource Play(AudioCue cue) => Play(cue, false);

        private AudioSource Play(AudioCue cue, bool ignorePause)
        {
            if (cue == null) return null;

            AudioClip clip = cue.PickClip();
            if (clip == null)
            {
                WarnOnce(cue.Id, $"[Audio] Cue '{cue.Id}' has no clips; staying silent.");
                return null;
            }

            AudioSource source = NextSource();
            Configure(source, cue, clip);
            source.ignoreListenerPause = ignorePause;
            source.Play();
            playing[source] = cue;
            return source;
        }

        /// <summary>Plays a library cue by id.</summary>
        public AudioSource PlayById(string id)
        {
            AudioCue cue = library.Find(c => c != null && c.Id == id);
            if (cue == null)
            {
                WarnOnce("id:" + id, $"[Audio] No cue with id '{id}' in the library.");
                return null;
            }

            return Play(cue);
        }

        /// <summary>Plays a cue with its volume and pitch scaled (e.g. a longer, louder macro rewind).</summary>
        public AudioSource Play(AudioCue cue, float volumeScale, float pitchScale)
        {
            AudioSource source = Play(cue);
            if (source == null) return null;
            source.volume = Mathf.Clamp01(cue.Volume * volumeScale) * SixtyOne.Core.UserSettings.Sfx;
            source.pitch *= pitchScale;
            return source;
        }

        /// <summary>Plays a menu / UI cue that still sounds while the game is paused (ignoreListenerPause).</summary>
        public static void PlayUiCue(AudioCue cue)
        {
            if (Instance == null || cue == null) return;
            Instance.Play(cue, true);
        }

        /// <summary>True while any pooled source plays the cue.</summary>
        public bool IsPlaying(AudioCue cue)
        {
            if (cue == null) return false;
            foreach (AudioSource source in pool)
            {
                if (source.isPlaying && playing.TryGetValue(source, out AudioCue current) && current == cue) return true;
            }

            return false;
        }

        /// <summary>Fades out and stops every source playing the cue (unscaled time).</summary>
        public void FadeOut(AudioCue cue, float seconds)
        {
            if (cue == null) return;
            foreach (AudioSource source in pool)
            {
                if (source.isPlaying && playing.TryGetValue(source, out AudioCue current) && current == cue)
                {
                    playing.Remove(source);
                    StartCoroutine(FadeRoutine(source, seconds));
                }
            }
        }

        /// <summary>Fades a cue out if an AudioService exists.</summary>
        public static void FadeOutCue(AudioCue cue, float seconds)
        {
            if (Instance != null && cue != null) Instance.FadeOut(cue, seconds);
        }

        /// <summary>True if an AudioService exists and plays the cue.</summary>
        public static bool IsCuePlaying(AudioCue cue) => Instance != null && Instance.IsPlaying(cue);

        private static IEnumerator FadeRoutine(AudioSource source, float seconds)
        {
            float start = source.volume;
            for (float t = 0f; t < seconds && source.isPlaying; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(start, 0f, t / Mathf.Max(0.01f, seconds));
                yield return null;
            }

            source.Stop();
        }

        /// <summary>Stops every source currently playing the cue.</summary>
        public void Stop(AudioCue cue)
        {
            foreach (AudioSource source in pool)
            {
                if (playing.TryGetValue(source, out AudioCue current) && current == cue)
                {
                    source.Stop();
                    playing.Remove(source);
                }
            }

            if (music != null && music.isPlaying && playing.TryGetValue(music, out AudioCue m) && m == cue)
            {
                StopMusic();
            }
        }

        /// <summary>Plays a looping music cue on the music layer.</summary>
        public void PlayMusic(AudioCue cue)
        {
            if (cue == null) return;
            AudioClip clip = cue.PickClip();
            if (clip == null)
            {
                WarnOnce(cue.Id, $"[Audio] Music cue '{cue.Id}' has no clips; staying silent.");
                return;
            }

            Configure(music, cue, clip);
            music.volume = cue.Volume * SixtyOne.Core.UserSettings.Music;
            music.loop = true;
            music.Play();
            playing[music] = cue;
        }

        /// <summary>Sets the music layer's volume (live settings changes).</summary>
        public void SetMusicVolume(float volume)
        {
            if (music != null) music.volume = Mathf.Clamp01(volume);
        }

        public void StopMusic()
        {
            if (music == null) return;
            music.Stop();
            playing.Remove(music);
        }

        /// <summary>Muffles all audio (low-pass on the AudioListener). Used for the placement window tension.</summary>
        public void SetLowPass(float cutoffHz)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener == null) return;
            AudioLowPassFilter filter = listener.GetComponent<AudioLowPassFilter>();
            if (filter == null) filter = listener.gameObject.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = cutoffHz;
            filter.enabled = true;
        }

        /// <summary>Removes the low-pass muffle.</summary>
        public void ClearLowPass()
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            AudioLowPassFilter filter = listener != null ? listener.GetComponent<AudioLowPassFilter>() : null;
            if (filter != null) filter.enabled = false;
        }

        private AudioSource NextSource()
        {
            foreach (AudioSource source in pool)
            {
                if (!source.isPlaying) return source;
            }

            AudioSource stolen = pool[nextIndex % pool.Count];
            nextIndex++;
            return stolen;
        }

        private static void Configure(AudioSource source, AudioCue cue, AudioClip clip)
        {
            source.clip = clip;
            source.volume = cue.Volume * SixtyOne.Core.UserSettings.Sfx;
            source.ignoreListenerPause = false;
            source.pitch = cue.PickPitch();
            source.loop = cue.Loop;
            source.outputAudioMixerGroup = cue.MixerGroup;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void WarnOnce(string key, string message)
        {
            if (warned.Add(key))
            {
                Debug.LogWarning(message, this);
            }
        }
    }
}
