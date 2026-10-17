using SixtyOne.Building;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Code sprite animator (any frame count): while the motor runs, loops the run frames at RunFps scaled by
    /// the move speed and flips by horizontal direction. Standing still holds the idle frame with a subtle
    /// breathing loop (Y scale + small bob, random phase); higher panic breathes faster.
    /// </summary>
    public sealed class CharacterAnimator : MonoBehaviour
    {
        [Tooltip("Animated sprite (a child, so breathing never moves the collider or the feet pivot).")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Run frames in order (sorted numerically by the importer).")]
        [SerializeField] private Sprite[] runFrames = new Sprite[0];

        [Tooltip("Frame held while standing (there are no idle frames).")]
        [Min(0)]
        [SerializeField] private int idleFrame;

        [Tooltip("True if the source art faces right (flipX when running left).")]
        [SerializeField] private bool artFacesRight = true;

        [Tooltip("Movement source.")]
        [SerializeField] private CharacterMotor motor;

        [Tooltip("Fps, breathing and panic tuning.")]
        [SerializeField] private MovementConfig config;

        [Tooltip("Panic source for faster breathing. Optional.")]
        [SerializeField] private SurvivalController survival;

        private Vector3 baseScale = Vector3.one;
        private Vector3 basePosition;
        private float frameClock;
        private float breathPhase;

        public int FrameCount => runFrames != null ? runFrames.Length : 0;

        private void Awake()
        {
            if (body != null)
            {
                baseScale = body.transform.localScale;
                basePosition = body.transform.localPosition;
            }

            breathPhase = Random.value * Mathf.PI * 2f;
            if (survival == null) survival = FindAnyObjectByType<SurvivalController>();
        }

        private void LateUpdate()
        {
            if (body == null || runFrames == null || runFrames.Length == 0) return;
            float dt = Time.deltaTime;
            bool running = motor != null && motor.IsMoving && motor.Velocity.sqrMagnitude > 0.0001f;
            if (running) Run(dt);
            else Idle(dt);
        }

        private void Run(float dt)
        {
            float fps = config != null ? config.RunFps : 12f;
            frameClock += dt * fps * Mathf.Max(0.3f, motor.SpeedRatio);
            int frame = Mathf.FloorToInt(frameClock) % runFrames.Length;
            body.sprite = runFrames[frame];
            float vx = motor.Velocity.x;
            if (Mathf.Abs(vx) > 0.01f) body.flipX = artFacesRight ? vx < 0f : vx > 0f;
            body.transform.localScale = baseScale;
            body.transform.localPosition = basePosition;
        }

        private void Idle(float dt)
        {
            frameClock = 0f;
            body.sprite = runFrames[Mathf.Clamp(idleFrame, 0, runFrames.Length - 1)];
            float period = config != null ? config.BreathPeriod : 1.6f;
            float panicSpeed = config != null ? config.PanicBreathSpeed : 2f;
            float panic = survival != null ? survival.Panic01 : 0f;
            breathPhase += dt * Mathf.PI * 2f / period * Mathf.Lerp(1f, panicSpeed, panic);
            float k = (Mathf.Sin(breathPhase) + 1f) * 0.5f;
            float scale = config != null ? config.BreathScale : 0.02f;
            float bob = config != null ? config.BreathBob : 0.008f;
            body.transform.localScale = new Vector3(baseScale.x, baseScale.y * (1f + scale * k), baseScale.z);
            body.transform.localPosition = basePosition + new Vector3(0f, bob * k, 0f);
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(SpriteRenderer sprite, Sprite[] frames, int idle, bool facesRight, CharacterMotor source, MovementConfig tuning, SurvivalController panic)
        {
            body = sprite;
            runFrames = frames;
            idleFrame = idle;
            artFacesRight = facesRight;
            motor = source;
            config = tuning;
            survival = panic;
        }
    }
}
