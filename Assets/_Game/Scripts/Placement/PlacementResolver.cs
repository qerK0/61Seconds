using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Placement
{
    /// <summary>Result of the quake for one member.</summary>
    public sealed class PlacementResult
    {
        public MemberType Member { get; }
        /// <summary>Anchor the member was on (null = unplaced).</summary>
        public string AnchorId { get; }
        /// <summary>Hazard that hit (null if none or child-alone).</summary>
        public Hazard Hazard { get; }
        public HazardEffect Effect { get; }
        /// <summary>The child was trapped because no adult was near.</summary>
        public bool ChildAlone { get; }

        /// <summary>Death or Trapped: triggers the rewind.</summary>
        public bool IsFatal => Effect == HazardEffect.Death || Effect == HazardEffect.Trapped;

        public PlacementResult(MemberType member, string anchorId, Hazard hazard, HazardEffect effect, bool childAlone = false)
        {
            Member = member;
            AnchorId = anchorId;
            Hazard = hazard;
            Effect = effect;
            ChildAlone = childAlone;
        }
    }

    /// <summary>Resolves hazards against member placements. Pure C#, unit-testable.</summary>
    public static class PlacementResolver
    {
        private static readonly MemberType[] Order = { MemberType.Player, MemberType.Wife, MemberType.Child };

        /// <summary>One result per member, in order Player, Wife, Child.</summary>
        public static List<PlacementResult> Resolve(PlacementLayout layout, IReadOnlyDictionary<MemberType, string> placements, int act)
        {
            var results = new List<PlacementResult>(Order.Length);
            foreach (MemberType member in Order)
            {
                string anchorId = PlacementRules.AnchorOf(placements, member);
                if (anchorId != null && !layout.Contains(anchorId))
                {
                    anchorId = null;
                }

                Hazard hazard = anchorId == null ? layout.UnplacedHazard : layout.Get(anchorId).Hazard;
                HazardEffect effect = hazard != null && hazard.IsActiveInAct(act) ? hazard.EffectFor(member) : HazardEffect.None;

                if (effect == HazardEffect.None)
                {
                    hazard = null;
                }

                if (member == MemberType.Child && effect == HazardEffect.None && !PlacementRules.IsChildSupported(layout, placements))
                {
                    results.Add(new PlacementResult(member, anchorId, null, HazardEffect.Trapped, childAlone: true));
                    continue;
                }

                results.Add(new PlacementResult(member, anchorId, hazard, effect));
            }

            return results;
        }

        /// <summary>First Death/Trapped result, or null if everyone survived.</summary>
        public static PlacementResult FirstFatal(List<PlacementResult> results)
        {
            return results.Find(r => r.IsFatal);
        }
    }
}
