using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>
    /// Drives survival: the building timer (real time + action costs; collapse at 0 rewinds to the floor
    /// checkpoint), stamina drain, gas poisoning in leaking rooms (max = death), and panic (from the timer,
    /// poison and aftershocks) which feeds the click delay. Implements IActionCostSink so choices,
    /// interactables, dialogue and the bag pay time and stamina.
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

        [Tooltip("Pre-quake prep countdown: receives action costs until the building timer starts.")]
        [SerializeField] private SixtyOne.Prep.PrepPhase prep;

        [Tooltip("Dialogue driver (the real-time tick pauses while lines play).")]
        [SerializeField] private DialogueController dialogue;

        [Tooltip("Building seconds a rewind always leaves (a checkpoint saved with less would collapse again at once).")]
        [SerializeField] private float minSecondsAfterRewind = 60f;

        /// <summary>Poison at which the warning toast appears.</summary>
        private const float PoisonWarningLevel = 0.2f;

        public BuildingTimer Timer { get; } = new BuildingTimer();

        /// <summary>Current panic 0..1 (not part of GameState; recomputed after rewinds).</summary>
        public float Panic01 { get; private set; }

        /// <summary>Click delay right now (0 below the panic threshold or with Reduce Effects).</summary>
        public float CurrentInputDelay
        {
            get
            {
                GameSettings settings = gameManager != null ? gameManager.Settings : null;
                float threshold = settings != null ? settings.PanicDelayThreshold : 0.7f;
                float max = settings != null ? settings.MaxInputDelay : 0.3f;
                bool reduce = settings != null && settings.ReduceEffects;
                return SurvivalModel.InputDelay(Panic01, threshold, max, reduce);
            }
        }
        public LevelConfig Config => config;
        public float Remaining => gameManager != null ? gameManager.State.BuildingTime : 0f;
        public float Total => Timer.Total;

        private GameState State => gameManager.State;
        private bool alarmPlaying;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (config == null)
            {
                Debug.LogError("[Survival] No LevelConfig assigned.", this);
                return;
            }

            Timer.Configure(config.BuildingSeconds, config.PulseAtSeconds, config.AlarmAtSeconds);
            SixtyOne.Prep.PrepRules.ResetForPrep(State, config.PrepSeconds, config.BuildingSeconds);
            State.Stamina = config.StaminaMax;
            State.Inventory.Resize(config.MaxInventorySlots);
            Timer.Sync(State.BuildingTime);

            Timer.Pulse += GameEvents.RaiseBuildingPulse;
            Timer.Alarm += OnAlarm;
            Timer.AlarmTick += OnAlarmTick;
            Timer.Zero += OnCollapse;
            ActionCostRouter.Sink = this;
            InputDelayRouter.DelayProvider = () => CurrentInputDelay;
        }

        private void OnDestroy()
        {
            if (ActionCostRouter.Sink == (IActionCostSink)this) ActionCostRouter.Sink = null;
            InputDelayRouter.DelayProvider = null;
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.Died -= OnDied;
        }

        private void Update()
        {
            if (config == null || gameManager == null || gameManager.IsDead) return;
            // Out on the street: the run is won, the building can no longer kill.
            if (nodePlayer != null && nodePlayer.Current is EpilogueNode)
            {
                StopAlarm();
                return;
            }

            // Room transitions (tile rise/drop) never cost building time.
            float dt = NavigationEvents.TimerDelta(Time.deltaTime);
            bool paused = config.PauseInDialogue && dialogue != null && dialogue.IsPlayingLines;
            // Before the quake (prep) the building stands: no collapse clock, no drain, no gas leak yet.
            if (SixtyOne.Prep.PrepRules.SpendsPrep(State))
            {
                Panic01 = SurvivalModel.StepPanic(Panic01, 0f, config.PanicCalmPerSecond, dt);
                return;
            }

            if (config.RealtimeTick && !paused)
            {
                State.BuildingTime = Timer.Advance(State.BuildingTime, dt);
                State.Stamina = SurvivalModel.DrainStamina(State.Stamina, config.StaminaDrainPerSecond, dt);
            }

            if (gameManager.IsDead) return;
            UpdatePoison(dt);
            if (gameManager.IsDead) return;
            if (!paused) UpdateExhaustion(dt);
            if (gameManager.IsDead) return;
            Panic01 = SurvivalModel.StepPanic(Panic01, PanicBaseline(), config.PanicCalmPerSecond, dt);
        }

        /// <summary>True while the player is in a room with leaking gas and carries the wet cloth (worn as a mask).</summary>
        public bool MaskWorn
        {
            get
            {
                if (config == null || gameManager == null || config.WetClothItem == null || !StateOps.HasItem(State, config.WetClothItem.Id)) return false;
                SceneNode node = nodePlayer != null ? nodePlayer.Current : null;
                GasZone zone = config.ZoneFor(node != null ? node.RoomId : null);
                return zone != null && !State.Flags.Get(zone.ValveFlag);
            }
        }

        private void UpdatePoison(float dt)
        {
            SceneNode node = nodePlayer != null ? nodePlayer.Current : null;
            GasZone zone = config.ZoneFor(node != null ? node.RoomId : null);
            // No gas build-up during a timed placement: the player cannot reach the valve until it ends.
            bool placing = node is SixtyOne.Placement.PlacementNode;
            bool openGas = zone != null && !placing && !State.Flags.Get(zone.ValveFlag);
            bool wetCloth = config.WetClothItem != null && StateOps.HasItem(State, config.WetClothItem.Id);
            float before = State.Poison;
            State.Poison = SurvivalModel.StepPoison(before, openGas, zone != null ? zone.PoisonPerSecond : 0f, wetCloth,
                config.WetClothFactor, config.PoisonDecayPerSecond, dt);

            if (before < PoisonWarningLevel && State.Poison >= PoisonWarningLevel)
            {
                GameEvents.RaiseMessage(SixtyOne.Text.UiKeys.PoisonWarning);
            }

            if (State.Poison >= 1f)
            {
                gameManager.Die(new DeathInfo
                {
                    Victim = MemberType.Player,
                    TextKey = config.PoisonTextKey.Key,
                    TipId = config.PoisonTip != null ? config.PoisonTip.Id : null,
                    RewindTarget = RewindTarget.LastNode
                });
            }
        }

        private void UpdateExhaustion(float dt)
        {
            State.Exhaustion = SurvivalModel.StepExhaustion(State.Exhaustion, State.Stamina, config.ExhaustionPerSecond,
                config.ExhaustionDecayPerSecond, dt);
            if (State.Exhaustion < 1f) return;
            gameManager.Die(new DeathInfo
            {
                Victim = MemberType.Player,
                TextKey = config.ExhaustionTextKey.Key,
                TipId = config.ExhaustionTip != null ? config.ExhaustionTip.Id : null,
                RewindTarget = RewindTarget.Checkpoint
            });
        }

        private float PanicBaseline()
        {
            float timerPanic = config.EvaluateTimerPanic(Timer.ElapsedFraction(State.BuildingTime));
            return SurvivalModel.PanicBaseline(timerPanic, Mathf.Max(State.Poison, State.Exhaustion), config.PanicFromPoison);
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            // At least the aftershock panic (not additive: the opening quake raises an onset and the main shock).
            if (config != null) Panic01 = Mathf.Clamp01(Mathf.Max(Panic01, config.AftershockPanic));
        }

        public void SpendTime(float seconds, bool isMove)
        {
            if (config == null || gameManager == null || gameManager.IsDead || seconds <= 0f) return;
            if (SixtyOne.Prep.PrepRules.SpendsPrep(State))
            {
                if (prep != null) prep.Spend(seconds);
                return;
            }

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
            StartAlarm();
        }

        /// <summary>The alarm loops for the last seconds; started once (a rewind inside the window restarts it).</summary>
        private void StartAlarm()
        {
            if (alarmPlaying || config.AlarmCue == null) return;
            alarmPlaying = true;
            AudioService.PlayCue(config.AlarmCue);
        }

        private void StopAlarm()
        {
            if (!alarmPlaying) return;
            alarmPlaying = false;
            AudioService.FadeOutCue(config.AlarmCue, 0.4f);
        }

        private void OnDied(DeathInfo info) => StopAlarm();

        private void OnAlarmTick(int secondsLeft)
        {
            GameEvents.RaiseBuildingTick(secondsLeft);
            AudioService.PlayCue(config.TickCue);
        }

        /// <summary>
        /// Asked when the building timer hits 0: true = the family is far enough away (out on the street), so the
        /// collapse is a cinematic beat instead of a death. Set by StreetQuakeDirector while the street is shown.
        /// </summary>
        public static Func<bool> CollapseSpares { get; set; }

        private void OnCollapse()
        {
            if (gameManager.IsDead) return;
            if (CollapseSpares != null && CollapseSpares())
            {
                StopAlarm();
                StateOps.SetFlag(State, FlagNames.CollapsedBehind, true);
                GameEvents.RaiseBuildingCollapsedBehind();
                return;
            }

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
            // A snapshot taken close to a death (no stamina, deep exhaustion / poison, the timer nearly out) must not
            // kill again on its own and loop the same death card and info card.
            if (config != null && gameManager != null)
            {
                RewindRecovery.Apply(State, gameManager.Session.LastRewindWasMacro, config.StaminaMax, minSecondsAfterRewind);
            }

            Timer.Sync(State.BuildingTime);
            if (config == null) return;
            Panic01 = PanicBaseline();
            if (State.BuildingTimerStarted && State.BuildingTime <= config.AlarmAtSeconds) StartAlarm();
            else StopAlarm();
        }
    }
}
