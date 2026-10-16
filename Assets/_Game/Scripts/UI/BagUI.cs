using SixtyOne.Core;
using SixtyOne.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Four bag slots with icon and name.</summary>
    public sealed class BagUI : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Item registry for icons and names.")]
        [SerializeField] private GameContent content;

        [Tooltip("Slot icon images (4).")]
        [SerializeField] private Image[] slotIcons = new Image[Inventory.DefaultCapacity];

        [Tooltip("Slot name labels (4).")]
        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[Inventory.DefaultCapacity];

        private void OnEnable()
        {
            GameEvents.ItemAdded += OnChanged;
            GameEvents.ItemRemoved += OnChanged;
            GameEvents.Rewound += OnChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.ItemAdded -= OnChanged;
            GameEvents.ItemRemoved -= OnChanged;
            GameEvents.Rewound -= OnChanged;
        }

        private void OnChanged(string _) => Refresh();

        public void Refresh()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            int count = Mathf.Max(slotIcons.Length, slotLabels.Length);
            for (int i = 0; i < count; i++)
            {
                string itemId = gm != null && i < gm.State.Inventory.Count ? gm.State.Inventory.Items[i] : null;
                Item item = itemId != null && content != null ? content.FindItem(itemId) : null;

                if (i < slotIcons.Length && slotIcons[i] != null)
                {
                    Image icon = slotIcons[i];
                    icon.enabled = itemId != null;
                    if (item != null && item.Icon != null) icon.sprite = item.Icon;
                }

                if (i < slotLabels.Length && slotLabels[i] != null)
                {
                    slotLabels[i].text = item != null ? item.NameKey.Display : itemId ?? string.Empty;
                }
            }
        }
    }
}
