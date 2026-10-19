using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// The open container's contents next to the bag (same slot art). Click an item to move it into the bag (bag
    /// full = the bag flashes + denied cue), "Hepsini al" takes all that fit, "Kapat" closes. A bag click puts that
    /// item back (BagUI checks LootEvents.Open). Neighbours' furniture is titled "Ödünç al". Closes when the player
    /// runs off, on node changes, deaths and rewinds. Listens to LootEvents / InventoryEvents only.
    /// </summary>
    public sealed class LootWindow : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Item registry (icons, names, key items).")]
        [SerializeField] private GameContent content;

        [Tooltip("Shown / hidden with the window.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Title (container name, or 'Ödünç al' in neighbours' flats).")]
        [SerializeField] private TMP_Text title;

        [Tooltip("Container name under the title in neighbours' flats. Optional.")]
        [SerializeField] private TMP_Text subtitle;

        [Tooltip("'Boş' when nothing is left.")]
        [SerializeField] private TMP_Text emptyLabel;

        [Tooltip("Slot buttons.")]
        [SerializeField] private Button[] slotButtons = new Button[0];

        [Tooltip("Slot icons.")]
        [SerializeField] private Image[] slotIcons = new Image[0];

        [Tooltip("Slot name labels.")]
        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[0];

        [Tooltip("Take everything that fits.")]
        [SerializeField] private Button takeAllButton;

        [Tooltip("Close the window.")]
        [SerializeField] private Button closeButton;

        [Tooltip("The player's motor: running off closes the window. Optional.")]
        [SerializeField] private CharacterMotor runner;

        private LootView view;
        private readonly List<string> shown = new List<string>();

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        private void Awake()
        {
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => OnSlot(index));
            }

            if (takeAllButton != null) takeAllButton.onClick.AddListener(TakeAll);
            if (closeButton != null) closeButton.onClick.AddListener(LootEvents.RaiseClosed);
            UIVisibility.Set(group, false);
        }

        private void OnEnable()
        {
            LootEvents.OnOpened += OnOpened;
            LootEvents.OnClosed += OnClosed;
            InventoryEvents.OnInventoryChanged += Refresh;
            GameEvents.NodeEntered += CloseOn;
            GameEvents.Rewound += CloseOn;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            LootEvents.OnOpened -= OnOpened;
            LootEvents.OnClosed -= OnClosed;
            InventoryEvents.OnInventoryChanged -= Refresh;
            GameEvents.NodeEntered -= CloseOn;
            GameEvents.Rewound -= CloseOn;
            GameEvents.Died -= OnDied;
        }

        private void Update()
        {
            if (view != null && runner != null && runner.IsMoving) LootEvents.RaiseClosed();
        }

        private void OnOpened(LootView opened)
        {
            view = opened;
            UIVisibility.Set(group, true);
            Refresh();
        }

        private void OnClosed()
        {
            view = null;
            UIVisibility.Set(group, false);
        }

        private void CloseOn(string _) => LootEvents.RaiseClosed();
        private void OnDied(DeathInfo _) => LootEvents.RaiseClosed();

        private bool IsKeyItem(string itemId)
        {
            Item item = content != null ? content.FindItem(itemId) : null;
            return item != null && item.IsKeyItem;
        }

        private void OnSlot(int index)
        {
            GameManager gm = Gm;
            if (view == null || gm == null || gm.IsDead || InputGate.IsBlocked || index < 0 || index >= shown.Count) return;
            string itemId = shown[index];
            if (LootLogic.Take(gm.State, view.ContainerId, itemId, IsKeyItem(itemId), view.Borrowed) == LootTakeResult.Taken && view.Borrowed)
            {
                GameEvents.RaiseMessage(UiKeys.Borrowed);
            }

            Refresh();
        }

        private void TakeAll()
        {
            GameManager gm = Gm;
            if (view == null || gm == null || gm.IsDead || InputGate.IsBlocked) return;
            if (LootLogic.TakeAll(gm.State, view.ContainerId, IsKeyItem, view.Borrowed) > 0 && view.Borrowed) GameEvents.RaiseMessage(UiKeys.Borrowed);
            Refresh();
        }

        public void Refresh()
        {
            if (view == null) return;
            GameManager gm = Gm;
            shown.Clear();
            if (gm != null) shown.AddRange(gm.State.Containers.ItemsIn(view.ContainerId));

            string nameText = string.IsNullOrEmpty(view.NameKey) ? string.Empty : TextDB.GetDisplay(view.NameKey);
            if (title != null) title.text = view.Borrowed ? TextDB.GetDisplay(UiKeys.LootBorrowTitle) : nameText;
            if (subtitle != null)
            {
                subtitle.gameObject.SetActive(view.Borrowed);
                subtitle.text = nameText;
            }

            if (emptyLabel != null) emptyLabel.gameObject.SetActive(shown.Count == 0);
            if (takeAllButton != null) takeAllButton.interactable = shown.Count > 0;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                string itemId = i < shown.Count ? shown[i] : null;
                Item item = itemId != null && content != null ? content.FindItem(itemId) : null;
                if (slotButtons[i] != null) slotButtons[i].interactable = itemId != null;
                if (i < slotIcons.Length && slotIcons[i] != null)
                {
                    // The area above the name label, with the fixed 12% inset.
                    SlotIcon.Apply(slotIcons[i], itemId != null ? item : null, new Vector2(SlotIcon.Inset, 0.32f), new Vector2(1f - SlotIcon.Inset, 0.92f));
                }

                if (i < slotLabels.Length && slotLabels[i] != null)
                {
                    slotLabels[i].text = item != null ? item.NameKey.Display : itemId ?? string.Empty;
                }
            }
        }
    }
}
