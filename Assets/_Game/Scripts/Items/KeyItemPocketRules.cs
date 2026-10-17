using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Items
{
    /// <summary>What a click on a backpack key-item pocket does.</summary>
    public enum PocketAction
    {
        /// <summary>Play the current node's choice that needs this item (e.g. the whistle at the dog).</summary>
        UseChoice,

        /// <summary>Switch the flashlight on / off (FlagNames.FlashlightOff).</summary>
        ToggleFlashlight,

        /// <summary>No use here: show the item's name.</summary>
        ShowName
    }

    /// <summary>
    /// Pure rules for the backpack's key-item pockets (left = flashlight, right = whistle). An owned item first plays
    /// an available choice of the current node that requires it; otherwise the flashlight toggles and anything else
    /// (or a missing item) shows its name.
    /// </summary>
    public static class KeyItemPocketRules
    {
        public const string FlashlightId = "flashlight";
        public const string WhistleId = "whistle";

        public static PocketAction Decide(SceneNode node, GameState state, string itemId, out int choiceIndex)
        {
            choiceIndex = -1;
            if (state == null || string.IsNullOrEmpty(itemId) || !StateOps.HasItem(state, itemId)) return PocketAction.ShowName;
            choiceIndex = FindUseChoice(node, state, itemId);
            if (choiceIndex >= 0) return PocketAction.UseChoice;
            return itemId == FlashlightId ? PocketAction.ToggleFlashlight : PocketAction.ShowName;
        }

        /// <summary>Index of the first clickable, unlocked choice of the node that requires the item, or -1.</summary>
        public static int FindUseChoice(SceneNode node, GameState state, string itemId)
        {
            if (node == null || state == null) return -1;
            foreach (ChoiceView view in ChoiceView.Build(node, state))
            {
                if (view.Locked || view.Choice == null) continue;
                foreach (Condition condition in view.Choice.Conditions)
                {
                    if (condition != null && condition.Type == ConditionType.HasItem && condition.ItemId == itemId) return view.Index;
                }
            }

            return -1;
        }

        /// <summary>True when the owned flashlight is switched on (it is on by default).</summary>
        public static bool FlashlightOn(GameState state) =>
            state != null && StateOps.HasItem(state, FlashlightId) && !state.Flags.Get(FlagNames.FlashlightOff);
    }
}
