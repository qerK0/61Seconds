using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Click-and-run and character animation tuning. Walking costs no building time beyond the real time that
    /// passes; only door transitions inside a flat cost doorSeconds (moves to other zones use their choice cost).
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Movement Config", fileName = "MovementConfig")]
    public sealed class MovementConfig : ScriptableObject
    {
        [Header("Run")]
        [Tooltip("Player run speed in world units per second.")]
        [Min(0.1f)]
        [SerializeField] private float runSpeed = 2.4f;

        [Tooltip("Followers run this much faster than the player so they catch up.")]
        [Min(0.1f)]
        [SerializeField] private float followerSpeedFactor = 1.1f;

        [Tooltip("Injured members run at this fraction of their speed.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float injuredSpeedFactor = 0.75f;

        [Tooltip("Grid-cell margin kept from the walls when clamping a click onto the floor.")]
        [Min(0f)]
        [SerializeField] private float floorMargin = 0.25f;

        [Header("Followers")]
        [Tooltip("Seconds the wife waits before following.")]
        [Min(0f)]
        [SerializeField] private float wifeDelay = 0.2f;

        [Tooltip("Seconds the child waits before following.")]
        [Min(0f)]
        [SerializeField] private float childDelay = 0.35f;

        [Tooltip("Wife's spot relative to the player (world units).")]
        [SerializeField] private Vector2 wifeOffset = new Vector2(0.38f, 0.1f);

        [Tooltip("Child's spot relative to the player (world units).")]
        [SerializeField] private Vector2 childOffset = new Vector2(-0.32f, 0.12f);

        [Tooltip("A follower starts running once it is this far (world units) from its spot.")]
        [Min(0.01f)]
        [SerializeField] private float followStartDistance = 0.35f;

        [Header("Doors")]
        [Tooltip("Building/prep seconds for a door between rooms of the same flat (other doors use their choice's cost).")]
        [Min(0f)]
        [SerializeField] private float doorSeconds = 1f;

        [Header("Animation")]
        [Tooltip("Run animation frames per second at full run speed.")]
        [Min(1f)]
        [SerializeField] private float runFps = 12f;

        [Tooltip("Idle breathing loop length in seconds (calm).")]
        [Min(0.1f)]
        [SerializeField] private float breathPeriod = 1.6f;

        [Tooltip("Idle breathing: extra Y scale at the top of a breath (0.02 = 1.0 to 1.02).")]
        [Min(0f)]
        [SerializeField] private float breathScale = 0.02f;

        [Tooltip("Idle breathing: vertical bob in world units (~1-2 px at 1080p).")]
        [Min(0f)]
        [SerializeField] private float breathBob = 0.008f;

        [Tooltip("At full panic the breathing runs this many times faster.")]
        [Min(1f)]
        [SerializeField] private float panicBreathSpeed = 2.2f;

        public float RunSpeed => runSpeed;
        public float FollowerSpeedFactor => followerSpeedFactor;
        public float InjuredSpeedFactor => injuredSpeedFactor;
        public float FloorMargin => floorMargin;
        public float WifeDelay => wifeDelay;
        public float ChildDelay => childDelay;
        public Vector2 WifeOffset => wifeOffset;
        public Vector2 ChildOffset => childOffset;
        public float FollowStartDistance => followStartDistance;
        public float DoorSeconds => doorSeconds;
        public float RunFps => runFps;
        public float BreathPeriod => breathPeriod;
        public float BreathScale => breathScale;
        public float BreathBob => breathBob;
        public float PanicBreathSpeed => panicBreathSpeed;

        /// <summary>Authoring helper for the generator.</summary>
        public void Configure(float run, float doorCost, float fps, float breath)
        {
            runSpeed = run;
            doorSeconds = doorCost;
            runFps = fps;
            breathPeriod = breath;
        }
    }
}
