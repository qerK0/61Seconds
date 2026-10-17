using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// Music: two AudioSources that crossfade (unscaled time). Prep = calm track; a quake stops the music; after a
    /// quake the tension track plays; the street / epilogue plays the outro. Driven by GameEvents and the game state,
    /// so a rewind to a checkpoint restarts the right track for that stage.
    /// </summary>
    public sealed class MusicService : MonoBehaviour
    {
        [Tooltip("State owner. Defaults to GameManager.Instance.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current node (epilogue). Found in the scene if empty.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Calm music before the quake (prep).")]
        [SerializeField] private AudioCue prepMusic;

        [Tooltip("Tension music after the opening quake.")]
        [SerializeField] private AudioCue tensionMusic;

        [Tooltip("Street / epilogue.")]
        [SerializeField] private AudioCue outroMusic;

        [Tooltip("Crossfade seconds between tracks.")]
        [SerializeField] private float crossfadeSeconds = 1.5f;

        [Tooltip("Fade-out seconds when a quake hits.")]
        [SerializeField] private float quakeFadeSeconds = 0.6f;

        private readonly AudioSource[] sources = new AudioSource[2];
        private readonly AudioCue[] sourceCue = new AudioCue[2];
        private int activeIndex;
        private MusicTrack current = (MusicTrack)(-1);
        private bool quaking;

        private void Awake()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var go = new GameObject("Music " + (char)('A' + i));
                go.transform.SetParent(transform, false);
                AudioSource source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = true;
                source.volume = 0f;
                sources[i] = source;
            }
        }

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (nodePlayer == null) nodePlayer = FindAnyObjectByType<NodePlayer>();
        }

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.QuakeEnded += OnQuakeEnded;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.QuakeEnded -= OnQuakeEnded;
            GameEvents.Rewound -= OnRewound;
        }

        private void OnQuakeStarted(QuakeInfo info) => quaking = true;
        private void OnQuakeEnded(QuakeInfo info) => quaking = false;

        private void OnRewound(string nodeId)
        {
            quaking = false;
            // Force a restart of the stage's track from the top.
            current = (MusicTrack)(-1);
            for (int i = 0; i < sources.Length; i++) sourceCue[i] = null;
        }

        private void Update()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm == null) return;
            bool epilogue = nodePlayer != null && nodePlayer.Current is EpilogueNode;
            MusicTrack wanted = MusicRules.TrackFor(epilogue, quaking, gm.State.BuildingTimerStarted);
            if (wanted != current)
            {
                current = wanted;
                AudioCue cue = CueFor(wanted);
                if (cue != null && cue != sourceCue[activeIndex])
                {
                    activeIndex = 1 - activeIndex;
                    AudioClip clip = cue.PickClip();
                    AudioSource source = sources[activeIndex];
                    sourceCue[activeIndex] = clip != null ? cue : null;
                    if (clip != null)
                    {
                        source.clip = clip;
                        source.loop = cue.Loop;
                        source.pitch = cue.PickPitch();
                        source.outputAudioMixerGroup = cue.MixerGroup;
                        source.volume = 0f;
                        source.Play();
                    }
                }
                else if (cue == null)
                {
                    sourceCue[activeIndex] = null;
                }
            }

            float fade = current == MusicTrack.None ? quakeFadeSeconds : crossfadeSeconds;
            float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, fade);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                float goal = i == activeIndex && sourceCue[i] != null ? sourceCue[i].Volume * SixtyOne.Core.UserSettings.Music : 0f;
                source.volume = Mathf.MoveTowards(source.volume, goal, step);
                if (goal <= 0f && source.volume <= 0.001f && source.isPlaying) source.Stop();
            }
        }

        private AudioCue CueFor(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Prep: return prepMusic;
                case MusicTrack.Tension: return tensionMusic;
                case MusicTrack.Outro: return outroMusic;
                default: return null;
            }
        }
    }
}
