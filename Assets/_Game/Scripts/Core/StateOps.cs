using SixtyOne.Text;

namespace SixtyOne.Core
{
    /// <summary>State mutations that also raise the matching GameEvents.</summary>
    public static class StateOps
    {
        /// <summary>Sets a flag; raises FlagChanged if the value changed.</summary>
        public static void SetFlag(GameState state, string flag, bool value)
        {
            if (state.Flags.Set(flag, value))
            {
                GameEvents.RaiseFlagChanged(flag, value);
            }
        }

        /// <summary>Adds an item to the bag. Raises ItemAdded, or a "bag full" message.</summary>
        public static bool TryAddItem(GameState state, string itemId)
        {
            if (state.Inventory.TryAdd(itemId))
            {
                GameEvents.RaiseItemAdded(itemId);
                return true;
            }

            if (state.Inventory.IsFull)
            {
                GameEvents.RaiseMessage(UiKeys.BagFull);
            }

            return false;
        }

        /// <summary>Removes an item; raises ItemRemoved if it was in the bag.</summary>
        public static bool RemoveItem(GameState state, string itemId)
        {
            if (!state.Inventory.Remove(itemId))
            {
                return false;
            }

            GameEvents.RaiseItemRemoved(itemId);
            return true;
        }

        /// <summary>Sets a member's health; raises MemberStateChanged if it changed.</summary>
        public static void SetHealth(GameState state, MemberType member, MemberHealth health)
        {
            FamilyMemberState m = state.GetMember(member);
            if (m.Health == health)
            {
                return;
            }

            m.Health = health;
            GameEvents.RaiseMemberStateChanged(member, health);
        }
    }
}
