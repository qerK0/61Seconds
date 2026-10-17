using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Placement
{
    /// <summary>How a member gets to a new spot.</summary>
    public enum MoveMode
    {
        /// <summary>Instant (rewinds, first show).</summary>
        Snap,

        /// <summary>Runs there with the run animation (placement, room arrivals).</summary>
        Run
    }

    /// <summary>Pure movement rules for members (unit-tested).</summary>
    public static class MemberMoveRules
    {
        /// <summary>Rewinds always snap; everything else runs (Reduce Effects only reduces camera/visual effects).</summary>
        public static MoveMode ArrivalMode(bool rewind, bool reduceEffects)
        {
            return rewind ? MoveMode.Snap : MoveMode.Run;
        }
    }

    /// <summary>The building's quake counter (pure).</summary>
    public static class QuakeRules
    {
        /// <summary>Quake count after a quake: its configured number (never lower than before), else one more.</summary>
        public static int CountAfter(int before, int quakeNumber)
        {
            return quakeNumber > 0 ? System.Math.Max(before, quakeNumber) : before + 1;
        }
    }

    /// <summary>
    /// Placement runs in flight. Reserving writes the anchor into GameState at once (capacity and adjacency rules
    /// see it immediately, so the next member can be queued while the first is still running); FinishAll hands
    /// back every run that has not arrived so the controller can snap the last bit before the quake resolves.
    /// Pure C#.
    /// </summary>
    public sealed class PlacementRuns
    {
        private readonly Dictionary<MemberType, string> pending = new Dictionary<MemberType, string>();

        public int Count => pending.Count;

        /// <summary>Reserves the anchor for the member (GameState) and records the run.</summary>
        public void Reserve(GameState state, MemberType member, string anchorId)
        {
            state.GetMember(member).AnchorId = anchorId;
            pending[member] = anchorId;
        }

        /// <summary>The member reached its anchor.</summary>
        public void Arrived(MemberType member) => pending.Remove(member);

        public bool IsRunning(MemberType member) => pending.ContainsKey(member);

        /// <summary>Anchor the member is running to, or null.</summary>
        public string TargetOf(MemberType member) => pending.TryGetValue(member, out string id) ? id : null;

        /// <summary>Every run still in flight (member, target anchor); clears the list.</summary>
        public List<KeyValuePair<MemberType, string>> FinishAll()
        {
            var unfinished = new List<KeyValuePair<MemberType, string>>(pending);
            pending.Clear();
            return unfinished;
        }

        public void Clear() => pending.Clear();
    }
}
