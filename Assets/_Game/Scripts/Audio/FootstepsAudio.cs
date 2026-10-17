using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>Loops the family footsteps while the player runs (click-and-run) and fades them out on arrival.</summary>
    public sealed class FootstepsAudio : MonoBehaviour
    {
        [Tooltip("The player's motor.")]
        [SerializeField] private CharacterMotor player;

        [Tooltip("Footsteps loop.")]
        [SerializeField] private AudioCue footsteps;

        [Tooltip("Fade speed (volume units per second); fast so steps stop on arrival.")]
        [SerializeField] private float fadePerSecond = 4f;

        private AudioLoopLayer layer;

        private void Awake()
        {
            layer = new AudioLoopLayer(transform, "Footsteps");
        }

        private void Update()
        {
            bool running = player != null && player.isActiveAndEnabled && player.IsMoving;
            layer.Tick(footsteps, running ? 1f : 0f, fadePerSecond, Time.unscaledDeltaTime);
        }
    }
}
