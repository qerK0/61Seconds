using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// A window/balcony where the bedsheet rope can be tied to climb down to the room below. Needs the rope in
    /// the bag (missing = message, nothing happens) and a way to bring the child down safely (the child harness:
    /// without it the point is shown locked, greyed, and a click shows the reason); heavy (locked at 0 stamina);
    /// costs time and stamina; the rope stays tied (removed from the bag); then the target node is entered
    /// (a "descend" room transition plays).
    /// </summary>
    public sealed class RopeDescentInteractable : MonoBehaviour, IClickable
    {
        [Tooltip("Unique id.")]
        [SerializeField] private string id = "rope_descent";

        [Tooltip("Label shown above it.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("The bedsheet rope.")]
        [SerializeField] private Item requiredItem;

        [Tooltip("Message when the rope is missing.")]
        [SerializeField] private TextKey missingKey;

        [Tooltip("Item that lets the child come down safely (the child harness). Empty = no family rule.")]
        [SerializeField] private Item familyItem;

        [Tooltip("Reason shown while the family item is missing (ShowLocked).")]
        [SerializeField] private TextKey lockedKey;

        [Tooltip("Sprite tinted grey while locked.")]
        [SerializeField] private SpriteRenderer lockTint;

        [Tooltip("Node entered after climbing down.")]
        [SerializeField] private SceneNode targetNode;

        [Tooltip("Node player that enters the target node.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Building-timer seconds for the climb.")]
        [Min(0f)]
        [SerializeField] private float timeCost = 10f;

        [Tooltip("Stamina spent on the climb.")]
        [Min(0f)]
        [SerializeField] private float staminaCost = 20f;

        [Tooltip("The rope stays tied to the window (removed from the bag).")]
        [SerializeField] private bool consumesRope = true;

        public string Id => id;
        public TextKey LabelKey => labelKey;
        public Item RequiredItem => requiredItem;
        public SceneNode TargetNode => targetNode;
        public Item FamilyItem => familyItem;
        public TextKey LockedKey => lockedKey;

        private Color unlockedColor = Color.white;

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked;
            }
        }

        /// <summary>Authoring helper for scene builders.</summary>
        public void Configure(string interactableId, TextKey label, Item rope, TextKey missing, SceneNode target, NodePlayer player, float seconds, float stamina)
        {
            id = interactableId;
            labelKey = label;
            requiredItem = rope;
            missingKey = missing;
            targetNode = target;
            nodePlayer = player;
            timeCost = seconds;
            staminaCost = stamina;
        }

        /// <summary>Authoring helper: the family-safety rule (child harness), its locked reason and the tinted sprite.</summary>
        public void ConfigureFamilyRule(Item item, TextKey reason, SpriteRenderer tint)
        {
            familyItem = item;
            lockedKey = reason;
            lockTint = tint;
        }

        /// <summary>Pure rule: can the whole family come down (the child is carried safely)?</summary>
        public static bool FamilyCanCome(GameState state, Item family)
        {
            return family == null || StateOps.HasItem(state, family.Id);
        }

        /// <summary>Pure rule: rope, stamina and the family rule all met.</summary>
        public static bool CanDescend(GameState state, Item rope, Item family)
        {
            return CanDescend(state, rope) && FamilyCanCome(state, family);
        }

        private void Awake()
        {
            if (lockTint != null) unlockedColor = lockTint.color;
        }

        private void OnEnable()
        {
            InventoryEvents.OnInventoryChanged += RefreshLock;
            RefreshLock();
        }

        private void OnDisable()
        {
            InventoryEvents.OnInventoryChanged -= RefreshLock;
        }

        private void RefreshLock()
        {
            if (lockTint == null) return;
            GameManager gm = GameManager.Instance;
            bool locked = gm != null && !FamilyCanCome(gm.State, familyItem);
            lockTint.color = locked ? new Color(0.45f, 0.45f, 0.5f, unlockedColor.a) : unlockedColor;
        }

        /// <summary>Pure rule: can the climb start? (rope owned and stamina left).</summary>
        public static bool CanDescend(GameState state, Item rope)
        {
            return rope != null && StateOps.HasItem(state, rope.Id) && state.Stamina > ChoiceView.ExhaustedStamina;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable || targetNode == null) return;
            GameState state = gm.State;

            if (requiredItem == null || !StateOps.HasItem(state, requiredItem.Id))
            {
                GameEvents.RaiseMessage(missingKey.IsEmpty ? UiKeys.RopeDescentMissing : missingKey.Key);
                return;
            }

            if (!FamilyCanCome(state, familyItem))
            {
                GameEvents.RaiseMessage(lockedKey.IsEmpty ? UiKeys.Locked : lockedKey.Key);
                return;
            }

            if (!CanDescend(state, requiredItem))
            {
                GameEvents.RaiseMessage(UiKeys.StaminaExhausted);
                return;
            }

            ActionCostRouter.SpendStamina(staminaCost);
            ActionCostRouter.SpendTime(timeCost, true);
            if (gm.IsDead) return;
            if (consumesRope) StateOps.RemoveItem(state, requiredItem.Id);
            if (nodePlayer != null) nodePlayer.Enter(targetNode);
        }
    }
}
