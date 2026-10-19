using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// Looping background layers: dripping water in the damaged building (every indoor room after the quake), the
    /// frightened dog's growl in its corridor until it is gone (one bark when the player walks in), and the heartbeat,
    /// whose level follows panic above the click-delay threshold, exhaustion and poison.
    /// </summary>
    public sealed class AmbienceService : MonoBehaviour
    {
        [Tooltip("State owner. Defaults to GameManager.Instance.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current node (epilogue = outside). Found in the scene if empty.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Panic source for the heartbeat. Found in the scene if empty.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Damaged-building loop (after the quake, indoors).")]
        [SerializeField] private AudioCue damagedBuilding;

        [Tooltip("Dog growl loop.")]
        [SerializeField] private AudioCue dogGrowl;

        [Tooltip("One bark when the player enters the dog's room.")]
        [SerializeField] private AudioCue dogBark;

        [Tooltip("Room the dog is in.")]
        [SerializeField] private string dogRoomId = "f2_corridor";

        [Tooltip("Flag set once the dog is gone (fed, called away or passed).")]
        [SerializeField] private string dogGoneFlag = "DogGone";

        [Tooltip("Heartbeat loop.")]
        [SerializeField] private AudioCue heartbeat;

        [Tooltip("Fade speed (volume units per second).")]
        [SerializeField] private float fadePerSecond = 0.6f;

        private AudioLoopLayer ambience;
        private AudioLoopLayer growl;
        private AudioLoopLayer heart;
        private bool dogWasThere;

        private void Awake()
        {
            ambience = new AudioLoopLayer(transform, "Ambience");
            growl = new AudioLoopLayer(transform, "Dog Growl");
            heart = new AudioLoopLayer(transform, "Heartbeat");
        }

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (nodePlayer == null) nodePlayer = FindAnyObjectByType<NodePlayer>();
            if (survival == null) survival = FindAnyObjectByType<SurvivalController>();
        }

        private void Update()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm == null) return;
            GameState state = gm.State;
            float dt = Time.unscaledDeltaTime;
            bool outside = nodePlayer != null && nodePlayer.Current is EpilogueNode;

            bool damaged = !outside && state.Flags.Get(FlagNames.QuakeHappened);
            ambience.Tick(damagedBuilding, damaged ? 1f : 0f, fadePerSecond, dt);

            bool dogThere = !outside && !gm.IsDead && state.CurrentRoomId == dogRoomId && !state.Flags.Get(dogGoneFlag);
            if (dogThere && !dogWasThere) AudioService.PlayCue(dogBark);
            dogWasThere = dogThere;
            growl.Tick(dogGrowl, dogThere ? 1f : 0f, fadePerSecond * 2f, dt);

            heart.Tick(heartbeat, outside ? 0f : HeartbeatLevel(gm), fadePerSecond, dt);
        }

        private float HeartbeatLevel(GameManager gm)
        {
            float threshold = gm.Settings != null ? gm.Settings.PanicDelayThreshold : 0.7f;
            float panic = survival != null ? survival.Panic01 : 0f;
            float fromPanic = panic > threshold && threshold < 1f ? (panic - threshold) / (1f - threshold) : 0f;
            return Mathf.Clamp01(Mathf.Max(fromPanic, Mathf.Max(gm.State.Exhaustion, gm.State.Poison)));
        }
    }
}
