namespace SixtyOne.Core
{
    /// <summary>Runtime state of one family member: health and the anchor they stand on.</summary>
    public sealed class FamilyMemberState
    {
        /// <summary>Which member this is.</summary>
        public MemberType Type { get; }

        /// <summary>Current health.</summary>
        public MemberHealth Health { get; set; }

        /// <summary>Id of the anchor the member stands on, or null when unplaced.</summary>
        public string AnchorId { get; set; }

        public FamilyMemberState(MemberType type, MemberHealth health = MemberHealth.Healthy, string anchorId = null)
        {
            Type = type;
            Health = health;
            AnchorId = anchorId;
        }

        /// <summary>Returns an independent copy.</summary>
        public FamilyMemberState Clone()
        {
            return new FamilyMemberState(Type, Health, AnchorId);
        }
    }
}
