using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Player-facing options (toggleable). Referenced by GameManager.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Tooltip("In later loops, subtly highlight items the player died for lacking.")]
        [SerializeField] private bool knowledgeHighlight = true;

        [Tooltip("Accessibility: no white flash, chromatic aberration, lens wobble, dutch tilt or rotational shake; lighter camera shake.")]
        [SerializeField] private bool reduceEffects;

        [Tooltip("Camera shake multiplier while Reduce Effects is on.")]
        [Range(0f, 1f)]
        [SerializeField] private float reducedShakeScale = 0.35f;

        [Tooltip("Panic level (0..1) above which clicks start to lag. Below it there is never any delay.")]
        [Range(0f, 1f)]
        [SerializeField] private float panicDelayThreshold = 0.7f;

        [Tooltip("Click delay in seconds at full panic.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxInputDelay = 0.3f;

        [Tooltip("Show a subtle sparkle on hidden items (only in later loops or after a while in the room).")]
        [SerializeField] private bool hiddenItemSparkle = true;

        [Tooltip("Seconds in a room before an unknown hidden item starts to sparkle. 0 = only known items.")]
        [SerializeField] private float hiddenSparkleAfterSeconds = 25f;

        [Tooltip("Tint the highlight pulses towards.")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.55f, 1f);

        [Tooltip("Highlight pulses per second.")]
        [SerializeField] private float highlightPulseSpeed = 1.5f;

        public bool KnowledgeHighlight
        {
            get => knowledgeHighlight;
            set => knowledgeHighlight = value;
        }

        public bool ReduceEffects
        {
            get => reduceEffects;
            set => reduceEffects = value;
        }

        public float ReducedShakeScale => reducedShakeScale;
        public float PanicDelayThreshold => panicDelayThreshold;
        public float MaxInputDelay => maxInputDelay;
        public bool HiddenItemSparkle
        {
            get => hiddenItemSparkle;
            set => hiddenItemSparkle = value;
        }

        public float HiddenSparkleAfterSeconds => hiddenSparkleAfterSeconds;
        public Color HighlightColor => highlightColor;
        public float HighlightPulseSpeed => highlightPulseSpeed;
    }
}
