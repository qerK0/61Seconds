using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>A bag item. Trap items (gold, laptop...) kill when picked up.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Item", fileName = "item_new")]
    public sealed class Item : ScriptableObject
    {
        [Tooltip("Unique id stored in the inventory.")]
        [SerializeField] private string id;

        [Tooltip("Display name.")]
        [SerializeField] private TextKey nameKey;

        [Tooltip("Short description.")]
        [SerializeField] private TextKey descriptionKey;

        [Tooltip("Bag icon (artist).")]
        [SerializeField] private Sprite icon;

        [Tooltip("Picking this up is a do-not-do (wasting time on valuables).")]
        [SerializeField] private bool isTrap;

        [Tooltip("Death used when a trap item is picked up.")]
        [SerializeField] private DeathData trapDeath = new DeathData();

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey NameKey => nameKey;
        public TextKey DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public bool IsTrap => isTrap;
        public DeathData TrapDeath => trapDeath;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string itemId, TextKey itemName, TextKey description, Sprite itemIcon = null)
        {
            id = itemId;
            nameKey = itemName;
            descriptionKey = description;
            if (itemIcon != null) icon = itemIcon;
            isTrap = false;
            trapDeath = new DeathData();
        }

        /// <summary>Marks the item as a trap pickup.</summary>
        public void ConfigureTrap(DeathData death)
        {
            isTrap = true;
            trapDeath = death;
        }
    }
}
