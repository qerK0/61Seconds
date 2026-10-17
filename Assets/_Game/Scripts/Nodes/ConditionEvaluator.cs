using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Nodes
{
    /// <summary>Evaluates conditions against a GameState. Pure logic.</summary>
    public static class ConditionEvaluator
    {
        public static bool Evaluate(Condition condition, GameState state)
        {
            if (condition == null)
            {
                return true;
            }

            switch (condition.Type)
            {
                case ConditionType.HasItem: return StateOps.HasItem(state, condition.ItemId);
                case ConditionType.LacksItem: return !StateOps.HasItem(state, condition.ItemId);
                case ConditionType.FlagTrue: return state.Flags.Get(condition.Flag);
                case ConditionType.FlagFalse: return !state.Flags.Get(condition.Flag);
                case ConditionType.MemberState: return state.GetMember(condition.Member).Health == condition.Health;
                case ConditionType.InRoom: return state.CurrentRoomId == condition.Room;
                case ConditionType.HasAnyItem:
                    foreach (Item item in condition.AnyItems)
                    {
                        if (item != null && StateOps.HasItem(state, item.Id)) return true;
                    }

                    return false;
                default: return false;
            }
        }

        /// <summary>True if every condition holds (an empty or null list is true).</summary>
        public static bool AllMet(IReadOnlyList<Condition> conditions, GameState state)
        {
            if (conditions == null)
            {
                return true;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                if (!Evaluate(conditions[i], state))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
