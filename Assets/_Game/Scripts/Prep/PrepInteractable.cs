using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// Something the player can use during prep: pick up an item, open a drawer, close the gas valve,
    /// switch off the breaker. Every use costs countdown time (input blocked while it runs).
    /// One-shot uses are stored as a Used_ flag so rewinds restore them.
    /// </summary>
    public class PrepInteractable : MonoBehaviour, IClickable
    {
        [Tooltip("Unique id (used for the Used_ flag).")]
        [SerializeField] private string id;

        [Tooltip("Action label (progress bar), e.g. 'Çekmeceyi aç'.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("Countdown seconds this action takes (surface 1, drawer 3, breaker 4, gas valve 5).")]
        [Min(0f)]
        [SerializeField] private float timeCost = 1f;

        [Tooltip("Item needed to do this (e.g. wrench for the gas valve). Optional.")]
        [SerializeField] private Item requiredItem;

        [Tooltip("Message when the required item is missing. Empty = generic.")]
        [SerializeField] private TextKey missingRequirementKey;

        [Tooltip("Item added to the bag. Optional.")]
        [SerializeField] private Item grantsItem;

        [Tooltip("Extra effects (e.g. SetFlag GasOff).")]
        [SerializeField] private List<Outcome> outcomes = new List<Outcome>();

        [Tooltip("Can only be used once per timeline.")]
        [SerializeField] private bool oneShot = true;

        [Tooltip("Hide after use (pickups). Otherwise it is dimmed.")]
        [SerializeField] private bool hideWhenUsed;

        [Tooltip("Prep driver that runs the timed action.")]
        [SerializeField] private PrepController controller;

        [Tooltip("Main sprite (dimmed when used, pulses when highlighted).")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Tint when used.")]
        [SerializeField] private Color usedTint = new Color(0.45f, 0.45f, 0.45f, 1f);

        private Renderer[] renderers;
        private Collider2D[] colliders;
        private Color baseColor = Color.white;
        private bool highlighted;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey LabelKey => labelKey;
        public float TimeCost => timeCost;
        public Item RequiredItem => requiredItem;
        public TextKey MissingRequirementKey => missingRequirementKey;
        public Item GrantsItem => grantsItem;
        public IReadOnlyList<Outcome> Outcomes => outcomes;
        public bool OneShot => oneShot;

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

        public bool IsClickable => isActiveAndEnabled && controller != null && controller.CanInteract && !IsUsed;

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
            bool isOneShot, bool hide, IEnumerable<Outcome> extraOutcomes, PrepController driver, SpriteRenderer mainSprite)
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
            controller = driver;
            body = mainSprite;
        }

        public void OnClicked()
        {
            if (controller != null) controller.RequestInteract(this);
        }

        /// <summary>Applies the effects. Returns false if the item could not be added (bag full).</summary>
        public bool Apply(GameState state)
        {
            if (grantsItem != null && !StateOps.TryAddItem(state, grantsItem.Id))
            {
                return false;
            }

            OutcomeExecutor.Execute(outcomes, state);
            if (oneShot) StateOps.SetFlag(state, UsedFlag, true);
            Refresh();
            return true;
        }

        /// <summary>Updates used/hidden/highlight visuals from the current state.</summary>
        public void Refresh()
        {
            if (renderers == null) CacheParts();
            bool used = IsUsed;
            bool visible = !(used && hideWhenUsed);
            foreach (Renderer r in renderers) if (r != null) r.enabled = visible;
            foreach (Collider2D c in colliders) if (c != null) c.enabled = visible;

            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            bool highlightOn = settings == null || settings.KnowledgeHighlight;
            highlighted = highlightOn && !used && grantsItem != null && gm != null && gm.State.Knowledge.Contains(grantsItem.Id);

            if (body != null) body.color = used ? baseColor * usedTint : baseColor;
        }

        private void Update()
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
            if (body != null) baseColor = body.color;
        }

        private void OnStateEvent(string _) => Refresh();
    }
}
