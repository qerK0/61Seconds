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
        public Color HighlightColor => highlightColor;
        public float HighlightPulseSpeed => highlightPulseSpeed;
    }
}
