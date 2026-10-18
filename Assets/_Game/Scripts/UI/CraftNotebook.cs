using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// "Craft Defteri": a spiral notebook page listing every recipe (one handwritten line each; undiscovered results
    /// read "???" under a pencil scribble) with two pasted-in slots and a "Birleştir" button. While it is open a bag
    /// click fills a slot instead of selecting; a valid pair crafts with the same costs and rules as the bag combine
    /// (two bedsheets in the slots tie the rope). A recipe is discovered once crafted or once all its inputs were held
    /// at the same time; discoveries live in the KnowledgeSet, so they survive every rewind. Opens with the notebook
    /// button or C (the page slides up with a paper rustle); Esc closes it before the pause menu. Timers keep running.
    /// </summary>
    public sealed class CraftNotebook : MonoBehaviour
    {
        /// <summary>The open notebook (null when closed): BagUI routes slot clicks here, PauseMenu lets Esc close it.</summary>
        public static CraftNotebook Open { get; private set; }

        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameContent content;
        [SerializeField] private SurvivalController survival;
        [SerializeField] private CanvasGroup group;

        [Tooltip("The page that slides up (its rest position is the authored one).")]
        [SerializeField] private RectTransform page;

        [Tooltip("Recipe of each line (null = the bedsheet rope line).")]
        [SerializeField] private Recipe[] recipes = new Recipe[0];

        [SerializeField] private TMP_Text[] lineTexts = new TMP_Text[0];
        [SerializeField] private Image[] resultIcons = new Image[0];

        [Tooltip("Pencil scribble over each undiscovered result.")]
        [SerializeField] private GameObject[] scribbles = new GameObject[0];

        [Header("Slots")]
        [SerializeField] private Image[] slotIcons = new Image[2];
        [SerializeField] private TMP_Text[] slotLabels = new TMP_Text[2];
        [SerializeField] private Button[] slotButtons = new Button[2];
        [SerializeField] private Button combineButton;
        [SerializeField] private Button openButton;

        [SerializeField] private AudioCue rustleCue;
        [SerializeField] private float slideDistance = 120f;
        [SerializeField] private float slideSeconds = 0.25f;

        private readonly Item[] slots = new Item[2];
        private Vector2 rest;
        private bool open;

        /// <summary>Knowledge key of a discovered recipe (by its output item id).</summary>
        public static string KnowledgeKey(string outputId) => "recipe:" + outputId;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;
        private LevelConfig Config => survival != null ? survival.Config : null;

        private void Awake()
        {
            if (page != null) rest = page.anchoredPosition;
            UIVisibility.Set(group, false);
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                if (slotButtons[i] != null) slotButtons[i].onClick.AddListener(() => ClearSlot(index));
            }

            if (combineButton != null) combineButton.onClick.AddListener(Combine);
            if (openButton != null) openButton.onClick.AddListener(Toggle);
        }

        private void OnEnable()
        {
            InventoryEvents.OnInventoryChanged += OnInventoryChanged;
            GameEvents.ItemAdded += OnItemAdded;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            InventoryEvents.OnInventoryChanged -= OnInventoryChanged;
            GameEvents.ItemAdded -= OnItemAdded;
            GameEvents.Died -= OnDied;
            if (Open == this) Open = null;
        }

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || PauseState.IsPaused) return;
            if (k.cKey.wasPressedThisFrame && !ScreenHold.Active) Toggle();
        }

        /// <summary>Opens or closes the notebook.</summary>
        public void Toggle()
        {
            if (open) Close();
            else if (Gm != null && !Gm.IsDead) Show();
        }

        private void Show()
        {
            open = true;
            Open = this;
            Discover();
            Refresh();
            AudioService.PlayUiCue(rustleCue);
            StopAllCoroutines();
            StartCoroutine(Slide(true));
        }

        /// <summary>Closes the notebook (Esc, the button, C, a death).</summary>
        public void Close()
        {
            if (!open) return;
            open = false;
            if (Open == this) Open = null;
            slots[0] = slots[1] = null;
            StopAllCoroutines();
            StartCoroutine(Slide(false));
        }

        private IEnumerator Slide(bool show)
        {
            if (show) UIVisibility.Set(group, true);
            Vector2 low = rest - new Vector2(0f, slideDistance);
            Vector2 from = show ? low : rest, to = show ? rest : low;
            for (float t = 0f; t < slideSeconds; t += Time.unscaledDeltaTime)
            {
                float e = Easing.Evaluate(Ease.OutQuad, t / slideSeconds);
                if (page != null) page.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
                if (group != null) group.alpha = show ? e : 1f - e;
                yield return null;
            }

            if (page != null) page.anchoredPosition = to;
            if (show) group.alpha = 1f;
            else UIVisibility.Set(group, false);
        }

        /// <summary>A bag click while open: the item goes into the first free slot (else replaces the second).</summary>
        public void Take(Item item)
        {
            if (item == null) return;
            int index = slots[0] == null ? 0 : 1;
            slots[index] = item;
            Refresh();
        }

        private void ClearSlot(int index)
        {
            if (index < 0 || index >= slots.Length) return;
            slots[index] = null;
            Refresh();
        }

        private void Combine()
        {
            GameManager gm = Gm;
            if (gm == null || gm.IsDead || InputGate.IsBlocked) return;
            if (slots[0] == null || slots[1] == null)
            {
                GameEvents.RaiseMessage(UiKeys.CraftNeedsTwo);
                return;
            }

            LevelConfig config = Config;
            if (config != null && config.BedsheetItem != null && slots[0].Id == config.BedsheetItem.Id && slots[1].Id == config.BedsheetItem.Id)
            {
                // The bedsheet rope: same rule and costs as the bag's tie-rope.
                if (!Crafting.TieRope(gm.State, config.BedsheetItem.Id, config.BedsheetRopeItem.Id, config.RequiredBedsheetsForRope))
                {
                    GameEvents.RaiseMessage(UiKeys.RopeNeedsSheets);
                    return;
                }

                ActionCostRouter.SpendStamina(config.RopeCraftStamina);
                ActionCostRouter.SpendTime(config.RopeCraftSeconds);
                gm.State.Knowledge.Add(KnowledgeKey(config.BedsheetRopeItem.Id));
                GameEvents.RaiseMessage(UiKeys.RopeTied);
            }
            else
            {
                Recipe recipe = Crafting.Find(Book, slots[0], slots[1]);
                if (recipe == null || !Crafting.Craft(gm.State, recipe))
                {
                    GameEvents.RaiseMessage(UiKeys.CraftFail);
                    return;
                }

                ActionCostRouter.SpendStamina(recipe.StaminaCost);
                ActionCostRouter.SpendTime(recipe.TimeCost);
                gm.State.Knowledge.Add(KnowledgeKey(recipe.Output.Id));
                GameEvents.RaiseMessage(UiKeys.CraftDone);
            }

            slots[0] = slots[1] = null;
            InventoryEvents.RaiseInventoryChanged();
            Refresh();
        }

        private RecipeBook book;

        /// <summary>The recipes of the lines as a book (for Crafting.Find).</summary>
        private RecipeBook Book
        {
            get
            {
                if (book != null) return book;
                book = ScriptableObject.CreateInstance<RecipeBook>();
                var list = new System.Collections.Generic.List<Recipe>();
                foreach (Recipe r in recipes) if (r != null) list.Add(r);
                book.Configure(list);
                return book;
            }
        }

        private void OnInventoryChanged()
        {
            Discover();
            if (open) Refresh();
        }

        private void OnItemAdded(string itemId) => OnInventoryChanged();

        private void OnDied(DeathInfo info) => Close();

        /// <summary>Marks every recipe whose inputs are all held right now as discovered.</summary>
        private void Discover()
        {
            GameManager gm = Gm;
            if (gm == null) return;
            GameState state = gm.State;
            LevelConfig config = Config;
            foreach (Recipe r in recipes)
            {
                if (r == null)
                {
                    if (config != null && config.BedsheetItem != null && config.BedsheetRopeItem != null
                        && state.Inventory.CountOf(config.BedsheetItem.Id) >= config.RequiredBedsheetsForRope)
                    {
                        state.Knowledge.Add(KnowledgeKey(config.BedsheetRopeItem.Id));
                    }

                    continue;
                }

                if (r.InputA == null || r.InputB == null || r.Output == null) continue;
                bool both = r.InputA == r.InputB
                    ? state.Inventory.CountOf(r.InputA.Id) >= 2
                    : StateOps.HasItem(state, r.InputA.Id) && StateOps.HasItem(state, r.InputB.Id);
                if (both) state.Knowledge.Add(KnowledgeKey(r.Output.Id));
            }
        }

        private void Refresh()
        {
            GameManager gm = Gm;
            LevelConfig config = Config;
            for (int i = 0; i < recipes.Length; i++)
            {
                Recipe r = recipes[i];
                Item a = r != null ? r.InputA : config != null ? config.BedsheetItem : null;
                Item b = r != null ? r.InputB : null;
                Item output = r != null ? r.Output : config != null ? config.BedsheetRopeItem : null;
                bool known = gm != null && output != null && gm.State.Knowledge.Contains(KnowledgeKey(output.Id));
                string left = r != null
                    ? Name(a) + " + " + Name(b)
                    : TextDB.Format(UiKeys.CraftTimes, Name(a), config != null ? config.RequiredBedsheetsForRope : 3);
                string right = known ? Name(output) : TextDB.GetDisplay(UiKeys.CraftUnknown);
                if (i < lineTexts.Length && lineTexts[i] != null) lineTexts[i].text = left + " = " + right;
                if (i < resultIcons.Length && resultIcons[i] != null) resultIcons[i].enabled = known && output != null && output.Icon != null;
                if (i < scribbles.Length && scribbles[i] != null) scribbles[i].SetActive(!known);
            }

            for (int i = 0; i < slots.Length; i++)
            {
                Item item = slots[i];
                if (i < slotIcons.Length && slotIcons[i] != null)
                {
                    slotIcons[i].sprite = item != null ? item.Icon : null;
                    slotIcons[i].enabled = item != null && item.Icon != null;
                }

                if (i < slotLabels.Length && slotLabels[i] != null) slotLabels[i].text = item != null ? Name(item) : TextDB.GetDisplay(UiKeys.CraftSlotEmpty);
            }
        }

        private static string Name(Item item) => item != null ? item.NameKey.Display : "?";
    }
}
