using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A timed placement phase: tap a member, tap an anchor; when the timer ends the quake plays
    /// and every Hazard resolves against the members on its anchor.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Placement Node", fileName = "node_placement_new")]
    public sealed class PlacementNode : SceneNode
    {
        [Tooltip("Node after everyone survives the quake.")]
        [SerializeField] private SceneNode nextNode;

        [Tooltip("Hazard for members left unplaced when the quake starts.")]
        [SerializeField] private Hazard unplacedHazard;

        [Tooltip("Death used when the child ends up with no adult near.")]
        [SerializeField] private DeathData childAloneDeath = new DeathData();

        [Tooltip("Seconds of shaking before results.")]
        [SerializeField] private float quakeSeconds = 4f;

        public SceneNode NextNode => nextNode;
        public Hazard UnplacedHazard => unplacedHazard;
        public DeathData ChildAloneDeath => childAloneDeath;
        public float QuakeSeconds => quakeSeconds;

        /// <summary>Authoring helper for generators (call after Configure).</summary>
        public void ConfigurePlacement(SceneNode next, Hazard unplaced, DeathData childAlone, float shakeSeconds)
        {
            nextNode = next;
            unplacedHazard = unplaced;
            childAloneDeath = childAlone;
            quakeSeconds = shakeSeconds;
        }
    }
}
