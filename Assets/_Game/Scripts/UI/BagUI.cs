using System.Collections;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// The bag (LevelConfig.MaxInventorySlots, 6 by default). Click a slot to select it; click it again to use it
    /// (food, water, first aid; a bedsheet ties the rope once there are enough); click another slot to combine
    /// the two (crafting recipes). With a slot selected, Drop leaves the item in the current room (it can be
    /// picked up again there). Key items never appear here (see KeyItemCornerHUD). Flashes red when full.
    /// </summary>
    public sealed class BagUI : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Item registry for icons and names.")]
        [SerializeField] private GameContent content;

        [Tooltip("Max stamina (food/water) source.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Recipes for combining two slots. Optional.")]
        [SerializeField] private RecipeBook recipes;

        [Tooltip("Slot icon images (one per slot).")]
        [SerializeField] private Image[] slotIcons = new Image[Inventory.DefaultCapacity];

        [Tooltip("Slot name labels (one per slot).")]
        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[Inventory.DefaultCapacity];

        [Tooltip("Slot backgrounds, flashed red when the bag is full and tinted when selected.")]
        [SerializeField] private Image[] slotBackgrounds = new Image[Inventory.DefaultCapacity];

        [Tooltip("Slot buttons (one per slot).")]
        [SerializeField] private Button[] slotButtons = new Button[Inventory.DefaultCapacity];

        [Tooltip("Drop button, shown while a slot is selected. Optional.")]
        [SerializeField] private Button dropButton;

        [Tooltip("'Tie a rope' prompt, shown while the bag holds enough bedsheets. Optional.")]
        [SerializeField] private Button ropeButton;

        [Tooltip("'used / total' slot counter. Optional.")]
        [SerializeField] private TMP_Text countLabel;

        [Tooltip("Building-timer seconds to drop an item.")]
        [Min(0f)]
        [SerializeField] private float dropSeconds = 0.5f;

        [Tooltip("Flash color for 'bag full'.")]
        [SerializeField] private Color fullFlashColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        [Tooltip("Tint of the selected slot.")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.82f, 0.2f, 0.45f);

        private Color[] backgroundColors;
        private int selected = -1;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        private void Awake()
        {
            CacheColors();
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => OnSlotClicked(index));
            }

            if (dropButton != null) dropButton.onClick.AddListener(DropSelected);
            if (ropeButton != null) ropeButton.onClick.AddListener(() => TieRope(Gm));
        }

        private void OnEnable()
        {
            GameEvents.ItemAdded += OnChanged;
            GameEvents.ItemRemoved += OnChanged;
            GameEvents.Rewound += OnChanged;
            GameEvents.Message += OnMessage;
            GameEvents.Died += OnDied;
            InventoryEvents.OnInventoryChanged += OnInventoryChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.ItemAdded -= OnChanged;
            GameEvents.ItemRemoved -= OnChanged;
            GameEvents.Rewound -= OnChanged;
            GameEvents.Message -= OnMessage;
            GameEvents.Died -= OnDied;
            InventoryEvents.OnInventoryChanged -= OnInventoryChanged;
        }

        private void OnInventoryChanged() => OnChanged(null);

        private LevelConfig Config => survival != null ? survival.Config : null;

        private void OnChanged(string _)
        {
            selected = -1;
            Refresh();
        }

        private void OnDied(DeathInfo info) => OnChanged(null);

        /// <summary>Item in a slot, or null.</summary>
        private Item ItemAt(int slot)
        {
            GameManager gm = Gm;
            if (gm == null || slot < 0 || slot >= gm.State.Inventory.Count) return null;
            string id = gm.State.Inventory.Items[slot];
            return content != null ? content.FindItem(id) : null;
        }

        private void OnSlotClicked(int slot)
        {
            GameManager gm = Gm;
            if (gm == null || gm.IsDead || InputGate.IsBlocked) return;

            Item item = ItemAt(slot);
            if (item == null)
            {
                selected = -1;
                Refresh();
                return;
            }

            if (selected < 0)
            {
                selected = slot;
                GameEvents.RaiseMessage(UiKeys.BagSelectHint);
                Refresh();
                return;
            }

            int first = selected;
            selected = -1;
            if (first == slot) Use(gm, item);
            else Combine(gm, ItemAt(first), item);
            Refresh();
        }

        private void Use(GameManager gm, Item item)
        {
            LevelConfig config = Config;
            if (config != null && config.BedsheetItem != null && item.Id == config.BedsheetItem.Id)
            {
                TieRope(gm);
                return;
            }

            float max = survival != null && survival.Config != null ? survival.Config.StaminaMax : 100f;
            if (!ItemUse.Apply(gm.State, item, max))
            {
                GameEvents.RaiseMessage(UiKeys.ItemNotUsable);
                return;
            }

            ActionCostRouter.SpendTime(item.UseSeconds);
            GameEvents.RaiseMessage(UiKeys.ItemUsed);
        }

        private void Combine(GameManager gm, Item a, Item b)
        {
            Recipe recipe = recipes != null ? Crafting.Find(recipes, a, b) : null;
            if (recipe == null || !Crafting.Craft(gm.State, recipe))
            {
                GameEvents.RaiseMessage(UiKeys.CraftFail);
                return;
            }

            ActionCostRouter.SpendStamina(recipe.StaminaCost);
            ActionCostRouter.SpendTime(recipe.TimeCost);
            GameEvents.RaiseMessage(UiKeys.CraftDone);
        }

        /// <summary>Ties the configured number of bedsheets into one rope (interaction prompt / bedsheet use).</summary>
        private void TieRope(GameManager gm)
        {
            LevelConfig config = Config;
            if (gm == null || gm.IsDead || InputGate.IsBlocked || config == null || config.BedsheetItem == null || config.BedsheetRopeItem == null) return;
            if (!Crafting.TieRope(gm.State, config.BedsheetItem.Id, config.BedsheetRopeItem.Id, config.RequiredBedsheetsForRope))
            {
                GameEvents.RaiseMessage(UiKeys.RopeNeedsSheets);
                return;
            }

            ActionCostRouter.SpendStamina(config.RopeCraftStamina);
            ActionCostRouter.SpendTime(config.RopeCraftSeconds);
            GameEvents.RaiseMessage(UiKeys.RopeTied);
            Refresh();
        }

        /// <summary>Drops the selected bag item into the current room.</summary>
        private void DropSelected()
        {
            GameManager gm = Gm;
            if (gm == null || gm.IsDead || InputGate.IsBlocked || selected < 0 || selected >= gm.State.Inventory.Count) return;
            string id = gm.State.Inventory.Items[selected];
            selected = -1;
            if (StateOps.DropItem(gm.State, id, gm.State.CurrentRoomId))
            {
                ActionCostRouter.SpendTime(dropSeconds);
                GameEvents.RaiseMessage(UiKeys.ItemDropped);
            }

            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = Gm;
            if (dropButton != null) dropButton.gameObject.SetActive(gm != null && selected >= 0 && selected < gm.State.Inventory.Count);
            LevelConfig config = Config;
            bool canTie = gm != null && config != null && config.BedsheetItem != null
                && Crafting.CanTieRope(gm.State, config.BedsheetItem.Id, config.RequiredBedsheetsForRope);
            if (ropeButton != null) ropeButton.gameObject.SetActive(canTie);
            if (countLabel != null && gm != null) countLabel.text = gm.State.Inventory.Count + "/" + gm.State.Inventory.Capacity;

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

                if (backgroundColors != null && i < slotBackgrounds.Length && slotBackgrounds[i] != null)
                {
                    slotBackgrounds[i].color = i == selected ? selectedColor : backgroundColors[i];
                }
            }
        }

        private void CacheColors()
        {
            backgroundColors = new Color[slotBackgrounds.Length];
            for (int i = 0; i < slotBackgrounds.Length; i++)
            {
                if (slotBackgrounds[i] != null) backgroundColors[i] = slotBackgrounds[i].color;
            }
        }

        private void OnMessage(string key)
        {
            if (key != UiKeys.BagFull) return;
            StopAllCoroutines();
            StartCoroutine(FlashFull());
        }

        private IEnumerator FlashFull()
        {
            for (int blink = 0; blink < 3; blink++)
            {
                SetBackgrounds(fullFlashColor, useOriginal: false);
                yield return new WaitForSecondsRealtime(0.12f);
                SetBackgrounds(fullFlashColor, useOriginal: true);
                yield return new WaitForSecondsRealtime(0.12f);
            }

            Refresh();
        }

        private void SetBackgrounds(Color color, bool useOriginal)
        {
            for (int i = 0; i < slotBackgrounds.Length; i++)
            {
                if (slotBackgrounds[i] != null) slotBackgrounds[i].color = useOriginal ? backgroundColors[i] : color;
            }
        }
    }
}
