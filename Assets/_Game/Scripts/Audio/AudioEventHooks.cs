using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>
    /// Plays the AudioEventMap cues in response to GameEvents: the floor's quake recording (kept running from the
    /// prep onset through the placement window, faded at the end), the pre-quake growl in the last prep seconds and
    /// during a placement countdown, craft, pickup, clicks, the death sting and a longer, louder rewind for macro rewinds.
    /// </summary>
    public sealed class AudioEventHooks : MonoBehaviour
    {
        [Tooltip("Which cue plays for which event.")]
        [SerializeField] private AudioEventMap map;

        [Tooltip("Service to play through. Defaults to AudioService.Instance.")]
        [SerializeField] private AudioService service;

        [Tooltip("Current node (placement countdown). Found in the scene if empty.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("The pre-quake growl starts this many prep seconds before the quake.")]
        [SerializeField] private float preQuakeLeadSeconds = 8f;

        [Tooltip("Fade-out of the quake recording when the shaking ends.")]
        [SerializeField] private float quakeFadeSeconds = 1.2f;

        [Tooltip("Macro (checkpoint) rewind: volume and pitch scale of the rewind sound (lower pitch = longer).")]
        [SerializeField] private Vector2 macroRewindVolumePitch = new Vector2(1.25f, 0.8f);

        private AudioService Service => service != null ? service : AudioService.Instance;
        private AudioCue quakeCue;
        private bool quaking;
        private RewindTarget lastRewind;

        private void Start()
        {
            if (map != null && Service != null) Service.Register(map.All);
            if (nodePlayer == null) nodePlayer = FindAnyObjectByType<NodePlayer>();
        }

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.QuakeEnded += OnQuakeEnded;
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
            GameEvents.ItemAdded += OnItemAdded;
            GameEvents.AfadAlert += OnAfadAlert;
            GameEvents.ChoiceMade += OnChoiceMade;
            GameEvents.Message += OnMessage;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.QuakeEnded -= OnQuakeEnded;
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
            GameEvents.ItemAdded -= OnItemAdded;
            GameEvents.AfadAlert -= OnAfadAlert;
            GameEvents.ChoiceMade -= OnChoiceMade;
            GameEvents.Message -= OnMessage;
        }

        private void Update()
        {
            if (map == null || map.PreQuake == null || Service == null || quaking) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsDead || Service.IsPlaying(map.PreQuake)) return;
            GameState state = gm.State;
            bool prepEnding = !state.BuildingTimerStarted && state.PrepTime > 0f && state.PrepTime <= preQuakeLeadSeconds;
            bool placementCountdown = nodePlayer != null && nodePlayer.Current is PlacementNode && nodePlayer.Timer.Running;
            if (prepEnding || placementCountdown) Service.Play(map.PreQuake);
        }

        private void Play(AudioCue cue)
        {
            if (cue != null && Service != null) Service.Play(cue);
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            quaking = true;
            if (map == null || Service == null) return;
            if (map.PreQuake != null) Service.FadeOut(map.PreQuake, 0.4f);
            AudioCue cue = map.QuakeFor(info.SoundAct);
            // The opening quake raises an onset and then the main shock: keep one recording running across both.
            if (cue == quakeCue && Service.IsPlaying(cue)) return;
            StopQuake(0.3f);
            quakeCue = cue;
            if (cue != null && !Mathf.Approximately(info.Loudness, 1f)) Service.Play(cue, info.Loudness, 1f);
            else Play(cue);
        }

        private void OnQuakeEnded(QuakeInfo info)
        {
            quaking = false;
            StopQuake(quakeFadeSeconds);
        }

        private void StopQuake(float fade)
        {
            if (Service == null) return;
            if (quakeCue != null) Service.FadeOut(quakeCue, fade);
            if (map != null && map.QuakeRumble != null && map.QuakeRumble != quakeCue) Service.FadeOut(map.QuakeRumble, fade);
            quakeCue = null;
        }

        private void OnDied(DeathInfo info)
        {
            lastRewind = info.RewindTarget;
            StopQuake(0.2f);
            if (map != null && map.PreQuake != null && Service != null) Service.FadeOut(map.PreQuake, 0.2f);
            Play(info.Cue != null ? info.Cue : map != null ? map.DeathSting : null);
        }

        private void OnRewound(string nodeId)
        {
            quaking = false;
            StopQuake(0.2f);
            if (map == null || map.Rewind == null || Service == null) return;
            if (lastRewind == RewindTarget.Checkpoint) Service.Play(map.Rewind, macroRewindVolumePitch.x, macroRewindVolumePitch.y);
            else Service.Play(map.Rewind);
        }

        private void OnItemAdded(string itemId) => Play(map != null ? map.Pickup : null);
        private void OnAfadAlert(float magnitude)
        {
            // PrepPhase also plays the alert on the prep node: never stack two copies.
            if (map != null && Service != null && !Service.IsPlaying(map.AfadAlert)) Play(map.AfadAlert);
        }
        private void OnChoiceMade(string nodeId, string labelKey) => Play(map != null ? map.ChoiceClick : null);

        private void OnMessage(string key)
        {
            if (key == UiKeys.BagFull || key == UiKeys.PlacementNoRoom || key == UiKeys.PlacementChildAlone
                || key == UiKeys.PlacementPlayerLast || key == UiKeys.PlacementInvalid)
            {
                Play(map != null ? map.Denied : null);
            }
            else if (key == UiKeys.CraftDone || key == UiKeys.RopeTied)
            {
                Play(map != null ? map.Craft : null);
            }
        }
    }
}
