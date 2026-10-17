using System.Text;
using SixtyOne.Core;
using SixtyOne.Text;

namespace SixtyOne.Building
{
    /// <summary>Pure epilogue line selection (unit-tested) and composition.</summary>
    public static class EpilogueText
    {
        public static string FamilyLineKey(EpilogueNode node, GameState state)
        {
            foreach (FamilyMemberState member in state.Members)
            {
                if (member.Health != MemberHealth.Healthy) return node.FamilyInjuredKey.Key;
            }

            return node.FamilyHealthyKey.Key;
        }

        public static string BorrowLineKey(EpilogueNode node, GameState state)
        {
            if (state.Borrowed.Count == 0) return node.BorrowNoneKey.Key;
            return state.Flags.Get(FlagNames.LeftNote) ? node.BorrowWithNoteKey.Key : node.BorrowWithoutNoteKey.Key;
        }

        /// <summary>The building line: it collapsed behind us (timer ran out on the street), else the time it still had.</summary>
        public static string BuildingLineKey(EpilogueNode node, GameState state)
        {
            return state.Flags.Get(FlagNames.CollapsedBehind) ? node.CollapsedBehindKey.Key : node.TimeLeftKey.Key;
        }

        /// <summary>The robbery line, or null if nothing happened on the stairs.</summary>
        public static string RobbedLineKey(EpilogueNode node, GameState state)
        {
            return state.Flags.Get(FlagNames.RobbedAtExit) && !node.RobbedKey.IsEmpty ? node.RobbedKey.Key : null;
        }

        /// <summary>Full body text: family line, building line, robbery line, borrow line, loops line.</summary>
        public static string Compose(EpilogueNode node, GameState state)
        {
            var text = new StringBuilder();
            text.Append(TextDB.GetDisplay(FamilyLineKey(node, state))).Append("\n\n");
            string building = BuildingLineKey(node, state);
            if (!string.IsNullOrEmpty(building))
            {
                text.Append(TextDB.Format(building, UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, state.BuildingTime)))).Append("\n\n");
            }

            string robbed = RobbedLineKey(node, state);
            if (robbed != null) text.Append(TextDB.GetDisplay(robbed)).Append("\n\n");
            text.Append(TextDB.Format(BorrowLineKey(node, state), state.Borrowed.Count)).Append("\n\n");
            text.Append(TextDB.Format(node.LoopsKey.Key, state.LoopCount));
            return text.ToString();
        }
    }
}
