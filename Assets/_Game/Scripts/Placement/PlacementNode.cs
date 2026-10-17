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

        [Tooltip("Magnitude shown for this quake/aftershock. 0 = act default.")]
        [SerializeField] private float magnitude;

        [Tooltip("Camera shake multiplier (1 = act default).")]
        [SerializeField] private float shakeScale = 1f;

        [Tooltip("Which quake this is (GameState.QuakeCount after it): opening 1, F3 aftershock 2, F2 aftershock 3. 0 = one more than before.")]
        [Min(0)]
        [SerializeField] private int quakeNumber;

        public SceneNode NextNode => nextNode;
        public Hazard UnplacedHazard => unplacedHazard;
        public DeathData ChildAloneDeath => childAloneDeath;
        public float QuakeSeconds => quakeSeconds;
        public float Magnitude => magnitude;
        public override float AlertMagnitude => magnitude > 0f ? magnitude : base.AlertMagnitude;
        public float ShakeScale => shakeScale;
        public int QuakeNumber => quakeNumber;

        /// <summary>Authoring helper: the quake count this placement's quake brings the building to (debris, cracks).</summary>
        public void ConfigureQuakeNumber(int number) => quakeNumber = number;

        /// <summary>Authoring helper: magnitude and shake strength of this quake moment.</summary>
        public void ConfigureQuake(float quakeMagnitude, float cameraShakeScale)
        {
            magnitude = quakeMagnitude;
            shakeScale = cameraShakeScale;
        }

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
