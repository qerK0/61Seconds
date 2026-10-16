using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>Authoring data for a death: line of text, tip, sting, victim and optional rewind target.</summary>
    [Serializable]
    public sealed class DeathData
    {
        [Tooltip("One-line death text.")]
        [SerializeField] private TextKey textKey;

        [Tooltip("Deprem Notu unlocked by this death.")]
        [SerializeField] private Tip tip;

        [Tooltip("Optional sting; default death cue if empty.")]
        [SerializeField] private AudioCue cue;

        [Tooltip("Who dies. The child is always Trapped instead.")]
        [SerializeField] private MemberType victim = MemberType.Player;

        [Tooltip("LastNode = micro rewind; Checkpoint = macro rewind to the floor checkpoint.")]
        [SerializeField] private RewindTarget rewindTarget = RewindTarget.LastNode;

        [Tooltip("LastNode only: rewind to this node instead of the latest snapshot (delayed bombs).")]
        [SerializeField] private SceneNode rewindTo;

        public TextKey TextKey => textKey;
        public Tip Tip => tip;
        public AudioCue Cue => cue;
        public MemberType Victim => victim;
        public SceneNode RewindTo => rewindTo;
        public RewindTarget RewindTarget => rewindTarget;

        public DeathData()
        {
        }

        public DeathData(TextKey textKey, Tip tip, MemberType victim = MemberType.Player, SceneNode rewindTo = null, AudioCue cue = null,
            RewindTarget target = RewindTarget.LastNode)
        {
            rewindTarget = target;
            this.textKey = textKey;
            this.tip = tip;
            this.victim = victim;
            this.rewindTo = rewindTo;
            this.cue = cue;
        }

        /// <summary>Builds the runtime DeathInfo.</summary>
        public DeathInfo ToInfo(string anchorId = null)
        {
            return new DeathInfo
            {
                Victim = victim,
                TextKey = textKey.Key,
                TipId = tip != null ? tip.Id : null,
                AnchorId = anchorId,
                RewindNodeId = rewindTo != null ? rewindTo.Id : null,
                RewindTarget = rewindTarget,
                Cue = cue
            };
        }
    }
}
