using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Items
{
    /// <summary>Using a bag item (food, water, first aid kit). Pure: the caller pays Item.UseSeconds.</summary>
    public static class ItemUse
    {
        /// <summary>Applies the item. Returns false if it is not usable on its own.</summary>
        public static bool Apply(GameState state, Item item, float staminaMax)
        {
            if (item == null || !item.IsUsable || !state.Inventory.Has(item.Id)) return false;

            if (item.StaminaRestore > 0f)
            {
                state.Stamina = SurvivalModel.RestoreStamina(state.Stamina, item.StaminaRestore, staminaMax);
            }

            if (item.HealsInjury)
            {
                foreach (FamilyMemberState member in state.Members)
                {
                    if (member.Health != MemberHealth.Injured) continue;
                    StateOps.SetHealth(state, member.Type, MemberHealth.Healthy);
                    StateOps.SetFlag(state, FlagNames.Injured(member.Type), false);
                }
            }

            if (item.ConsumedOnUse) StateOps.RemoveItem(state, item.Id);
            return true;
        }
    }
}
