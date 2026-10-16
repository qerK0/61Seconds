using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Items
{
    /// <summary>All recipes the bag can craft.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Recipe Book", fileName = "RecipeBook")]
    public sealed class RecipeBook : ScriptableObject
    {
        [Tooltip("Recipes checked in order.")]
        [SerializeField] private List<Recipe> recipes = new List<Recipe>();

        public IReadOnlyList<Recipe> Recipes => recipes;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(IEnumerable<Recipe> all)
        {
            recipes = new List<Recipe>(all);
        }
    }
}
