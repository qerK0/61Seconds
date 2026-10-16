using SixtyOne.Audio;

namespace SixtyOne.Core
{
    /// <summary>Everything the death card, audio and rewind need to know about one death.</summary>
    public sealed class DeathInfo
    {
        /// <summary>Who died (or got trapped, for the child).</summary>
        public MemberType Victim { get; set; } = MemberType.Player;

        /// <summary>One-line death text key.</summary>
        public string TextKey { get; set; }

        /// <summary>Id of the "Deprem Notu" tip unlocked by this death.</summary>
        public string TipId { get; set; }

        /// <summary>Anchor whose label gets revealed, if the death happened on one.</summary>
        public string AnchorId { get; set; }

        /// <summary>Micro (last node) or macro (prep start) rewind.</summary>
        public RewindTarget RewindTarget { get; set; } = RewindTarget.LastNode;

        /// <summary>Item the player lacked (ShowLockedFatal). Added to the KnowledgeSet.</summary>
        public string MissingItemId { get; set; }

        /// <summary>Node to rewind to for LastNode rewinds. Null = latest snapshot.</summary>
        public string RewindNodeId { get; set; }

        /// <summary>Optional sting played instead of the default death cue.</summary>
        public AudioCue Cue { get; set; }
    }
}
