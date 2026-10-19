using System.Collections.Generic;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Items
{
    /// <summary>
    /// A needs-aware trader: gives the FIRST item of the ordered upcoming requirements the player lacks;
    /// if nothing is missing, gives the first fallback (food, water) the player lacks, else the first fallback.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Trader Profile", fileName = "trader_new")]
    public sealed class TraderProfile : ScriptableObject
    {
        [Tooltip("Upcoming requirements in the order the player will need them.")]
        [SerializeField] private List<Item> needs = new List<Item>();

        [Tooltip("Given when nothing is missing (e.g. food, water).")]
        [SerializeField] private List<Item> fallback = new List<Item>();

        public IReadOnlyList<Item> Needs => needs;
        public IReadOnlyList<Item> Fallback => fallback;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(IEnumerable<Item> orderedNeeds, IEnumerable<Item> fallbackItems)
        {
            needs = new List<Item>(orderedNeeds);
            fallback = new List<Item>(fallbackItems);
        }
    }
}
