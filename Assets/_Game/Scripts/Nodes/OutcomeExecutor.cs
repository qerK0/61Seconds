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

        /// <summary>Item ids a TakeItems outcome removed from the bag (in order), or null.</summary>
        public List<string> Taken;
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
                        if (o.Item != null) StateOps.TryAddItem(state, o.Item.Id, o.Item.IsKeyItem);
                        break;
                    case OutcomeType.RemoveItem:
                        if (o.Item != null) StateOps.RemoveItem(state, o.Item.Id);
                        break;
                    case OutcomeType.SetMemberState:
                        StateOps.SetHealth(state, o.Member, o.Health);
                        break;
                    case OutcomeType.TraderGive:
                        Item given = SixtyOne.Items.TraderLogic.Pick(o.Trader, state);
                        if (given != null) StateOps.TryAddItem(state, given.Id, given.IsKeyItem);
                        break;
                    case OutcomeType.GiveItem:
                        if (o.Item == null) break;
                        bool received = StateOps.GiveOrDrop(state, o.Item.Id, o.Item.IsKeyItem, state.CurrentRoomId);
                        string giver = o.MessageKey.IsEmpty ? string.Empty : o.MessageKey.Display;
                        GameEvents.RaiseQueuedMessage(SixtyOne.Text.TextDB.Format(received ? SixtyOne.Text.UiKeys.GiftReceived : SixtyOne.Text.UiKeys.GiftDropped,
                            giver, o.Item.NameKey.Display));
                        break;
                    case OutcomeType.ShowMessage:
                        if (!o.MessageKey.IsEmpty) GameEvents.RaiseMessage(o.MessageKey.Key);
                        break;
                    case OutcomeType.TakeItems:
                        if (result.Taken == null) result.Taken = new List<string>();
                        result.Taken.AddRange(TakeItems(o, state));
                        break;
                }
            }

            return result;
        }

        private static List<string> TakeItems(Outcome o, GameState state)
        {
            var ids = new HashSet<string>();
            var keyItems = new HashSet<string>();
            foreach (Item item in o.TakeItemsList)
            {
                if (item == null) continue;
                ids.Add(item.Id);
                if (item.IsKeyItem) keyItems.Add(item.Id);
            }

            if (o.FallbackItem != null && o.FallbackItem.IsKeyItem) keyItems.Add(o.FallbackItem.Id);
            return SixtyOne.Items.RobberLogic.Take(state, ids, o.FallbackItem != null ? o.FallbackItem.Id : null, o.FallbackCount,
                o.ExcludeKeyItems, keyItems.Contains);
        }
    }
}
