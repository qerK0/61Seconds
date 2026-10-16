using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Items
{
    /// <summary>Combine two bag items into one (order-independent). Costs building time and stamina.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Recipe", fileName = "recipe_new")]
    public sealed class Recipe : ScriptableObject
    {
        [Tooltip("Display name of the recipe.")]
        [SerializeField] private TextKey nameKey;

        [Tooltip("First input (order does not matter).")]
        [SerializeField] private Item inputA;

        [Tooltip("Second input.")]
        [SerializeField] private Item inputB;

        [Tooltip("Crafted item.")]
        [SerializeField] private Item output;

        [Tooltip("Building-timer seconds.")]
        [Min(0f)]
        [SerializeField] private float timeCost = 4f;

        [Tooltip("Stamina spent.")]
        [Min(0f)]
        [SerializeField] private float staminaCost = 2f;

        public TextKey NameKey => nameKey;
        public Item InputA => inputA;
        public Item InputB => inputB;
        public Item Output => output;
        public float TimeCost => timeCost;
        public float StaminaCost => staminaCost;

        /// <summary>True if a and b are this recipe's inputs in either order.</summary>
        public bool Matches(Item a, Item b)
        {
            if (a == null || b == null || inputA == null || inputB == null) return false;
            return (a.Id == inputA.Id && b.Id == inputB.Id) || (a.Id == inputB.Id && b.Id == inputA.Id);
        }

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(TextKey name, Item a, Item b, Item result, float seconds, float stamina)
        {
            nameKey = name;
            inputA = a;
            inputB = b;
            output = result;
            timeCost = seconds;
            staminaCost = stamina;
        }
    }
}
