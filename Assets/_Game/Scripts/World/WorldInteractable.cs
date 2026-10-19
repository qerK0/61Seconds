using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// Something the player can use while exploring: pick up an item, open a drawer, borrow supplies.
    /// Using it subtracts its time cost from the building timer. One-shot uses are stored as a Used_ flag so
    /// rewinds restore them. Borrowed items are recorded in the BorrowLedger ("ödünç al").
    /// </summary>
    public class WorldInteractable : MonoBehaviour, IClickable, IApproachable
    {
        [Tooltip("Unique id (used for the Used_ flag).")]
        [SerializeField] private string id;

        [Tooltip("Label shown above it, e.g. 'Çekmeceyi aç'.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("Building-timer seconds (surface 1, drawer/cabinet 3).")]
        [Min(0f)]
        [SerializeField] private float timeCost = 1f;

        [Tooltip("Stamina spent (heavy things).")]
        [Min(0f)]
        [SerializeField] private float staminaCost;

        [Tooltip("Item needed to do this. Optional.")]
        [SerializeField] private Item requiredItem;

        [Tooltip("Message when the required item is missing. Empty = generic.")]
        [SerializeField] private TextKey missingRequirementKey;

        [Tooltip("Item added to the bag. Optional.")]
        [SerializeField] private Item grantsItem;

        [Tooltip("Taken from a neighbour's flat: recorded as borrowed (epilogue).")]
        [SerializeField] private bool borrowed;

        [Tooltip("Extra effects (e.g. SetFlag).")]
        [SerializeField] private List<Outcome> outcomes = new List<Outcome>();

        [Tooltip("Can only be used once per timeline.")]
        [SerializeField] private bool oneShot = true;

        [Tooltip("Hide after use (pickups). Otherwise it is dimmed.")]
        [SerializeField] private bool hideWhenUsed;

        [Tooltip("Only usable during the pre-quake prep (our own flat): once the quake hits, what was not packed is gone.")]
        [SerializeField] private bool prepOnly;

        [Tooltip("Main sprite (dimmed when used, pulses when highlighted). Optional.")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Tint when used.")]
        [SerializeField] private Color usedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private Renderer[] renderers;
        private Collider2D[] colliders;
        private TMPro.TMP_Text[] labels;
        private Color baseColor = Color.white;
        private bool highlighted;

        /// <summary>The player runs here before using it (the mover clamps it onto the floor).</summary>
        public Vector3 ApproachPoint => transform.position;
        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey LabelKey => labelKey;
        public float TimeCost => timeCost;
        public Item RequiredItem => requiredItem;
        public Item GrantsItem => grantsItem;
        public bool Borrowed => borrowed;
        public IReadOnlyList<Outcome> Outcomes => outcomes;
        public bool OneShot => oneShot;
        public bool PrepOnly => prepOnly;

        /// <summary>A prep-only interactable after the quake: no longer usable.</summary>
        public bool IsGone
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return prepOnly && gm != null && gm.State.BuildingTimerStarted;
            }
        }

        /// <summary>Flag set when a one-shot interactable has been used.</summary>
        public string UsedFlag => "Used_" + Id;

        public bool IsUsed
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return oneShot && gm != null && gm.State.Flags.Get(UsedFlag);
            }
        }

        public virtual bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked && !IsUsed && !IsGone;
            }
        }

        protected virtual void Awake()
        {
            CacheParts();
        }

        protected virtual void OnEnable()
        {
            GameEvents.Rewound += OnStateEvent;
            GameEvents.NodeEntered += OnStateEvent;
            Refresh();
        }

        protected virtual void OnDisable()
        {
            GameEvents.Rewound -= OnStateEvent;
            GameEvents.NodeEntered -= OnStateEvent;
        }

        /// <summary>Sensible defaults when the component is added in the Editor.</summary>
        protected virtual void Reset()
        {
        }

        /// <summary>Preset helper for subclasses (e.g. PickupItem).</summary>
        protected void ApplyPreset(float cost, bool isOneShot, bool hide)
        {
            timeCost = cost;
            oneShot = isOneShot;
            hideWhenUsed = hide;
        }

        /// <summary>Authoring helper for generators / scene builders.</summary>
        public void Configure(string interactableId, TextKey label, float cost, Item grants, Item required, TextKey missingRequirement,
            bool isOneShot, bool hide, IEnumerable<Outcome> extraOutcomes, SpriteRenderer mainSprite, bool isBorrowed = false, float stamina = 0f,
            bool isPrepOnly = false)
        {
            id = interactableId;
            labelKey = label;
            timeCost = cost;
            grantsItem = grants;
            requiredItem = required;
            missingRequirementKey = missingRequirement;
            oneShot = isOneShot;
            hideWhenUsed = hide;
            outcomes = extraOutcomes != null ? new List<Outcome>(extraOutcomes) : new List<Outcome>();
            body = mainSprite;
            borrowed = isBorrowed;
            staminaCost = stamina;
            prepOnly = isPrepOnly;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable) return;
            GameState state = gm.State;

            if (requiredItem != null && !StateOps.HasItem(state, requiredItem.Id))
            {
                GameEvents.RaiseMessage(missingRequirementKey.IsEmpty ? UiKeys.NeedsItem : missingRequirementKey.Key);
                return;
            }

            if (grantsItem != null && !grantsItem.IsKeyItem && state.Inventory.IsFull)
            {
                // Refuse before wasting time.
                GameEvents.RaiseMessage(UiKeys.BagFull);
                return;
            }

            ActionCostRouter.SpendStamina(staminaCost);
            ActionCostRouter.SpendTime(timeCost);
            if (gm.IsDead) return;
            Apply(state);
        }

        /// <summary>Applies the effects. Returns false if the item could not be added (bag full).</summary>
        public bool Apply(GameState state)
        {
            if (grantsItem != null)
            {
                if (!StateOps.TryAddItem(state, grantsItem.Id, grantsItem.IsKeyItem)) return false;
                if (borrowed)
                {
                    state.Borrowed.Add(grantsItem.Id);
                    GameEvents.RaiseMessage(UiKeys.Borrowed);
                }
            }

            OutcomeExecutor.Execute(outcomes, state);
            if (oneShot) StateOps.SetFlag(state, UsedFlag, true);
            Refresh();
            return true;
        }

        /// <summary>Updates used/hidden/highlight visuals from the current state.</summary>
        public virtual void Refresh()
        {
            if (renderers == null) CacheParts();
            bool used = IsUsed;
            bool visible = !(used && hideWhenUsed);
            foreach (Renderer r in renderers) if (r != null) r.enabled = visible;
            foreach (Collider2D c in colliders) if (c != null) c.enabled = visible;
            // A used prop may stay visible (opened drawer), but its "what you get here" label must go.
            bool spent = used || IsGone;
            foreach (TMPro.TMP_Text label in labels) if (label != null) label.enabled = visible && !spent;

            highlighted = !spent && IsKnownItem();
            if (body != null) body.color = spent ? baseColor * usedTint : baseColor;
        }

        /// <summary>True if the knowledge highlight applies (setting on, item in the KnowledgeSet).</summary>
        protected bool IsKnownItem()
        {
            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            bool highlightOn = settings == null || settings.KnowledgeHighlight;
            return highlightOn && grantsItem != null && gm != null && gm.State.Knowledge.Contains(grantsItem.Id);
        }

        protected virtual void Update()
        {
            if (!highlighted || body == null) return;
            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            Color glow = settings != null ? settings.HighlightColor : new Color(1f, 0.95f, 0.55f, 1f);
            float speed = settings != null ? settings.HighlightPulseSpeed : 1.5f;
            float k = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            body.color = Color.Lerp(baseColor, glow, k * 0.6f);
        }

        private void CacheParts()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
            labels = GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (body != null) baseColor = body.color;
        }

        private void OnStateEvent(string _) => Refresh();
    }
}
