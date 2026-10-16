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
        public AudioSource Play(AudioCue cue)
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
            music.loop = true;
            music.Play();
            playing[music] = cue;
        }

        public void StopMusic()
        {
            if (music == null) return;
            music.Stop();
            playing.Remove(music);
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
            source.volume = cue.Volume;
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
