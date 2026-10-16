using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>
    /// Drives the building timer (real time + action costs). Collapse at 0 kills and rewinds to the floor
    /// checkpoint. Implements IActionCostSink so choices, interactables, dialogue and the bag pay time and stamina.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public sealed class SurvivalController : MonoBehaviour, IActionCostSink
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current node (room for gas zones).")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Timer, stamina, poison and panic settings.")]
        [SerializeField] private LevelConfig config;

        [Tooltip("Dialogue driver (the real-time tick pauses while lines play).")]
        [SerializeField] private DialogueController dialogue;

        public BuildingTimer Timer { get; } = new BuildingTimer();
        public LevelConfig Config => config;
        public float Remaining => gameManager != null ? gameManager.State.BuildingTime : 0f;
        public float Total => Timer.Total;

        private GameState State => gameManager.State;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (config == null)
            {
                Debug.LogError("[Survival] No LevelConfig assigned.", this);
                return;
            }

            Timer.Configure(config.BuildingSeconds, config.PulseAtSeconds, config.AlarmAtSeconds);
            State.BuildingTime = config.BuildingSeconds;
            State.Stamina = config.StaminaMax;
            Timer.Sync(State.BuildingTime);

            Timer.Pulse += GameEvents.RaiseBuildingPulse;
            Timer.Alarm += OnAlarm;
            Timer.AlarmTick += OnAlarmTick;
            Timer.Zero += OnCollapse;
            ActionCostRouter.Sink = this;
        }

        private void OnDestroy()
        {
            if (ActionCostRouter.Sink == (IActionCostSink)this) ActionCostRouter.Sink = null;
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
        }

        private void Update()
        {
            if (config == null || gameManager == null || gameManager.IsDead) return;

            bool paused = config.PauseInDialogue && dialogue != null && dialogue.IsPlayingLines;
            if (config.RealtimeTick && !paused)
            {
                State.BuildingTime = Timer.Advance(State.BuildingTime, Time.deltaTime);
            }
        }

        public void SpendTime(float seconds, bool isMove)
        {
            if (config == null || gameManager == null || gameManager.IsDead || seconds <= 0f) return;
            bool exhausted = State.Stamina <= ChoiceView.ExhaustedStamina;
            float multiplier = SurvivalModel.TimeMultiplier(SurvivalModel.InjuredCount(State), isMove, config.InjuredMoveExtraPerMember,
                exhausted, config.StaminaZeroTimeMultiplier);
            float actual = seconds * multiplier;
            State.BuildingTime = Timer.Advance(State.BuildingTime, actual);
            GameEvents.RaiseTimeSpent(actual);
        }

        public void SpendStamina(float amount)
        {
            if (gameManager == null || amount <= 0f) return;
            State.Stamina = Mathf.Max(0f, State.Stamina - amount);
        }

        private void OnAlarm()
        {
            GameEvents.RaiseBuildingAlarm();
            AudioService.PlayCue(config.AlarmCue);
        }

        private void OnAlarmTick(int secondsLeft)
        {
            GameEvents.RaiseBuildingTick(secondsLeft);
            AudioService.PlayCue(config.TickCue);
        }

        private void OnCollapse()
        {
            if (gameManager.IsDead) return;
            gameManager.Die(new DeathInfo
            {
                Victim = MemberType.Player,
                TextKey = config.CollapseTextKey.Key,
                TipId = config.CollapseTip != null ? config.CollapseTip.Id : null,
                RewindTarget = RewindTarget.Checkpoint
            });
        }

        private void OnRewound(string nodeId)
        {
            Timer.Sync(State.BuildingTime);
        }
    }
}
