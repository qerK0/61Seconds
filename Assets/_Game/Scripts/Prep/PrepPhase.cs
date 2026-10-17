using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// Runs the pre-quake prep countdown (GameState.PrepTime) while the current node is a PrepPhaseNode and the
    /// building timer has not started: real-time drain, action costs (SurvivalController forwards them here), the
    /// last-seconds ticks and the AFAD alert. At zero: the building timer starts, the quake onset plays and the
    /// linked placement node is entered (next frame, never inside a choice that is still executing).
    /// </summary>
    public sealed class PrepPhase : MonoBehaviour
    {
        [Tooltip("Owner of the game state.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Enters the placement node at zero.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Prep seconds, tick window and tick cue.")]
        [SerializeField] private LevelConfig config;

        [Tooltip("Shows the prep rooms; their hazard props are reset when prep starts (after a rewind they may still lie toppled).")]
        [SerializeField] private SixtyOne.Placement.RoomSwitcher rooms;

        [Tooltip("Played when the AFAD early warning appears.")]
        [SerializeField] private AudioCue alertCue;

        private bool pendingQuake;

        public PrepTimer Timer { get; } = new PrepTimer();

        /// <summary>True while the countdown runs (prep room shown, quake not started).</summary>
        public bool IsActive => gameManager != null && !gameManager.State.BuildingTimerStarted && nodePlayer != null && nodePlayer.Current is PrepPhaseNode;

        public float Remaining => gameManager != null ? gameManager.State.PrepTime : 0f;

        private GameState State => gameManager.State;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (config != null) Timer.Configure(config.PrepSeconds, config.PrepTickFrom);
            Timer.SecondTick += OnSecondTick;
            Timer.Zero += () => pendingQuake = true;
        }

        private void OnEnable()
        {
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.Rewound -= OnRewound;
        }

        private void Update()
        {
            if (!IsActive || gameManager.IsDead) return;
            if (pendingQuake)
            {
                StartQuake();
                return;
            }

            // Room transitions (tile rise/drop) never cost prep time.
            State.PrepTime = Timer.Advance(State.PrepTime, NavigationEvents.TimerDelta(Time.deltaTime));
        }

        /// <summary>Spends prep seconds on an action (pickup, drawer, valve, move). Ignored outside prep.</summary>
        public void Spend(float seconds)
        {
            if (!IsActive || seconds <= 0f) return;
            State.PrepTime = Timer.Advance(State.PrepTime, seconds);
            GameEvents.RaiseTimeSpent(seconds);
        }

        private void OnNodeEntered(string nodeId)
        {
            Timer.Sync(State.PrepTime);
            if (!(nodePlayer.Current is PrepPhaseNode prep) || State.BuildingTimerStarted) return;
            // The node's own room: during a room transition rooms.Current is still the previous room.
            SixtyOne.Placement.Room room = rooms != null ? rooms.FindRoom(prep.RoomId) : null;
            if (room != null) room.ResetProps();
            if (!prep.ShowAlertOnEnter) return;
            GameEvents.RaiseAlert(prep.AlertTitleKey.Key, prep.AlertBodyKey.Key, Mathf.CeilToInt(State.PrepTime).ToString());
            if (!AudioService.IsCuePlaying(alertCue)) AudioService.PlayCue(alertCue);
        }

        private void OnRewound(string nodeId)
        {
            pendingQuake = false;
            Timer.Sync(State.PrepTime);
        }

        private void OnSecondTick(int secondsLeft)
        {
            GameEvents.RaisePrepTick(secondsLeft);
            if (config != null) AudioService.PlayCue(config.TickCue);
        }

        private void StartQuake()
        {
            pendingQuake = false;
            var prep = (PrepPhaseNode)nodePlayer.Current;
            PrepRules.StartBuildingTimer(State);
            GameEvents.RaisePrepZero();

            SixtyOne.Placement.PlacementNode quake = prep.QuakeNode;
            if (quake == null)
            {
                Debug.LogError($"[Prep] '{prep.Id}' has no quake node.", this);
                return;
            }

            // Onset rumble now; the placement's own quake (hazards) follows when its timer ends.
            GameEvents.RaiseQuakeStarted(new QuakeInfo(quake.Act, quake.TimerSeconds, quake.Magnitude, prep.OnsetShakeScale));
            nodePlayer.Enter(quake);
        }
    }
}
