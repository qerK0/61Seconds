using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>Speaker key → recorded voice clip + pitch for the "animalese" dialogue blips.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Dialogue Voice Profile", fileName = "DialogueVoices")]
    public sealed class DialogueVoiceProfile : ScriptableObject
    {
        /// <summary>One speaker's voice.</summary>
        [Serializable]
        public struct Voice
        {
            [Tooltip("Speaker text key (e.g. npc.sevim.name).")]
            public string speakerKey;

            [Tooltip("Recorded speech; random short windows are sliced out of it.")]
            public AudioClip clip;

            [Tooltip("Pitch (1.15 higher, 0.85 lower).")]
            public float pitch;
        }

        [Tooltip("Voices by speaker key.")]
        [SerializeField] private List<Voice> voices = new List<Voice>();

        [Tooltip("Blip volume.")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.8f;

        [Tooltip("Blip length range in seconds.")]
        [SerializeField] private Vector2 blipSeconds = new Vector2(0.08f, 0.15f);

        [Tooltip("A blip every N revealed letters (random in range, inclusive).")]
        [SerializeField] private Vector2Int lettersPerBlip = new Vector2Int(2, 3);

        [Tooltip("Random pitch jitter (+/-).")]
        [SerializeField] private float pitchJitter = 0.04f;

        public float Volume => volume;
        public Vector2 BlipSeconds => blipSeconds;
        public Vector2Int LettersPerBlip => lettersPerBlip;
        public float PitchJitter => pitchJitter;

        /// <summary>The speaker's voice, or false.</summary>
        public bool TryGet(string speakerKey, out Voice voice)
        {
            foreach (Voice v in voices)
            {
                if (v.clip != null && v.speakerKey == speakerKey)
                {
                    voice = v;
                    return true;
                }
            }

            voice = default;
            return false;
        }

        /// <summary>Authoring helper for the audio import menu.</summary>
        public void Configure(IEnumerable<Voice> speakerVoices, float blipVolume)
        {
            voices = new List<Voice>(speakerVoices);
            volume = blipVolume;
        }
    }
}
