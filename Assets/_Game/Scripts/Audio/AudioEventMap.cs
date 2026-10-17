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

        [Tooltip("Quake recording per floor stage (1 = Floor 4 opening quake, 2 = Floor 3, 3 = Floor 2). Empty = quakeRumble.")]
        [SerializeField] private AudioCue[] quakeByAct = new AudioCue[0];
        [Tooltip("Low growl in the last seconds before a quake (prep end, placement countdown).")]
        [SerializeField] private AudioCue preQuake;
        [Tooltip("Successful combine / rope tied.")]
        [SerializeField] private AudioCue craft;
        public AudioCue QuakeRumble => quakeRumble;
        public AudioCue PreQuake => preQuake;
        public AudioCue Craft => craft;

        /// <summary>Quake cue for a floor stage (falls back to the generic rumble).</summary>
        public AudioCue QuakeFor(int act)
        {
            int i = act - 1;
            if (quakeByAct != null && i >= 0 && i < quakeByAct.Length && quakeByAct[i] != null) return quakeByAct[i];
            return quakeRumble;
        }
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
                AudioCue[] cues = { quakeRumble, deathSting, rewind, pickup, afadAlert, denied, choiceClick, preQuake, craft };
                foreach (AudioCue cue in cues)
                {
                    if (cue != null) yield return cue;
                }

                if (quakeByAct == null) yield break;
                foreach (AudioCue cue in quakeByAct)
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

        /// <summary>Authoring helper: per-floor quakes, the pre-quake growl and the craft sound.</summary>
        public void ConfigureExtras(AudioCue[] quakes, AudioCue preQuakeCue, AudioCue craftCue)
        {
            quakeByAct = quakes ?? new AudioCue[0];
            preQuake = preQuakeCue;
            craft = craftCue;
        }
    }
}
