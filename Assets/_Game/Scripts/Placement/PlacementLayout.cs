using System;
using System.Collections.Generic;

namespace SixtyOne.Placement
{
    /// <summary>Safe spots are hazard-free; Trap spots look tempting but are deadly.</summary>
    public enum AnchorTag
    {
        Safe,
        Trap
    }

    /// <summary>Pure-data view of one anchor (no MonoBehaviour), used by rules and resolver.</summary>
    public sealed class AnchorInfo
    {
        public string Id { get; }
        public AnchorTag Tag { get; }
        public int Capacity { get; }
        public Hazard Hazard { get; }
        public List<string> Adjacent { get; }

        public AnchorInfo(string id, AnchorTag tag, int capacity, Hazard hazard = null, IEnumerable<string> adjacent = null)
        {
            Id = id;
            Tag = tag;
            Capacity = capacity < 1 ? 1 : capacity;
            Hazard = hazard;
            Adjacent = adjacent != null ? new List<string>(adjacent) : new List<string>();
        }
    }

    /// <summary>All anchors of a room plus the hazard for members left unplaced.</summary>
    public sealed class PlacementLayout
    {
        private readonly Dictionary<string, AnchorInfo> anchors = new Dictionary<string, AnchorInfo>(StringComparer.Ordinal);

        /// <summary>Hits members who were never placed when the quake starts.</summary>
        public Hazard UnplacedHazard { get; set; }

        public IEnumerable<AnchorInfo> Anchors => anchors.Values;

        public PlacementLayout Add(AnchorInfo anchor)
        {
            anchors[anchor.Id] = anchor;
            return this;
        }

        public AnchorInfo Get(string id)
        {
            return !string.IsNullOrEmpty(id) && anchors.TryGetValue(id, out AnchorInfo a) ? a : null;
        }

        public bool Contains(string id) => Get(id) != null;

        /// <summary>True if either anchor lists the other (adjacency is symmetric).</summary>
        public bool AreAdjacent(string a, string b)
        {
            AnchorInfo infoA = Get(a);
            AnchorInfo infoB = Get(b);
            if (infoA == null || infoB == null) return false;
            return infoA.Adjacent.Contains(b) || infoB.Adjacent.Contains(a);
        }
    }
}
