using SixtyOne.Building;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>What a character's sprite is doing.</summary>
    public enum CharacterAnimState
    {
        /// <summary>Holding the idle frame and breathing.</summary>
        Idle,

        /// <summary>Looping the run frames.</summary>
        Run,

        /// <summary>Drop-Cover-Hold: crouch frames forward (then hold the last) or backward to standing.</summary>
        Crouch
    }

    /// <summary>How a quake crouch behaves.</summary>
    public enum CrouchMode
    {
        /// <summary>No crouch.</summary>
        Off,

        /// <summary>Crouch and hold until released (placement quakes: nobody moves).</summary>
        Hold,

        /// <summary>Crouch while standing still; running stands up at once (the street quake: run to the open).</summary>
        WhenStill
    }

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

        [Tooltip("Drop-Cover-Hold frames, standing (0) to the held crouch (last). Optional.")]
        [SerializeField] private Sprite[] crouchFrames = new Sprite[0];

        [Tooltip("Body scale for the crouch frames relative to the run frames (frame 0 = the same height).")]
        [SerializeField] private float crouchScale = 1f;

        [Tooltip("Crouch-down and stand-up speeds (frames per second).")]
        [SerializeField] private Vector2 crouchFps = new Vector2(14f, 18f);

        private Vector3 baseScale = Vector3.one;
        private Vector3 basePosition;
        private float frameClock;
        private float breathPhase;
        private CrouchMode crouchMode;
        private float crouchClock;

        public int FrameCount => runFrames != null ? runFrames.Length : 0;

        public bool HasCrouch => crouchFrames != null && crouchFrames.Length > 0;

        /// <summary>True while any crouch frame is shown (going down, held, or standing back up).</summary>
        public bool IsCrouching => HasCrouch && (crouchMode != CrouchMode.Off || crouchClock > 0f);

        /// <summary>Starts the Drop-Cover-Hold (forward once, then hold the last frame).</summary>
        public void StartCrouch(CrouchMode mode)
        {
            if (HasCrouch) crouchMode = mode;
        }

        /// <summary>Shaking ended: play the crouch backwards to standing.</summary>
        public void StandUp() => crouchMode = CrouchMode.Off;

        /// <summary>Straight back to the idle pose (rewind, death, Trapped): no reverse animation.</summary>
        public void ResetCrouch()
        {
            crouchMode = CrouchMode.Off;
            crouchClock = 0f;
        }

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
            bool moving = motor != null && motor.IsMoving;
            Vector2 velocity = motor != null ? motor.Velocity : Vector2.zero;
            // The street crouch gives way to running (stand up at once, run to the open).
            if (crouchMode == CrouchMode.WhenStill && StateFor(moving, velocity) == CharacterAnimState.Run) crouchClock = 0f;
            bool crouching = IsCrouching && !(crouchMode == CrouchMode.WhenStill && StateFor(moving, velocity) == CharacterAnimState.Run);
            State = StateFor(moving, velocity, crouching);
            if (State == CharacterAnimState.Crouch) Crouch(dt);
            else if (State == CharacterAnimState.Run) Run(dt);
            else Idle(dt);
        }

        /// <summary>Current state (Idle until the first frame).</summary>
        public CharacterAnimState State { get; private set; }

        /// <summary>Run while the motor moves with a real velocity; anything else is idle (pure).</summary>
        public static CharacterAnimState StateFor(bool moving, Vector2 velocity)
        {
            return moving && velocity.sqrMagnitude > 0.0001f ? CharacterAnimState.Run : CharacterAnimState.Idle;
        }

        /// <summary>Crouch beats run beats idle (pure).</summary>
        public static CharacterAnimState StateFor(bool moving, Vector2 velocity, bool crouching)
        {
            return crouching ? CharacterAnimState.Crouch : StateFor(moving, velocity);
        }

        /// <summary>
        /// Advances the crouch clock (in frames): forward at downFps while crouching, backward at upFps while
        /// standing up; clamped to [0, last frame] so the last frame is held (pure).
        /// </summary>
        public static float CrouchClock(float clock, float dt, bool down, float downFps, float upFps, int frameCount)
        {
            if (frameCount <= 0) return 0f;
            float next = down ? clock + dt * downFps : clock - dt * upFps;
            return Mathf.Clamp(next, 0f, frameCount - 1);
        }

        /// <summary>Crouch frame for a clock: never past the last (held) frame (pure).</summary>
        public static int CrouchFrameIndex(float clock, int frameCount)
        {
            if (frameCount <= 0) return 0;
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, clock)), 0, frameCount - 1);
        }

        /// <summary>Looping run frame for an accumulated frame clock (pure).</summary>
        public static int RunFrameIndex(float frameClock, int frameCount)
        {
            if (frameCount <= 0) return 0;
            int frame = Mathf.FloorToInt(Mathf.Max(0f, frameClock)) % frameCount;
            return frame;
        }

        /// <summary>flipX for a horizontal velocity; tiny velocities keep the current facing (pure).</summary>
        public static bool FlipFor(float velocityX, bool artFacesRight, bool currentFlip)
        {
            if (Mathf.Abs(velocityX) <= 0.01f) return currentFlip;
            return artFacesRight ? velocityX < 0f : velocityX > 0f;
        }

        /// <summary>Breathing phase speed in radians per second for a period and panic 0..1 (pure).</summary>
        public static float BreathRate(float period, float panic01, float panicSpeed)
        {
            return Mathf.PI * 2f / Mathf.Max(0.05f, period) * Mathf.Lerp(1f, panicSpeed, Mathf.Clamp01(panic01));
        }

        private void Run(float dt)
        {
            float fps = config != null ? config.RunFps : 12f;
            frameClock += dt * fps * Mathf.Max(0.3f, motor.SpeedRatio);
            body.sprite = runFrames[RunFrameIndex(frameClock, runFrames.Length)];
            body.flipX = FlipFor(motor.Velocity.x, artFacesRight, body.flipX);
            body.transform.localScale = baseScale;
            body.transform.localPosition = basePosition;
        }

        /// <summary>Crouch frames at their own scale on the shared foot pivot; the facing (flipX) stays as it was.</summary>
        private void Crouch(float dt)
        {
            frameClock = 0f;
            crouchClock = CrouchClock(crouchClock, dt, crouchMode != CrouchMode.Off, crouchFps.x, crouchFps.y, crouchFrames.Length);
            body.sprite = crouchFrames[CrouchFrameIndex(crouchClock, crouchFrames.Length)];
            body.transform.localScale = baseScale * crouchScale;
            body.transform.localPosition = basePosition;
        }

        private void Idle(float dt)
        {
            frameClock = 0f;
            body.sprite = runFrames[Mathf.Clamp(idleFrame, 0, runFrames.Length - 1)];
            float period = config != null ? config.BreathPeriod : 1.6f;
            float panicSpeed = config != null ? config.PanicBreathSpeed : 2f;
            float panic = survival != null ? survival.Panic01 : 0f;
            breathPhase += dt * BreathRate(period, panic, panicSpeed);
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

        /// <summary>Authoring helper: crouch frames and their body scale relative to the run frames.</summary>
        public void ConfigureCrouch(Sprite[] frames, float scaleRelativeToRun)
        {
            crouchFrames = frames ?? new Sprite[0];
            crouchScale = scaleRelativeToRun;
        }
    }
}
