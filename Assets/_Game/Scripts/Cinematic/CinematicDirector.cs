using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Building;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// Drives the cinematic look from GameEvents and the building timer: per-floor base grade, building tension
    /// (timer -> 0), placement letterbox + heartbeat, poison darkening, quake flash/aberration/wobble/flicker/dust/tilt, dark post-quake with the
    /// flashlight, death black-and-white, rewind spike and the win look. Works on an instantiated copy of
    /// the volume profile (never edits the asset) and uses unscaled time only; Time.timeScale is never touched.
    /// </summary>
    public sealed class CinematicDirector : MonoBehaviour
    {
        [Tooltip("Profiles for act 1, 2 and 3 (index 0 = act 1).")]
        [SerializeField] private CinematicProfile[] actProfiles = new CinematicProfile[3];

        [Tooltip("Global volume to drive. Its shared profile is copied at runtime.")]
        [SerializeField] private Volume volume;

        [Tooltip("Weight of the cinematic volume.")]
        [Range(0f, 1f)] [SerializeField] private float volumeWeight = 1f;

        [Tooltip("Camera (ortho size push-in, rotation, post-processing). Defaults to Camera.main.")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current node (placement detection).")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Building timer and poison source.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Global Light2D (ambient).")]
        [SerializeField] private Light2D globalLight;

        [Tooltip("Practical lights (lamps, windows) that flicker and die with the power.")]
        [SerializeField] private Light2D[] practicalLights = new Light2D[0];

        [Tooltip("Soft light that follows the player after the quake when the flashlight item is in the bag.")]
        [SerializeField] private Light2D flashlight;

        [Tooltip("Player transform the flashlight follows.")]
        [SerializeField] private Transform player;

        [Tooltip("Dust particles burst at quake start (emission off; emitted by code).")]
        [SerializeField] private ParticleSystem dust;

        [Tooltip("Letterbox bars for the placement window.")]
        [SerializeField] private LetterboxBars letterbox;

        [Tooltip("Entering any of these node ids blends to the warm 'we got out' look.")]
        [SerializeField] private string[] exitNodeIds = new string[0];

        private readonly List<VolumeComponent> runtimeComponents = new List<VolumeComponent>();
        private VolumeProfile runtimeProfile;
        private Tonemapping tonemapping;
        private ColorAdjustments colorAdjustments;
        private SplitToning splitToning;
        private Vignette vignette;
        private FilmGrain filmGrain;
        private Bloom bloom;
        private ChromaticAberration chromatic;
        private LensDistortion lensDistortion;

        private CinematicState state;
        private float baseOrthoSize;
        private float flashTimer;
        private float quakeTime;
        private float quakeDuration;
        private float tilt;
        private float deathTarget;
        private float winTarget;
        private float postQuakeTarget;
        private bool quaking;
        private bool quakeHappened;
        private bool powerOut;
        private bool flickering;
        private bool lowPassOn;
        private int lastPlacementSecond = -1;
        private Coroutine flickerRoutine;

        /// <summary>Profile of the current act.</summary>
        public CinematicProfile Profile
        {
            get
            {
                int act = gameManager != null ? gameManager.State.Act : 1;
                int index = Mathf.Clamp(act - 1, 0, actProfiles.Length - 1);
                return actProfiles.Length > 0 ? actProfiles[index] : null;
            }
        }

        private bool ReduceEffects
        {
            get
            {
                GameSettings settings = gameManager != null ? gameManager.Settings : null;
                return settings != null && settings.ReduceEffects;
            }
        }

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null)
            {
                baseOrthoSize = targetCamera.orthographicSize;
                targetCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }

            BuildRuntimeProfile();
            if (flashlight != null) flashlight.enabled = false;
        }

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.QuakeEnded += OnQuakeEnded;
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.BuildingTick += OnHeartbeat;
            GameEvents.PrepTick += OnHeartbeat;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.QuakeEnded -= OnQuakeEnded;
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.BuildingTick -= OnHeartbeat;
            GameEvents.PrepTick -= OnHeartbeat;
            SetLowPass(false, 0f);
        }

        private void OnDestroy()
        {
            foreach (VolumeComponent component in runtimeComponents)
            {
                if (component != null) Destroy(component);
            }

            if (runtimeProfile != null) Destroy(runtimeProfile);
        }

        private void BuildRuntimeProfile()
        {
            if (volume == null) return;

            // Deep copy: Instantiate(profile) alone would share (and mutate) the asset's components.
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "Cinematic (runtime)";
            if (volume.sharedProfile != null)
            {
                foreach (VolumeComponent source in volume.sharedProfile.components)
                {
                    if (source == null) continue;
                    VolumeComponent copy = Instantiate(source);
                    copy.name = source.name;
                    runtimeProfile.components.Add(copy);
                    runtimeComponents.Add(copy);
                }
            }

            tonemapping = Ensure<Tonemapping>();
            colorAdjustments = Ensure<ColorAdjustments>();
            splitToning = Ensure<SplitToning>();
            vignette = Ensure<Vignette>();
            filmGrain = Ensure<FilmGrain>();
            bloom = Ensure<Bloom>();
            chromatic = Ensure<ChromaticAberration>();
            lensDistortion = Ensure<LensDistortion>();

            tonemapping.mode.Override(TonemappingMode.ACES);
            filmGrain.type.Override(FilmGrainLookup.Medium1);
            vignette.color.Override(Color.black);
            vignette.rounded.Override(false);

            volume.isGlobal = true;
            volume.sharedProfile = runtimeProfile;
            volume.weight = volumeWeight;
        }

        private T Ensure<T>() where T : VolumeComponent
        {
            if (!runtimeProfile.TryGet(out T component))
            {
                component = runtimeProfile.Add<T>(true);
                runtimeComponents.Add(component);
            }

            component.active = true;
            return component;
        }

        private void Update()
        {
            CinematicProfile p = Profile;
            if (p == null || runtimeProfile == null) return;

            float dt = Time.unscaledDeltaTime;
            bool reduce = ReduceEffects;

            // Tension: the prep countdown before the quake, then the building timer for the rest of the game.
            GameState gs = gameManager != null ? gameManager.State : null;
            bool inPrep = gs != null && !gs.BuildingTimerStarted && survival != null && survival.Config != null && survival.Config.PrepSeconds > 0f;
            if (inPrep) state.Prep = 1f - Mathf.Clamp01(gs.PrepTime / survival.Config.PrepSeconds);
            else state.Prep = survival != null ? survival.Timer.ElapsedFraction(survival.Remaining) : 0f;
            bool prepTicking = inPrep && gs.PrepTime > 0f && gs.PrepTime <= survival.Config.PrepTickFrom;
            // Gas and exhaustion darken the same way (the stronger one wins).
            state.Poison = gameManager != null ? Mathf.Max(gameManager.State.Poison, gameManager.State.Exhaustion) : 0f;

            bool inWindow = nodePlayer != null && nodePlayer.Current is PlacementNode && nodePlayer.Timer.Running;
            if (inWindow)
            {
                // Heartbeat on every placement-timer second.
                int second = Mathf.CeilToInt(nodePlayer.Timer.Remaining);
                if (second != lastPlacementSecond) state.Heartbeat = 1f;
                lastPlacementSecond = second;
            }
            else
            {
                lastPlacementSecond = -1;
            }

            state.Heartbeat = CinematicMixer.Approach(state.Heartbeat, 0f, p.HeartbeatSeconds, dt);
            state.PostQuake = CinematicMixer.Approach(state.PostQuake, postQuakeTarget, 1.5f, dt);
            state.Death = CinematicMixer.Approach(state.Death, deathTarget, deathTarget > 0f ? p.DeathBlendSeconds : 0.25f, dt);
            state.Rewind = CinematicMixer.Approach(state.Rewind, 0f, p.RewindSeconds, dt);
            state.Win = CinematicMixer.Approach(state.Win, winTarget, p.WinBlendSeconds, dt);

            flashTimer = Mathf.Max(0f, flashTimer - dt);
            state.Flash = p.FlashSeconds > 0f ? Mathf.Clamp01(flashTimer / p.FlashSeconds) : 0f;

            UpdateQuake(p, dt);
            state.ReduceEffects = reduce;

            CinematicFrame frame = CinematicMixer.Mix(p, state);
            Apply(p, frame);
            UpdateCamera(p, frame, reduce, dt);
            UpdateLights(p);

            bool bars = (inWindow || quaking || prepTicking) && deathTarget <= 0f;
            if (letterbox != null) letterbox.Show(bars, p.LetterboxAspect, p.LetterboxSlideSeconds);
            SetLowPass(inWindow && deathTarget <= 0f && p.PlacementLowPassHz > 0f, p.PlacementLowPassHz);
        }

        private void UpdateQuake(CinematicProfile p, float dt)
        {
            if (!quaking)
            {
                state.Chromatic = CinematicMixer.Approach(state.Chromatic, 0f, 0.4f, dt);
                state.LensDistortion = 0f;
                return;
            }

            quakeTime += dt;
            float pulse = Mathf.Max(0.01f, p.ChromaticPulseSeconds);
            state.Chromatic = quakeTime < pulse ? Mathf.Lerp(p.ChromaticPeak, p.ChromaticSettle, quakeTime / pulse) : p.ChromaticSettle;

            // Wobble fades out over the last 30% of the quake.
            float envelope = quakeDuration > 0f ? Mathf.Clamp01((quakeDuration - quakeTime) / (quakeDuration * 0.3f)) : 1f;
            state.LensDistortion = Mathf.Sin(quakeTime * p.LensWobbleFrequency * Mathf.PI * 2f) * p.LensWobbleAmplitude * envelope;
        }

        private void Apply(CinematicProfile p, CinematicFrame f)
        {
            volume.weight = volumeWeight;
            colorAdjustments.postExposure.Override(f.PostExposure);
            colorAdjustments.contrast.Override(f.Contrast);
            colorAdjustments.saturation.Override(f.Saturation);
            colorAdjustments.colorFilter.Override(f.ColorFilter);
            splitToning.shadows.Override(p.SplitShadows);
            splitToning.highlights.Override(p.SplitHighlights);
            splitToning.balance.Override(p.SplitBalance);
            vignette.intensity.Override(f.Vignette);
            vignette.smoothness.Override(p.VignetteSmoothness);
            filmGrain.intensity.Override(f.Grain);
            filmGrain.response.Override(p.GrainResponse);
            bloom.threshold.Override(p.BloomThreshold);
            bloom.intensity.Override(p.BloomIntensity);
            bloom.scatter.Override(p.BloomScatter);
            chromatic.intensity.Override(f.Chromatic);
            lensDistortion.intensity.Override(f.LensDistortion);
        }

        /// <summary>Un-zoomed orthographic size (set by room framing); the cinematic zoom scales from it.</summary>
        public float BaseOrthoSize
        {
            get => baseOrthoSize;
            set => baseOrthoSize = Mathf.Max(0.1f, value);
        }

        private void UpdateCamera(CinematicProfile p, CinematicFrame frame, bool reduce, float dt)
        {
            if (targetCamera == null) return;
            targetCamera.orthographicSize = baseOrthoSize * (1f - frame.Zoom);

            float tiltTarget = quaking && !reduce ? p.DutchTiltDegrees : 0f;
            tilt = Mathf.MoveTowards(tilt, tiltTarget, 12f * dt);
            float shake = 0f;
            if (quaking && !reduce)
            {
                float envelope = quakeDuration > 0f ? Mathf.Clamp01(1f - quakeTime / quakeDuration) : 1f;
                shake = (Mathf.Sin(quakeTime * 23f) + 0.5f * Mathf.Sin(quakeTime * 37f)) * p.RotationalShakeDegrees * envelope;
            }

            targetCamera.transform.localRotation = Quaternion.Euler(0f, 0f, tilt + shake);
        }

        private void UpdateLights(CinematicProfile p)
        {
            if (!flickering)
            {
                SetLights(!powerOut, p);
            }

            if (flashlight == null) return;
            bool hasFlashlight = gameManager != null && StateOps.HasItem(gameManager.State, p.FlashlightItemId)
                && !gameManager.State.Flags.Get(FlagNames.FlashlightOff);
            bool on = powerOut && state.PostQuake > 0.01f && hasFlashlight && deathTarget <= 0f;
            flashlight.enabled = on;
            if (!on) return;
            flashlight.intensity = p.FlashlightIntensity * state.PostQuake;
            // Profile radius and offset are tuned at world scale 1 (RoomVisualConfig scales the World root).
            float worldScale = player != null && player.parent != null ? Mathf.Abs(player.parent.lossyScale.x) : 1f;
            flashlight.pointLightOuterRadius = p.FlashlightRadius * worldScale;
            if (player != null) flashlight.transform.position = player.position + new Vector3(0f, 0.4f * worldScale, 0f);
        }

        private void SetLights(bool on, CinematicProfile p)
        {
            if (globalLight != null)
            {
                globalLight.color = p.GlobalLightColor;
                globalLight.intensity = on ? p.GlobalLightIntensity : p.PostQuakeGlobalLight;
            }

            foreach (Light2D light in practicalLights)
            {
                if (light == null) continue;
                light.enabled = on;
                light.intensity = p.PracticalLightIntensity;
            }
        }

        private IEnumerator Flicker(CinematicProfile p)
        {
            flickering = true;
            int count = Random.Range(Mathf.Max(1, p.FlickerCountMin), Mathf.Max(p.FlickerCountMin, p.FlickerCountMax) + 1);
            for (int i = 0; i < count; i++)
            {
                SetLights(false, p);
                yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.1f));
                SetLights(true, p);
                yield return new WaitForSecondsRealtime(Random.Range(0.06f, 0.22f));
            }

            powerOut = PowerCut(p);
            flickering = false;
            flickerRoutine = null;
        }

        private bool PowerCut(CinematicProfile p)
        {
            if (!p.LightsOffAfterQuake) return false;
            bool keep = !string.IsNullOrEmpty(p.KeepLightsFlag) && gameManager != null && gameManager.State.Flags.Get(p.KeepLightsFlag);
            return !keep;
        }

        private void StopFlicker()
        {
            if (flickerRoutine != null) StopCoroutine(flickerRoutine);
            flickerRoutine = null;
            flickering = false;
        }

        private void SetLowPass(bool on, float cutoffHz)
        {
            if (on == lowPassOn) return;
            lowPassOn = on;
            if (AudioService.Instance == null) return;
            if (on) AudioService.Instance.SetLowPass(cutoffHz);
            else AudioService.Instance.ClearLowPass();
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            CinematicProfile p = Profile;
            if (p == null) return;
            quaking = true;
            quakeTime = 0f;
            quakeDuration = info.Duration;
            if (!ReduceEffects) flashTimer = p.FlashSeconds;

            if (dust != null && p.DustBurstCount > 0)
            {
                if (targetCamera != null)
                {
                    Vector3 c = targetCamera.transform.position;
                    dust.transform.position = new Vector3(c.x, c.y + 1f, 0f);
                }

                dust.Emit(p.DustBurstCount);
            }

            // Aftershocks in the dark don't bring the lights back for a flicker.
            if (!powerOut)
            {
                StopFlicker();
                flickerRoutine = StartCoroutine(Flicker(p));
            }
        }

        private void OnQuakeEnded(QuakeInfo info)
        {
            quaking = false;
            quakeHappened = true;
            postQuakeTarget = 1f;
        }

        private void OnDied(DeathInfo info)
        {
            deathTarget = 1f;
            quaking = false;
        }

        private void OnRewound(string nodeId)
        {
            deathTarget = 0f;
            state.Death = 0f;
            state.Rewind = 1f;
            state.Heartbeat = 0f;
            quaking = false;
            StopFlicker();
        }

        private void OnHeartbeat(int secondsLeft)
        {
            state.Heartbeat = 1f;
        }

        private void OnNodeEntered(string nodeId)
        {
            CinematicProfile p = Profile;
            quaking = false;
            StopFlicker();

            // The QuakeHappened flag lives in GameState, so this is right after any rewind too.
            quakeHappened = gameManager != null && gameManager.State.Flags.Get(FlagNames.QuakeHappened);
            powerOut = quakeHappened && p != null && PowerCut(p);
            postQuakeTarget = quakeHappened ? 1f : 0f;

            winTarget = 0f;
            foreach (string id in exitNodeIds)
            {
                if (id == nodeId) winTarget = 1f;
            }
        }
    }
}
