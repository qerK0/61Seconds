using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// Code tweens for the rabid dog in the F2 corridor (state-driven, so rewinds snap it back):
    /// barks and trembles while its QTE runs, backs off a few cells after the whistle, and runs off-screen and fades
    /// out after the food is thrown. Unscaled time; frozen while paused.
    /// </summary>
    public sealed class RabidDogPresenter : MonoBehaviour
    {
        [Tooltip("State owner. Defaults to GameManager.Instance.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Sprite child that trembles (the idle loop's renderer).")]
        [SerializeField] private Transform body;

        [Tooltip("QTE node id: the dog trembles and barks while it runs.")]
        [SerializeField] private string qteNodeId = "f2.dog";

        [Tooltip("Flag set by the whistle: the dog backs off.")]
        [SerializeField] private string scaredFlag = "DogScared";

        [Tooltip("Flag set by the thrown food: the dog runs off-screen.")]
        [SerializeField] private string luredFlag = "DogLured";

        [Tooltip("Played when the QTE starts.")]
        [SerializeField] private AudioCue barkCue;

        [Tooltip("Back-off offset after the whistle (grid cells).")]
        [SerializeField] private Vector2 scaredOffset = new Vector2(1.6f, -0.2f);

        [Tooltip("Where the dog runs to after the food (grid cells from its spot; off the corridor's far end).")]
        [SerializeField] private Vector2 runOffset = new Vector2(7f, 0.3f);

        [Tooltip("Seconds of the run before it is gone.")]
        [Min(0.1f)]
        [SerializeField] private float runSeconds = 1.1f;

        [Tooltip("Seconds of the whistle back-off.")]
        [Min(0.05f)]
        [SerializeField] private float backSeconds = 0.45f;

        [Tooltip("Tremble amplitude while the QTE runs (world units at scale 1).")]
        [SerializeField] private float shakeAmplitude = 0.02f;

        [Tooltip("Tremble frequency (Hz).")]
        [SerializeField] private float shakeFrequency = 26f;

        private SpriteRenderer[] renderers = new SpriteRenderer[0];
        private Color[] baseColors = new Color[0];
        private SpriteRenderer bodyRenderer;
        private IsoSorter sorter;
        private Vector3 basePosition;
        private Vector3 bodyBase;
        private bool baseFlip;
        private bool qteActive;
        private bool hidden;
        private float runT = -1f;
        private Vector3 runStart;
        private float shakeTime;

        private void Awake()
        {
            basePosition = transform.localPosition;
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            if (body != null)
            {
                bodyBase = body.localPosition;
                bodyRenderer = body.GetComponent<SpriteRenderer>();
            }

            if (bodyRenderer != null) baseFlip = bodyRenderer.flipX;
            sorter = GetComponent<IsoSorter>();
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        }

        private void OnEnable()
        {
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.Rewound += OnRewound;
            GameEvents.FlagChanged += OnFlagChanged;
            GameEvents.Died += OnDied;
            Snap();
        }

        private void OnDisable()
        {
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.Rewound -= OnRewound;
            GameEvents.FlagChanged -= OnFlagChanged;
            GameEvents.Died -= OnDied;
        }

        private GameState State
        {
            get
            {
                GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
                return gm != null ? gm.State : null;
            }
        }

        private bool Flag(string flag) => State != null && !string.IsNullOrEmpty(flag) && State.Flags.Get(flag);

        private Vector3 RestPosition => basePosition + (Flag(scaredFlag) ? IsoMath.GridToWorld(scaredOffset.x, scaredOffset.y) : Vector3.zero);

        private void OnNodeEntered(string nodeId)
        {
            bool was = qteActive;
            qteActive = nodeId == qteNodeId;
            if (qteActive && !was && !hidden) AudioService.PlayCue(barkCue);
        }

        private void OnRewound(string nodeId)
        {
            qteActive = false;
            Snap();
        }

        private void OnDied(DeathInfo info) => qteActive = false;

        private void OnFlagChanged(string flag, bool value)
        {
            if (flag != luredFlag || !value || hidden || runT >= 0f) return;
            runT = 0f;
            runStart = transform.localPosition;
            // The art faces right: face the run direction.
            if (bodyRenderer != null) bodyRenderer.flipX = IsoMath.GridToWorld(runOffset.x, runOffset.y).x < 0f;
        }

        /// <summary>Puts the dog where the current state says (no tween): gone, backed off or at its spot.</summary>
        private void Snap()
        {
            runT = -1f;
            SetHidden(Flag(luredFlag));
            if (bodyRenderer != null) bodyRenderer.flipX = baseFlip;
            transform.localPosition = RestPosition;
            if (body != null) body.localPosition = bodyBase;
            if (sorter != null) sorter.Apply();
        }

        private void SetHidden(bool on)
        {
            hidden = on;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].enabled = !on;
                renderers[i].color = baseColors[i];
            }
        }

        private void SetAlpha(float alpha)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = baseColors[i];
                c.a *= alpha;
                renderers[i].color = c;
            }
        }

        private void Update()
        {
            if (PauseState.IsPaused || State == null) return;
            float dt = Time.unscaledDeltaTime;
            Vector3 before = transform.localPosition;

            if (runT >= 0f)
            {
                runT = Mathf.Min(1f, runT + dt / runSeconds);
                float eased = runT * runT * (3f - 2f * runT);
                transform.localPosition = Vector3.Lerp(runStart, basePosition + IsoMath.GridToWorld(runOffset.x, runOffset.y), eased);
                SetAlpha(1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, runT)));
                if (runT >= 1f)
                {
                    runT = -1f;
                    SetHidden(true);
                }
            }
            else if (!hidden)
            {
                Vector3 offset = IsoMath.GridToWorld(scaredOffset.x, scaredOffset.y);
                float speed = offset.magnitude / backSeconds;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, RestPosition, speed * dt);
            }

            if (body != null)
            {
                Vector3 jitter = Vector3.zero;
                if (qteActive && !hidden && runT < 0f)
                {
                    shakeTime += dt;
                    GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
                    float amp = shakeAmplitude * (gm != null && gm.Settings != null && gm.Settings.ReduceEffects ? 0.3f : 1f);
                    float p = shakeTime * shakeFrequency * Mathf.PI * 2f;
                    jitter = new Vector3(Mathf.Sin(p) * amp, Mathf.Abs(Mathf.Sin(p * 0.5f + 1.3f)) * amp * 0.5f, 0f);
                }

                body.localPosition = bodyBase + jitter;
            }

            if (sorter != null && (transform.localPosition - before).sqrMagnitude > 0f) sorter.Apply();
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(Transform bodyTransform, AudioCue bark)
        {
            body = bodyTransform;
            barkCue = bark;
        }
    }
}
