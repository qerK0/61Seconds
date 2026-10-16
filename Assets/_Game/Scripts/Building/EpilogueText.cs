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

        /// <summary>Full body text: family line, borrow line, loops line.</summary>
        public static string Compose(EpilogueNode node, GameState state)
        {
            var text = new StringBuilder();
            text.Append(TextDB.GetDisplay(FamilyLineKey(node, state))).Append("\n\n");
            text.Append(TextDB.Format(BorrowLineKey(node, state), state.Borrowed.Count)).Append("\n\n");
            text.Append(TextDB.Format(node.LoopsKey.Key, state.LoopCount));
            return text.ToString();
        }
    }
}
