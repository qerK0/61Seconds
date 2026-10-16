using SixtyOne.Core;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>Plays the AudioEventMap cues in response to GameEvents.</summary>
    public sealed class AudioEventHooks : MonoBehaviour
    {
        [Tooltip("Which cue plays for which event.")]
        [SerializeField] private AudioEventMap map;

        [Tooltip("Service to play through. Defaults to AudioService.Instance.")]
        [SerializeField] private AudioService service;

        private AudioService Service => service != null ? service : AudioService.Instance;

        private void Start()
        {
            if (map != null && Service != null) Service.Register(map.All);
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

        private void Play(AudioCue cue)
        {
            if (cue != null && Service != null) Service.Play(cue);
        }

        private void OnQuakeStarted(QuakeInfo info) => Play(map != null ? map.QuakeRumble : null);

        private void OnQuakeEnded(QuakeInfo info)
        {
            if (map != null && Service != null && map.QuakeRumble != null) Service.Stop(map.QuakeRumble);
        }

        private void OnDied(DeathInfo info)
        {
            if (map != null && Service != null && map.QuakeRumble != null) Service.Stop(map.QuakeRumble);
            Play(info.Cue != null ? info.Cue : map != null ? map.DeathSting : null);
        }

        private void OnRewound(string nodeId) => Play(map != null ? map.Rewind : null);
        private void OnItemAdded(string itemId) => Play(map != null ? map.Pickup : null);
        private void OnAfadAlert(float magnitude) => Play(map != null ? map.AfadAlert : null);
        private void OnChoiceMade(string nodeId, string labelKey) => Play(map != null ? map.ChoiceClick : null);

        private void OnMessage(string key)
        {
            if (key == UiKeys.BagFull || key == UiKeys.PlacementNoRoom || key == UiKeys.PlacementChildAlone
                || key == UiKeys.PlacementPlayerLast || key == UiKeys.PlacementInvalid)
            {
                Play(map != null ? map.Denied : null);
            }
        }
    }
}
