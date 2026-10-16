using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>
    /// A bag item. Usable items (food, water, first aid) are used by selecting their slot twice.
    /// Trap items are pickable but useless: they waste a slot and time.
    /// </summary>
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

        [Tooltip("A valuable that is useless in an earthquake (wastes a bag slot and time).")]
        [SerializeField] private bool isTrap;

        [Tooltip("Stamina restored when used (food, water). 0 = not a stamina item.")]
        [Min(0f)]
        [SerializeField] private float staminaRestore;

        [Tooltip("Using it treats every injured family member (first aid kit).")]
        [SerializeField] private bool healsInjury;

        [Tooltip("Removed from the bag when used.")]
        [SerializeField] private bool consumedOnUse = true;

        [Tooltip("Building-timer seconds it takes to use.")]
        [Min(0f)]
        [SerializeField] private float useSeconds = 2f;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey NameKey => nameKey;
        public TextKey DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public bool IsTrap => isTrap;
        public float StaminaRestore => staminaRestore;
        public bool HealsInjury => healsInjury;
        public bool ConsumedOnUse => consumedOnUse;
        public float UseSeconds => useSeconds;

        /// <summary>True if selecting the slot twice does something.</summary>
        public bool IsUsable => staminaRestore > 0f || healsInjury;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string itemId, TextKey itemName, TextKey description, Sprite itemIcon = null)
        {
            id = itemId;
            nameKey = itemName;
            descriptionKey = description;
            if (itemIcon != null) icon = itemIcon;
            isTrap = false;
            staminaRestore = 0f;
            healsInjury = false;
            consumedOnUse = true;
            useSeconds = 2f;
        }

        /// <summary>Makes the item usable from the bag.</summary>
        public void ConfigureUse(float stamina, bool heals, float seconds, bool consumed = true)
        {
            staminaRestore = stamina;
            healsInjury = heals;
            useSeconds = seconds;
            consumedOnUse = consumed;
        }

        /// <summary>Marks the item as a useless valuable.</summary>
        public void ConfigureTrap()
        {
            isTrap = true;
        }
    }
}
