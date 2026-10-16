namespace SixtyOne.Core
{
    /// <summary>
    /// Deep copy of GameState taken when a decision/placement node starts.
    /// Restoring keeps the live LoopCount, collected tips, revealed anchor labels and knowledge.
    /// Member anchor positions are kept for micro rewinds and restored for macro (prep start) rewinds.
    /// </summary>
    public sealed class Snapshot
    {
        private readonly GameState saved;

        /// <summary>Node the snapshot belongs to; the rewind target.</summary>
        public string NodeId { get; }

        private Snapshot(string nodeId, GameState saved)
        {
            NodeId = nodeId;
            this.saved = saved;
        }

        /// <summary>Takes a deep copy of the live state.</summary>
        public static Snapshot Capture(GameState live, string nodeId)
        {
            return new Snapshot(nodeId, live.Clone());
        }

        /// <summary>
        /// Restores the saved state into the live state object.
        /// keepMemberAnchors: true = members stay where the player last put them (micro rewind).
        /// </summary>
        public void RestoreInto(GameState live, bool keepMemberAnchors = true)
        {
            live.Inventory.CopyFrom(saved.Inventory);
            live.Flags.CopyFrom(saved.Flags);
            live.CurrentNodeId = NodeId;
            live.Act = saved.Act;

            foreach (FamilyMemberState member in live.Members)
            {
                FamilyMemberState savedMember = saved.GetMember(member.Type);
                member.Health = savedMember.Health;
                if (!keepMemberAnchors)
                {
                    member.AnchorId = savedMember.AnchorId;
                }
            }
        }
    }
}
