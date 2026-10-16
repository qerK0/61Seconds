using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>A bag item. Trap items (gold, laptop, console) are pickable but useless: they waste a slot and time.</summary>
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

        [Tooltip("A valuable that is useless in an earthquake (wastes a bag slot and prep time).")]
        [SerializeField] private bool isTrap;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey NameKey => nameKey;
        public TextKey DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
        public bool IsTrap => isTrap;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string itemId, TextKey itemName, TextKey description, Sprite itemIcon = null)
        {
            id = itemId;
            nameKey = itemName;
            descriptionKey = description;
            if (itemIcon != null) icon = itemIcon;
            isTrap = false;
        }

        /// <summary>Marks the item as a useless valuable.</summary>
        public void ConfigureTrap()
        {
            isTrap = true;
        }
    }
}
