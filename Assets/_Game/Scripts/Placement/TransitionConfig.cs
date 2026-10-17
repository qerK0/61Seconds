using SixtyOne.Audio;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>Room-to-room transition look: duration, slide/pan distances and cues. One asset per game.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Transition Config", fileName = "TransitionConfig")]
    public sealed class TransitionConfig : ScriptableObject
    {
        [Tooltip("Whole transition in seconds (fade out + fade in). The room swaps at the midpoint.")]
        [Min(0.05f)]
        [SerializeField] private float transitionDuration = 0.35f;

        [Tooltip("Transition when descending to another floor (rope, cables, stairs).")]
        [Min(0.05f)]
        [SerializeField] private float descendDuration = 0.6f;

        [Tooltip("Camera pan in world units during a room-to-room move.")]
        [SerializeField] private float lateralPan = 0.6f;

        [Tooltip("Camera pan in world units when changing floors (downwards).")]
        [SerializeField] private float descendPan = 1.2f;

        [Tooltip("Peak darkness of the cross-fade (1 = black).")]
        [Range(0f, 1f)]
        [SerializeField] private float peakAlpha = 1f;

        [Tooltip("Played on a room-to-room move (door).")]
        [SerializeField] private AudioCue doorCue;

        [Tooltip("Played on a room-to-room move (footsteps).")]
        [SerializeField] private AudioCue footstepsCue;

        [Tooltip("Played when changing floors.")]
        [SerializeField] private AudioCue descendCue;

        public float TransitionDuration => transitionDuration;
        public float DescendDuration => descendDuration;
        public float LateralPan => lateralPan;
        public float DescendPan => descendPan;
        public float PeakAlpha => peakAlpha;
        public AudioCue DoorCue => doorCue;
        public AudioCue FootstepsCue => footstepsCue;
        public AudioCue DescendCue => descendCue;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(float duration, float descend, float pan, float descendPanUnits, AudioCue door, AudioCue footsteps, AudioCue descendSound)
        {
            transitionDuration = duration;
            descendDuration = descend;
            lateralPan = pan;
            descendPan = descendPanUnits;
            doorCue = door;
            footstepsCue = footsteps;
            descendCue = descendSound;
        }
    }
}
