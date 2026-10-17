using SixtyOne.Core;
using SixtyOne.Nodes;

namespace SixtyOne.Items
{
    /// <summary>Pure crafting: find a recipe for two items and swap the inputs for the output.</summary>
    public static class Crafting
    {
        /// <summary>First recipe matching a and b in either order, or null.</summary>
        public static Recipe Find(RecipeBook book, Item a, Item b)
        {
            if (book == null) return null;
            foreach (Recipe recipe in book.Recipes)
            {
                if (recipe != null && recipe.Matches(a, b)) return recipe;
            }

            return null;
        }

        /// <summary>Removes both inputs and adds the output (the freed slot always fits it). False if inputs are missing.</summary>
        public static bool Craft(GameState state, Recipe recipe)
        {
            if (recipe == null || recipe.Output == null) return false;
            string a = recipe.InputA.Id;
            string b = recipe.InputB.Id;
            if (!state.Inventory.Has(a) || !state.Inventory.Has(b)) return false;
            if (a == b && CountOf(state, a) < 2) return false;

            StateOps.RemoveItem(state, a);
            StateOps.RemoveItem(state, b);
            StateOps.TryAddItem(state, recipe.Output.Id, recipe.Output.IsKeyItem);
            return true;
        }

        /// <summary>True if the bag holds enough bedsheets to tie a rope.</summary>
        public static bool CanTieRope(GameState state, string bedsheetId, int required)
        {
            return !string.IsNullOrEmpty(bedsheetId) && required > 0 && state.Inventory.CountOf(bedsheetId) >= required;
        }

        /// <summary>
        /// Replaces `required` bedsheets with one rope (frees required-1 slots). False if there are too few sheets.
        /// The caller pays the time/stamina cost.
        /// </summary>
        public static bool TieRope(GameState state, string bedsheetId, string ropeId, int required)
        {
            if (string.IsNullOrEmpty(ropeId) || !CanTieRope(state, bedsheetId, required)) return false;
            for (int i = 0; i < required; i++) StateOps.RemoveItem(state, bedsheetId);
            StateOps.TryAddItem(state, ropeId);
            return true;
        }

        private static int CountOf(GameState state, string id) => state.Inventory.CountOf(id);
    }
}
