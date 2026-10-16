using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Audio
{
    /// <summary>Default cues for game events. The sound designer only swaps clips on these cues.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Audio Event Map", fileName = "AudioEventMap")]
    public sealed class AudioEventMap : ScriptableObject
    {
        [Tooltip("Looping rumble while a quake shakes.")]
        [SerializeField] private AudioCue quakeRumble;

        [Tooltip("Default death sting (when the death has no own cue).")]
        [SerializeField] private AudioCue deathSting;

        [Tooltip("Rewind / tape sound.")]
        [SerializeField] private AudioCue rewind;

        [Tooltip("Item added to the bag.")]
        [SerializeField] private AudioCue pickup;

        [Tooltip("AFAD phone alert siren.")]
        [SerializeField] private AudioCue afadAlert;

        [Tooltip("Bag full / refused action.")]
        [SerializeField] private AudioCue denied;

        [Tooltip("Any choice clicked.")]
        [SerializeField] private AudioCue choiceClick;

        public AudioCue QuakeRumble => quakeRumble;
        public AudioCue DeathSting => deathSting;
        public AudioCue Rewind => rewind;
        public AudioCue Pickup => pickup;
        public AudioCue AfadAlert => afadAlert;
        public AudioCue Denied => denied;
        public AudioCue ChoiceClick => choiceClick;

        /// <summary>All assigned cues.</summary>
        public IEnumerable<AudioCue> All
        {
            get
            {
                AudioCue[] cues = { quakeRumble, deathSting, rewind, pickup, afadAlert, denied, choiceClick };
                foreach (AudioCue cue in cues)
                {
                    if (cue != null) yield return cue;
                }
            }
        }

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(AudioCue rumble, AudioCue death, AudioCue rewindCue, AudioCue pickupCue, AudioCue alert, AudioCue deniedCue, AudioCue click)
        {
            quakeRumble = rumble;
            deathSting = death;
            rewind = rewindCue;
            pickup = pickupCue;
            afadAlert = alert;
            denied = deniedCue;
            choiceClick = click;
        }
    }
}
