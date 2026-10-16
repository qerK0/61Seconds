using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Nodes
{
    /// <summary>What a list of outcomes asks the caller to do next.</summary>
    public struct OutcomeResult
    {
        /// <summary>First GoToNode target, or null.</summary>
        public SceneNode NextNode;

        /// <summary>True if a Death outcome ran.</summary>
        public bool Died;
    }

    /// <summary>
    /// Applies state outcomes (flags, items, member health) immediately and reports
    /// GoToNode / Death back to the caller. Pure logic apart from raising GameEvents.
    /// </summary>
    public static class OutcomeExecutor
    {
        public static OutcomeResult Execute(IReadOnlyList<Outcome> outcomes, GameState state)
        {
            var result = new OutcomeResult();
            if (outcomes == null)
            {
                return result;
            }

            for (int i = 0; i < outcomes.Count; i++)
            {
                Outcome o = outcomes[i];
                if (o == null) continue;

                switch (o.Type)
                {
                    case OutcomeType.GoToNode:
                        if (result.NextNode == null) result.NextNode = o.TargetNode;
                        break;
                    case OutcomeType.Death:
                        result.Died = true;
                        break;
                    case OutcomeType.SetFlag:
                        StateOps.SetFlag(state, o.Flag, o.FlagValue);
                        break;
                    case OutcomeType.AddItem:
                        if (o.Item != null) StateOps.TryAddItem(state, o.Item.Id);
                        break;
                    case OutcomeType.RemoveItem:
                        if (o.Item != null) StateOps.RemoveItem(state, o.Item.Id);
                        break;
                    case OutcomeType.SetMemberState:
                        StateOps.SetHealth(state, o.Member, o.Health);
                        break;
                    case OutcomeType.TraderGive:
                        Item given = SixtyOne.Items.TraderLogic.Pick(o.Trader, state.Inventory);
                        if (given != null) StateOps.TryAddItem(state, given.Id);
                        break;
                    case OutcomeType.ShowMessage:
                        if (!o.MessageKey.IsEmpty) GameEvents.RaiseMessage(o.MessageKey.Key);
                        break;
                }
            }

            return result;
        }
    }
}
