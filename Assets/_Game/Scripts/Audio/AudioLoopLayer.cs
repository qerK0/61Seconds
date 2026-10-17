using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// One dedicated looping AudioSource that fades toward a target level (0..1 of the cue volume) every frame.
    /// Starts the clip when the level rises above 0 and stops it once it has faded out. Used for ambience,
    /// the dog growl, the heartbeat and footsteps.
    /// </summary>
    public sealed class AudioLoopLayer
    {
        private readonly AudioSource source;
        private AudioCue cue;

        public AudioLoopLayer(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;
        }

        public bool IsPlaying => source != null && source.isPlaying;

        /// <summary>Fades toward cue.Volume * level at fadePerSecond (in volume units). Null cue = fade out.</summary>
        public void Tick(AudioCue target, float level, float fadePerSecond, float dt)
        {
            if (source == null) return;
            if (target != cue && source.isPlaying && source.volume > 0.01f) level = 0f; // fade out the old cue first
            else if (target != cue)
            {
                source.Stop();
                cue = target;
            }

            float goal = cue != null ? cue.Volume * Mathf.Clamp01(level) * SixtyOne.Core.UserSettings.Sfx : 0f;
            if (goal > 0.001f && !source.isPlaying)
            {
                AudioClip clip = cue.PickClip();
                if (clip == null) return;
                source.clip = clip;
                source.pitch = cue.PickPitch();
                source.outputAudioMixerGroup = cue.MixerGroup;
                source.volume = 0f;
                source.time = Random.Range(0f, clip.length * 0.5f);
                source.Play();
            }

            source.volume = Mathf.MoveTowards(source.volume, goal, Mathf.Max(0.01f, fadePerSecond) * dt);
            if (goal <= 0.001f && source.volume <= 0.001f && source.isPlaying) source.Stop();
        }

        /// <summary>Stops at once (rewinds).</summary>
        public void StopNow()
        {
            if (source == null) return;
            source.Stop();
            source.volume = 0f;
        }
    }
}
