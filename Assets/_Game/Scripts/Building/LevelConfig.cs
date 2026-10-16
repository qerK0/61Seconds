using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>
    /// Whole-building settings: the collapse timer, stamina, poison (gas rooms), panic and their deaths.
    /// One asset per game; every number is tunable without code.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Level Config", fileName = "LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Building timer")]
        [Tooltip("Seconds until the building collapses (whole game).")]
        [SerializeField] private float buildingSeconds = 300f;
        [Tooltip("Seconds left when the timer UI starts to pulse.")]
        [SerializeField] private float pulseAtSeconds = 60f;
        [Tooltip("Seconds left when the alarm starts (ticks every second).")]
        [SerializeField] private float alarmAtSeconds = 30f;
        [Tooltip("The timer also runs in real time (not only on action costs).")]
        [SerializeField] private bool realtimeTick = true;
        [Tooltip("Pause the real-time tick while dialogue lines play (lines have their own cost).")]
        [SerializeField] private bool pauseInDialogue = true;
        [Tooltip("Collapse death line.")]
        [SerializeField] private TextKey collapseTextKey;
        [Tooltip("Collapse death tip.")]
        [SerializeField] private Tip collapseTip;
        [Tooltip("Played once when the alarm starts.")]
        [SerializeField] private AudioCue alarmCue;
        [Tooltip("Played every second during the alarm.")]
        [SerializeField] private AudioCue tickCue;

        [Header("Moves")]
        [Tooltip("Extra fraction of a move's time per injured family member (0.25 = +25% each).")]
        [SerializeField] private float injuredMoveExtraPerMember = 0.25f;

        [Header("Stamina")]
        [Tooltip("Maximum (and starting) stamina.")]
        [SerializeField] private float staminaMax = 100f;
        [Tooltip("Stamina drained per second.")]
        [SerializeField] private float staminaDrainPerSecond = 0.15f;
        [Tooltip("Time multiplier for every action while stamina is 0.")]
        [SerializeField] private float staminaZeroTimeMultiplier = 1.5f;

        [Header("Poison (gas)")]
        [Tooltip("Rooms with a gas leak and the valve flag that stops it.")]
        [SerializeField] private List<GasZone> gasZones = new List<GasZone>();
        [Tooltip("Poison lost per second outside gas (or once the valve is closed).")]
        [SerializeField] private float poisonDecayPerSecond = 0.01f;
        [Tooltip("Item that reduces poison gain while in the bag (wet cloth).")]
        [SerializeField] private Item wetClothItem;
        [Tooltip("Poison gain multiplier with the wet cloth (0.5 = halved).")]
        [Range(0f, 1f)] [SerializeField] private float wetClothFactor = 0.5f;
        [Tooltip("Gas poisoning death line.")]
        [SerializeField] private TextKey poisonTextKey;
        [Tooltip("Gas poisoning tip.")]
        [SerializeField] private Tip poisonTip;

        [Header("Panic")]
        [Tooltip("Panic from the building timer: x = elapsed fraction (0 start .. 1 collapse), y = panic 0..1.")]
        [SerializeField] private AnimationCurve panicFromTimer = AnimationCurve.EaseInOut(0f, 0f, 1f, 0.85f);
        [Tooltip("Panic added per unit of poison.")]
        [SerializeField] private float panicFromPoison = 0.6f;
        [Tooltip("Panic spike on an aftershock.")]
        [Range(0f, 1f)] [SerializeField] private float aftershockPanic = 0.45f;
        [Tooltip("Panic lost per second while calm (down to the timer/poison baseline).")]
        [SerializeField] private float panicCalmPerSecond = 0.04f;

        public float BuildingSeconds => buildingSeconds;
        public float PulseAtSeconds => pulseAtSeconds;
        public float AlarmAtSeconds => alarmAtSeconds;
        public bool RealtimeTick => realtimeTick;
        public bool PauseInDialogue => pauseInDialogue;
        public TextKey CollapseTextKey => collapseTextKey;
        public Tip CollapseTip => collapseTip;
        public AudioCue AlarmCue => alarmCue;
        public AudioCue TickCue => tickCue;
        public float InjuredMoveExtraPerMember => injuredMoveExtraPerMember;
        public float StaminaMax => staminaMax;
        public float StaminaDrainPerSecond => staminaDrainPerSecond;
        public float StaminaZeroTimeMultiplier => staminaZeroTimeMultiplier;
        public IReadOnlyList<GasZone> GasZones => gasZones;
        public float PoisonDecayPerSecond => poisonDecayPerSecond;
        public Item WetClothItem => wetClothItem;
        public float WetClothFactor => wetClothFactor;
        public TextKey PoisonTextKey => poisonTextKey;
        public Tip PoisonTip => poisonTip;
        public float PanicFromPoison => panicFromPoison;
        public float AftershockPanic => aftershockPanic;
        public float PanicCalmPerSecond => panicCalmPerSecond;

        /// <summary>Panic baseline from the timer for an elapsed fraction.</summary>
        public float EvaluateTimerPanic(float elapsedFraction)
        {
            float x = Mathf.Clamp01(elapsedFraction);
            return panicFromTimer != null && panicFromTimer.length > 0 ? Mathf.Clamp01(panicFromTimer.Evaluate(x)) : x;
        }

        /// <summary>Gas zone for a room, or null.</summary>
        public GasZone ZoneFor(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return null;
            foreach (GasZone zone in gasZones)
            {
                if (zone != null && zone.RoomId == roomId) return zone;
            }

            return null;
        }

        /// <summary>Authoring helper for generators.</summary>
        public void ConfigureTimer(float seconds, float pulseAt, float alarmAt, TextKey collapseText, Tip collapseTipAsset, AudioCue alarm, AudioCue tick)
        {
            buildingSeconds = seconds;
            pulseAtSeconds = pulseAt;
            alarmAtSeconds = alarmAt;
            collapseTextKey = collapseText;
            collapseTip = collapseTipAsset;
            alarmCue = alarm;
            tickCue = tick;
        }

        /// <summary>Authoring helper for generators.</summary>
        public void ConfigurePoison(IEnumerable<GasZone> zones, Item wetCloth, TextKey deathText, Tip tip)
        {
            gasZones = new List<GasZone>(zones);
            wetClothItem = wetCloth;
            poisonTextKey = deathText;
            poisonTip = tip;
        }
    }
}
