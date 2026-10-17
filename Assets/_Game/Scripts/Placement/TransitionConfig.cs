using SixtyOne.Audio;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Room-to-room transition: the old room's tiles, walls and props rise and fade (a wave from the far corner),
    /// the room swaps at the midpoint, then the new room's tiles drop in (floor, then walls, then props) with a small
    /// settle and the family pops in at the spawn. Heights are room-local units (they follow RoomVisualConfig).
    /// One asset per game.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Transition Config", fileName = "TransitionConfig")]
    public sealed class TransitionConfig : ScriptableObject
    {
        [Header("Out (old room rises)")]
        [Tooltip("Seconds until the last piece of the old room has risen and faded (ease-in).")]
        [Min(0.05f)]
        [SerializeField] private float outDuration = 0.35f;

        [Tooltip("How far pieces rise, in room-local units.")]
        [Min(0f)]
        [SerializeField] private float riseHeight = 1.2f;

        [Header("In (new room drops)")]
        [Tooltip("Seconds until the last piece of the new room has landed (ease-out + settle).")]
        [Min(0.05f)]
        [SerializeField] private float inDuration = 0.4f;

        [Tooltip("Start height above the final position, in room-local units.")]
        [Min(0f)]
        [SerializeField] private float dropHeight = 1f;

        [Tooltip("Descend (rope, cables, stairs, hole): longer in-phase.")]
        [Min(0.05f)]
        [SerializeField] private float descendInDuration = 0.65f;

        [Tooltip("Descend: start height above the final position, in room-local units.")]
        [Min(0f)]
        [SerializeField] private float descendDropHeight = 2.5f;

        [Header("Wave")]
        [Tooltip("Seconds between the far corner and the nearest piece starting (the wave).")]
        [Min(0f)]
        [SerializeField] private float stagger = 0.12f;

        [Tooltip("In-phase: walls start this many seconds after the floor.")]
        [Min(0f)]
        [SerializeField] private float wallLag = 0.05f;

        [Tooltip("In-phase: props start this many seconds after the floor (they land last).")]
        [Min(0f)]
        [SerializeField] private float propLag = 0.1f;

        [Tooltip("Landing overshoot (0.1 = dips about 10% below the final position before settling).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float bounce = 0.1f;

        [Tooltip("Seconds for the family to pop in at the spawn after the room has landed.")]
        [Min(0f)]
        [SerializeField] private float characterPop = 0.15f;

        [Header("Reduce Effects")]
        [Tooltip("With GameSettings.ReduceEffects: a plain fade of this total length instead of moving tiles.")]
        [Min(0.05f)]
        [SerializeField] private float reducedFadeDuration = 0.25f;

        [Header("Audio")]
        [Tooltip("Played on a room-to-room move (door).")]
        [SerializeField] private AudioCue doorCue;

        [Tooltip("Played on a room-to-room move (footsteps).")]
        [SerializeField] private AudioCue footstepsCue;

        [Tooltip("Played when changing floors.")]
        [SerializeField] private AudioCue descendCue;

        public float ReducedFadeDuration => reducedFadeDuration;
        public AudioCue DoorCue => doorCue;
        public AudioCue FootstepsCue => footstepsCue;
        public AudioCue DescendCue => descendCue;

        /// <summary>Timing values for one transition (descend = longer, higher drop).</summary>
        public TransitionTiming Timing(bool descend)
        {
            return new TransitionTiming
            {
                OutDuration = outDuration,
                InDuration = descend ? descendInDuration : inDuration,
                Rise = riseHeight,
                Drop = descend ? descendDropHeight : dropHeight,
                Stagger = stagger,
                WallLag = wallLag,
                PropLag = propLag,
                Bounce = bounce,
                CharacterPop = characterPop
            };
        }

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(float outSeconds, float inSeconds, float descendInSeconds, float rise, float drop, float descendDrop,
            float wave, float wallDelay, float propDelay, float overshoot, float pop, float reducedFade,
            AudioCue door, AudioCue footsteps, AudioCue descendSound)
        {
            outDuration = outSeconds;
            inDuration = inSeconds;
            descendInDuration = descendInSeconds;
            riseHeight = rise;
            dropHeight = drop;
            descendDropHeight = descendDrop;
            stagger = wave;
            wallLag = wallDelay;
            propLag = propDelay;
            bounce = overshoot;
            characterPop = pop;
            reducedFadeDuration = reducedFade;
            doorCue = door;
            footstepsCue = footsteps;
            descendCue = descendSound;
        }
    }
}
