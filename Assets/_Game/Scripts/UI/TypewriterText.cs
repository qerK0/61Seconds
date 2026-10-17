using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Fast typewriter reveal on a TMP label (maxVisibleCharacters, unscaled time). Purely visual: dialogue
    /// time costs are paid per line by DialogueController, never by the reveal. Complete() shows everything.
    /// </summary>
    public sealed class TypewriterText : MonoBehaviour
    {
        [Tooltip("Label to reveal.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Characters per second.")]
        [Min(1f)]
        [SerializeField] private float charsPerSecond = 90f;

        private float shown;
        private int total;

        public bool IsRevealing { get; private set; }

        /// <summary>Sets the text and starts revealing it (instant when reveal is false).</summary>
        public void Play(string text, bool reveal = true)
        {
            if (label == null) return;
            label.text = text ?? string.Empty;
            label.ForceMeshUpdate();
            total = label.textInfo.characterCount;
            shown = 0f;
            IsRevealing = reveal && total > 0;
            label.maxVisibleCharacters = IsRevealing ? 0 : 99999;
        }

        public void Complete()
        {
            IsRevealing = false;
            if (label != null) label.maxVisibleCharacters = 99999;
        }

        private void Update()
        {
            if (!IsRevealing) return;
            shown += Time.unscaledDeltaTime * charsPerSecond;
            int visible = Mathf.FloorToInt(shown);
            if (visible >= total)
            {
                Complete();
                return;
            }

            label.maxVisibleCharacters = visible;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(TMP_Text target, float speed)
        {
            label = target;
            charsPerSecond = speed;
        }
    }
}
