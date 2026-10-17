using System.Collections.Generic;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>
    /// Reusable NPC conversation: lines advance on click, each costing building time. Choices marked
    /// DuringDialogue (e.g. "Kısa kes") are offered while lines play; the rest appear after the last line.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Dialogue Node", fileName = "dlg_new")]
    public class DialogueNode : SceneNode
    {
        [Tooltip("Lines in order.")]
        [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

        public IReadOnlyList<DialogueLine> Lines => lines;

        /// <summary>Authoring helper (call after Configure).</summary>
        public void SetLines(IEnumerable<DialogueLine> dialogueLines)
        {
            lines = new List<DialogueLine>(dialogueLines);
        }
    }
}
