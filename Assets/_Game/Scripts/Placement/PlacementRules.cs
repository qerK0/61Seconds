using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Text;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Placement rules (pure): the wife goes anywhere with room, the child must share or be adjacent
    /// to an adult's anchor, the player is placed last, capacity is respected.
    /// placements maps member -> anchor id (missing/null = unplaced).
    /// </summary>
    public static class PlacementRules
    {
        public static string AnchorOf(IReadOnlyDictionary<MemberType, string> placements, MemberType member)
        {
            return placements != null && placements.TryGetValue(member, out string id) ? id : null;
        }

        /// <summary>Members on an anchor, optionally ignoring one member (the one being moved).</summary>
        public static int Occupants(IReadOnlyDictionary<MemberType, string> placements, string anchorId, MemberType? ignore = null)
        {
            int count = 0;
            foreach (KeyValuePair<MemberType, string> pair in placements)
            {
                if (pair.Value == anchorId && (!ignore.HasValue || pair.Key != ignore.Value))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>The player can only be picked once the wife and child are placed.</summary>
        public static bool CanSelect(MemberType member, IReadOnlyDictionary<MemberType, string> placements)
        {
            if (member != MemberType.Player) return true;
            return AnchorOf(placements, MemberType.Wife) != null && AnchorOf(placements, MemberType.Child) != null;
        }

        /// <summary>True if the member may stand on the anchor. reasonKey is a UI text key when false.</summary>
        public static bool CanPlace(PlacementLayout layout, IReadOnlyDictionary<MemberType, string> placements, MemberType member, string anchorId, out string reasonKey)
        {
            reasonKey = null;
            AnchorInfo anchor = layout.Get(anchorId);
            if (anchor == null)
            {
                reasonKey = UiKeys.PlacementInvalid;
                return false;
            }

            if (Occupants(placements, anchorId, member) >= anchor.Capacity)
            {
                reasonKey = UiKeys.PlacementNoRoom;
                return false;
            }

            if (member == MemberType.Child && !HasAdultNear(layout, placements, anchorId))
            {
                reasonKey = UiKeys.PlacementChildAlone;
                return false;
            }

            return true;
        }

        /// <summary>True if an adult stands on the anchor or an adjacent one.</summary>
        public static bool HasAdultNear(PlacementLayout layout, IReadOnlyDictionary<MemberType, string> placements, string anchorId)
        {
            return IsNear(layout, AnchorOf(placements, MemberType.Player), anchorId)
                || IsNear(layout, AnchorOf(placements, MemberType.Wife), anchorId);
        }

        /// <summary>Child placed and next to an adult.</summary>
        public static bool IsChildSupported(PlacementLayout layout, IReadOnlyDictionary<MemberType, string> placements)
        {
            string childAnchor = AnchorOf(placements, MemberType.Child);
            return childAnchor != null && HasAdultNear(layout, placements, childAnchor);
        }

        private static bool IsNear(PlacementLayout layout, string adultAnchor, string anchorId)
        {
            if (adultAnchor == null || anchorId == null) return false;
            return adultAnchor == anchorId || layout.AreAdjacent(adultAnchor, anchorId);
        }
    }
}
