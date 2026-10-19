using UnityEngine;
using UnityEngine.Audio;

namespace SixtyOne.Audio
{
    /// <summary>A playable sound: random clip, volume, pitch range, mixer group. Sound designer swaps clips here.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Audio Cue", fileName = "cue_new")]
    public sealed class AudioCue : ScriptableObject
    {
        [Tooltip("Unique id for AudioService.PlayById.")]
        [SerializeField] private string id;

        [Tooltip("One is picked at random each play. Empty = silent (warns once).")]
        [SerializeField] private AudioClip[] clips = new AudioClip[0];

        [Tooltip("Playback volume.")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("Random pitch between x and y.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);

        [Tooltip("Optional mixer group to route through.")]
        [SerializeField] private AudioMixerGroup mixerGroup;

        [Tooltip("Loop until stopped (rumbles, music).")]
        [SerializeField] private bool loop;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public float Volume => volume;
        public AudioMixerGroup MixerGroup => mixerGroup;
        public bool Loop => loop;

        /// <summary>True if at least one non-null clip is assigned.</summary>
        public bool HasClips
        {
            get
            {
                if (clips == null) return false;
                foreach (AudioClip clip in clips)
                {
                    if (clip != null) return true;
                }
                return false;
            }
        }

        /// <summary>Random non-null clip, or null.</summary>
        public AudioClip PickClip()
        {
            if (!HasClips) return null;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                if (clip != null) return clip;
            }
            foreach (AudioClip clip in clips)
            {
                if (clip != null) return clip;
            }
            return null;
        }

        /// <summary>Random pitch within the range.</summary>
        public float PickPitch()
        {
            return Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
        }

        /// <summary>Authoring helper: replaces the clips (the audio import menu).</summary>
        public void SetClips(params AudioClip[] newClips)
        {
            clips = newClips ?? new AudioClip[0];
        }

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string cueId, float cueVolume = 1f, bool cueLoop = false, float pitchMin = 1f, float pitchMax = 1f)
        {
            id = cueId;
            volume = cueVolume;
            loop = cueLoop;
            pitchRange = new Vector2(pitchMin, pitchMax);
        }
    }
}
