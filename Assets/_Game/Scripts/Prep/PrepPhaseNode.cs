using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// The preparation phase that opens an act: the AFAD alert starts a countdown (61s in Act 1), the
    /// player explores rooms and collects gear, then at placementWindowSeconds the linked PlacementNode
    /// takes over until the quake. A duration at or below the window means "straight to placement".
    /// Its entry snapshot is the macro rewind target.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Prep Phase Node", fileName = "node_prep_new")]
    public sealed class PrepPhaseNode : SceneNode
    {
        [Tooltip("Total countdown in seconds (Act 1 = 61). <= placement window = no prep.")]
        [SerializeField] private float duration = 61f;

        [Tooltip("Seconds left when the ÇÖK-KAPAN-TUTUN placement starts. The linked PlacementNode timer should match.")]
        [SerializeField] private float placementWindowSeconds = 10f;

        [Tooltip("Countdown ticks play for the last N seconds.")]
        [SerializeField] private int tickFromSeconds = 10;

        [Tooltip("Seconds it costs to walk to another room.")]
        [SerializeField] private float roomTravelSeconds = 1f;

        [Tooltip("Rooms the player can visit during prep. The node's RoomId is where it starts.")]
        [SerializeField] private List<string> roomIds = new List<string>();

        [Tooltip("Placement that runs during the placement window.")]
        [SerializeField] private PlacementNode placementNode;

        [Tooltip("Alert title at prep start.")]
        [SerializeField] private TextKey alertTitleKey;

        [Tooltip("Alert body at prep start ({0} = seconds).")]
        [SerializeField] private TextKey alertBodyKey;

        [Tooltip("Alert title when placement starts (ÇÖK-KAPAN-TUTUN).")]
        [SerializeField] private TextKey placementAlertTitleKey;

        [Tooltip("Alert body when placement starts.")]
        [SerializeField] private TextKey placementAlertBodyKey;

        [Tooltip("Played on every second of the last ticks.")]
        [SerializeField] private AudioCue tickCue;

        [Tooltip("Played when the prep alert appears.")]
        [SerializeField] private AudioCue alarmCue;

        [Tooltip("Played when the placement window starts.")]
        [SerializeField] private AudioCue placementAlertCue;

        public float Duration => duration;
        public float PlacementWindowSeconds => placementWindowSeconds;
        public int TickFromSeconds => tickFromSeconds;
        public float RoomTravelSeconds => roomTravelSeconds;
        public IReadOnlyList<string> RoomIds => roomIds;
        public PlacementNode PlacementNode => placementNode;
        public TextKey AlertTitleKey => alertTitleKey;
        public TextKey AlertBodyKey => alertBodyKey;
        public TextKey PlacementAlertTitleKey => placementAlertTitleKey;
        public TextKey PlacementAlertBodyKey => placementAlertBodyKey;
        public AudioCue TickCue => tickCue;
        public AudioCue AlarmCue => alarmCue;
        public AudioCue PlacementAlertCue => placementAlertCue;

        /// <summary>False when the act skips prep and goes straight to placement.</summary>
        public bool HasPrep => duration > placementWindowSeconds;

        public override bool IsPrepStart => true;

        /// <summary>Authoring helper for generators (call after Configure).</summary>
        public void ConfigurePrep(float seconds, float placementWindow, PlacementNode placement, IEnumerable<string> rooms, float travelSeconds = 1f, int tickFrom = 10)
        {
            duration = seconds;
            placementWindowSeconds = placementWindow;
            placementNode = placement;
            roomIds = new List<string>(rooms);
            roomTravelSeconds = travelSeconds;
            tickFromSeconds = tickFrom;
        }

        /// <summary>Authoring helper for generators.</summary>
        public void ConfigureAlerts(TextKey title, TextKey body, TextKey placementTitle, TextKey placementBody, AudioCue tick, AudioCue alarm, AudioCue placementAlarm)
        {
            alertTitleKey = title;
            alertBodyKey = body;
            placementAlertTitleKey = placementTitle;
            placementAlertBodyKey = placementBody;
            tickCue = tick;
            alarmCue = alarm;
            placementAlertCue = placementAlarm;
        }
    }
}
