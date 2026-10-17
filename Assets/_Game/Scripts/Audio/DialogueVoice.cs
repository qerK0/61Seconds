using SixtyOne.Dialogue;
using SixtyOne.UI;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// While the typewriter reveals a dialogue line, plays short random slices of the speaker's recorded voice every
    /// 2-3 letters (spaces and punctuation skipped), at the speaker's pitch. Stops when the line ends or is skipped.
    /// </summary>
    public sealed class DialogueVoice : MonoBehaviour
    {
        [Tooltip("Typewriter on the dialogue body.")]
        [SerializeField] private TypewriterText typewriter;

        [Tooltip("Speaker voices.")]
        [SerializeField] private DialogueVoiceProfile profile;

        private AudioSource source;
        private string speaker;
        private int lettersUntilBlip;
        private float stopAt;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
        }

        private void OnEnable()
        {
            DialogueController.SpeakerChanged += OnSpeakerChanged;
            if (typewriter == null) return;
            typewriter.CharRevealed += OnCharRevealed;
            typewriter.RevealEnded += StopVoice;
        }

        private void OnDisable()
        {
            DialogueController.SpeakerChanged -= OnSpeakerChanged;
            if (typewriter == null) return;
            typewriter.CharRevealed -= OnCharRevealed;
            typewriter.RevealEnded -= StopVoice;
        }

        private void Update()
        {
            if (source.isPlaying && Time.unscaledTime >= stopAt) source.Stop();
        }

        private void OnSpeakerChanged(string speakerKey)
        {
            speaker = speakerKey;
            lettersUntilBlip = 0;
            if (speakerKey == null) StopVoice();
        }

        private void OnCharRevealed(char c)
        {
            if (profile == null || speaker == null || !char.IsLetterOrDigit(c)) return;
            if (--lettersUntilBlip > 0) return;
            Vector2Int every = profile.LettersPerBlip;
            lettersUntilBlip = Random.Range(Mathf.Max(1, every.x), Mathf.Max(every.x, every.y) + 1);
            if (!profile.TryGet(speaker, out DialogueVoiceProfile.Voice voice)) return;

            float length = Random.Range(profile.BlipSeconds.x, profile.BlipSeconds.y);
            AudioClip clip = voice.clip;
            source.clip = clip;
            source.volume = profile.Volume;
            source.pitch = (voice.pitch > 0f ? voice.pitch : 1f) + Random.Range(-profile.PitchJitter, profile.PitchJitter);
            source.time = Random.Range(0f, Mathf.Max(0f, clip.length - length - 0.01f));
            source.Play();
            stopAt = Time.unscaledTime + length / Mathf.Max(0.1f, source.pitch);
        }

        private void StopVoice()
        {
            if (source != null) source.Stop();
        }
    }
}
