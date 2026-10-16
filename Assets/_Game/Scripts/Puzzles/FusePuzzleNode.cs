using System.Collections.Generic;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Puzzles
{
    /// <summary>
    /// A small switch puzzle (3-4 switches). Wrong attempts cost building time; solving it applies the solved
    /// outcomes (e.g. SetFlag PowerCut) and goes to the next node. The node's own choices (e.g. "Geri dön") stay
    /// available next to the switches; the body text holds the hint.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Fuse Puzzle Node", fileName = "puzzle_fuse_new")]
    public sealed class FusePuzzleNode : SceneNode
    {
        [Tooltip("Correct switch positions (true = on). Its length is the switch count.")]
        [SerializeField] private bool[] solution = { true, false, true, true };

        [Tooltip("Building-timer seconds lost per wrong attempt.")]
        [Min(0f)]
        [SerializeField] private float wrongAttemptSeconds = 3f;

        [Tooltip("Applied when solved.")]
        [SerializeField] private List<Outcome> solvedOutcomes = new List<Outcome>();

        [Tooltip("Node after solving.")]
        [SerializeField] private SceneNode nextNode;

        public bool[] Solution => solution;
        public float WrongAttemptSeconds => wrongAttemptSeconds;
        public IReadOnlyList<Outcome> SolvedOutcomes => solvedOutcomes;
        public SceneNode NextNode => nextNode;

        /// <summary>Authoring helper (call after Configure).</summary>
        public void ConfigurePuzzle(bool[] pattern, float wrongSeconds, IEnumerable<Outcome> onSolved, SceneNode next)
        {
            solution = (bool[])pattern.Clone();
            wrongAttemptSeconds = wrongSeconds;
            solvedOutcomes = new List<Outcome>(onSolved);
            nextNode = next;
        }
    }
}
