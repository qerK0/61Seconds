using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// A room of the pre-quake loot phase (Floor 4). While the current node is a PrepPhaseNode the prep countdown
    /// runs and action costs come out of it instead of the building timer. When it reaches zero the quake starts and
    /// the linked PlacementNode takes over. No node handler: the room is explorable and its choices move between
    /// prep rooms. The first prep node (the floor checkpoint) shows the AFAD early-warning alert.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Prep Phase Node", fileName = "node_prep_new")]
    public sealed class PrepPhaseNode : SceneNode
    {
        [Tooltip("Opening-quake placement entered when the prep countdown hits zero.")]
        [SerializeField] private PlacementNode quakeNode;

        [Tooltip("Show the AFAD early-warning alert when this node is entered (the prep start only).")]
        [SerializeField] private bool showAlertOnEnter;

        [Tooltip("Alert title (AFAD early warning).")]
        [SerializeField] private TextKey alertTitleKey;

        [Tooltip("Alert body ({0} = seconds until the quake).")]
        [SerializeField] private TextKey alertBodyKey;

        [Tooltip("Camera shake scale of the quake onset at zero (the placement's own quake follows).")]
        [Range(0f, 2f)]
        [SerializeField] private float onsetShakeScale = 0.4f;

        public PlacementNode QuakeNode => quakeNode;
        public bool ShowAlertOnEnter => showAlertOnEnter;
        public TextKey AlertTitleKey => alertTitleKey;
        public TextKey AlertBodyKey => alertBodyKey;
        public float OnsetShakeScale => onsetShakeScale;

        /// <summary>Authoring helper for generators (call after Configure).</summary>
        public void ConfigurePrep(PlacementNode quake, bool alertOnEnter, TextKey title, TextKey body, float shakeScale = 0.4f)
        {
            quakeNode = quake;
            showAlertOnEnter = alertOnEnter;
            alertTitleKey = title;
            alertBodyKey = body;
            onsetShakeScale = shakeScale;
        }
    }
}
