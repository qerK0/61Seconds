using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Player-facing options (toggleable). Referenced by GameManager.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Tooltip("In later loops, subtly highlight items the player died for lacking.")]
        [SerializeField] private bool knowledgeHighlight = true;

        [Tooltip("Tint the highlight pulses towards.")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.55f, 1f);

        [Tooltip("Highlight pulses per second.")]
        [SerializeField] private float highlightPulseSpeed = 1.5f;

        public bool KnowledgeHighlight
        {
            get => knowledgeHighlight;
            set => knowledgeHighlight = value;
        }

        public Color HighlightColor => highlightColor;
        public float HighlightPulseSpeed => highlightPulseSpeed;
    }
}
