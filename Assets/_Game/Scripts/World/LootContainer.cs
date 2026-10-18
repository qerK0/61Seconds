using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SixtyOne.World
{
    /// <summary>
    /// Furniture you search (wardrobe, cabinet, kitchen cabinet, bookshelf, drawers, toolbox, nightstand). Click:
    /// the player runs to it (PlayerMover), then a radial ring fills over searchSeconds with a rummaging sound
    /// (input locked except Esc; any click cancels and refunds nothing). The first search costs buildingCost once.
    /// Then the LootWindow opens with what is inside (GameState.Containers); a second visit opens it at once, free.
    /// In neighbours' flats every item taken is borrowed ("ödünç al").
    /// </summary>
    public sealed class LootContainer : MonoBehaviour, IClickable, IApproachable
    {
        [Tooltip("Unique id (ContainerLedger key).")]
        [SerializeField] private string id;

        [Tooltip("Container name, e.g. container.wardrobe (window title).")]
        [SerializeField] private TextKey nameKey;

        [Tooltip("Real seconds spent searching the first time.")]
        [Min(0f)]
        [SerializeField] private float searchSeconds = 1.5f;

        [Tooltip("Building-timer seconds, paid once on the first search (prep seconds before the quake).")]
        [Min(0f)]
        [SerializeField] private float buildingCost = 3f;

        [Tooltip("Items inside (ids).")]
        [SerializeField] private List<Item> contents = new List<Item>();

        [Tooltip("Hidden extras: only inside once this container was searched in an earlier loop (KnowledgeSet).")]
        [SerializeField] private List<Item> hiddenContents = new List<Item>();

        [Tooltip("A neighbour's furniture: items taken are recorded as borrowed.")]
        [SerializeField] private bool borrowed;

        [Tooltip("Our own flat: only searchable during the pre-quake prep.")]
        [SerializeField] private bool prepOnly;

        [Tooltip("Where the player stands. Defaults to this transform.")]
        [SerializeField] private Transform approachPoint;

        [Tooltip("Furniture sprite (dimmed once searched and empty, pulses when it holds a known item).")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Rummaging sound (cloth and wood), played while searching.")]
        [SerializeField] private AudioCue searchCue;

        [Tooltip("Item registry (key items, names).")]
        [SerializeField] private GameContent content;

        [Tooltip("Tint once searched and empty.")]
        [SerializeField] private Color emptyTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        private Color baseColor = Color.white;
        private Coroutine searching;
        private TMPro.TMP_Text[] labels = new TMPro.TMP_Text[0];

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey NameKey => nameKey;
        public bool Borrowed => borrowed;
        public bool PrepOnly => prepOnly;
        public float SearchSecondsConfigured => searchSeconds;
        public float BuildingCostConfigured => buildingCost;
        public IReadOnlyList<Item> Contents => contents;
        public Vector3 ApproachPoint => approachPoint != null ? approachPoint.position : transform.position;

        /// <summary>A prep-only container after the quake: what was not packed is gone.</summary>
        public bool IsGone
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return prepOnly && gm != null && gm.State.BuildingTimerStarted;
            }
        }

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked && searching == null && !IsGone;
            }
        }

        private void Awake()
        {
            if (body != null) baseColor = body.color;
            labels = GetComponentsInChildren<TMPro.TMP_Text>(true);
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnStateEvent;
            GameEvents.NodeEntered += OnNodeEntered;
            InventoryEvents.OnInventoryChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnStateEvent;
            GameEvents.NodeEntered -= OnNodeEntered;
            InventoryEvents.OnInventoryChanged -= Refresh;
            CancelSearch();
            if (LootEvents.IsOpen && LootEvents.Open.ContainerId == Id) LootEvents.RaiseClosed();
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(string containerId, TextKey containerName, float seconds, float cost, IEnumerable<Item> items, IEnumerable<Item> hidden,
            bool isBorrowed, bool isPrepOnly, Transform approach, SpriteRenderer sprite, AudioCue cue, GameContent registry)
        {
            id = containerId;
            nameKey = containerName;
            searchSeconds = seconds;
            buildingCost = cost;
            contents = items != null ? new List<Item>(items) : new List<Item>();
            hiddenContents = hidden != null ? new List<Item>(hidden) : new List<Item>();
            borrowed = isBorrowed;
            prepOnly = isPrepOnly;
            approachPoint = approach;
            body = sprite;
            searchCue = cue;
            content = registry;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable) return;
            GameState state = gm.State;
            if (LootLogic.IsSearched(state, Id))
            {
                OpenWindow();
                return;
            }

            float cost = LootLogic.BuildingCost(state, Id, buildingCost);
            if (cost > 0f)
            {
                StateOps.SetFlag(state, LootLogic.PaidFlag(Id), true);
                ActionCostRouter.SpendTime(cost);
                if (gm.IsDead) return;
            }

            searching = StartCoroutine(Search(LootLogic.SearchSeconds(state, Id, searchSeconds)));
        }

        private IEnumerator Search(float seconds)
        {
            InputGate.Block();
            AudioService.PlayCue(searchCue);
            Vector3 ringPoint = transform.position + new Vector3(0f, 0.6f * Mathf.Abs(transform.lossyScale.y), 0f);
            float t = 0f;
            LootEvents.RaiseSearchProgress(ringPoint, 0f);
            // Skip the frame of the click that started the search.
            yield return null;
            while (t < seconds)
            {
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.IsDead || ClickedElsewhere())
                {
                    EndSearch(false);
                    yield break;
                }

                t += Time.deltaTime;
                LootEvents.RaiseSearchProgress(ringPoint, Mathf.Clamp01(t / Mathf.Max(0.01f, seconds)));
                yield return null;
            }

            EndSearch(true);
        }

        private static bool ClickedElsewhere()
        {
            if (PauseState.IsPaused) return false;
            Pointer pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

        private void EndSearch(bool finished)
        {
            searching = null;
            InputGate.Unblock();
            AudioService.StopCue(searchCue);
            LootEvents.RaiseSearchEnded();
            if (!finished) return;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsDead) return;
            LootLogic.CompleteSearch(gm.State, Id, Ids(contents), Ids(hiddenContents));
            Refresh();
            OpenWindow();
        }

        private void CancelSearch()
        {
            if (searching == null) return;
            StopCoroutine(searching);
            searching = null;
            InputGate.Unblock();
            AudioService.StopCue(searchCue);
            LootEvents.RaiseSearchEnded();
        }

        private void OpenWindow()
        {
            LootEvents.RaiseOpened(new LootView { ContainerId = Id, NameKey = nameKey.Key, Borrowed = borrowed });
        }

        /// <summary>Dims an emptied container and pulses one that holds an item the player died for lacking.</summary>
        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            bool searched = LootLogic.IsSearched(gm.State, Id);
            bool empty = searched && gm.State.Containers.ItemsIn(Id).Count == 0;
            bool spent = empty || IsGone;
            if (body != null) body.color = spent ? baseColor * emptyTint : baseColor;
            foreach (TMPro.TMP_Text label in labels) if (label != null) label.enabled = !spent;
        }

        private void Update()
        {
            if (body == null || IsGone || !KnowsSomethingInside()) return;
            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            Color glow = settings != null ? settings.HighlightColor : new Color(1f, 0.95f, 0.55f, 1f);
            float speed = settings != null ? settings.HighlightPulseSpeed : 1.5f;
            float k = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            body.color = Color.Lerp(baseColor, glow, k * 0.6f);
        }

        private bool KnowsSomethingInside()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return false;
            GameSettings settings = gm.Settings;
            if (settings != null && !settings.KnowledgeHighlight) return false;
            GameState state = gm.State;
            IEnumerable<string> inside = LootLogic.IsSearched(state, Id) ? state.Containers.ItemsIn(Id) : Ids(contents);
            foreach (string itemId in inside)
            {
                if (state.Knowledge.Contains(itemId)) return true;
            }

            return false;
        }

        private static List<string> Ids(IEnumerable<Item> items)
        {
            var ids = new List<string>();
            if (items == null) return ids;
            foreach (Item item in items) if (item != null) ids.Add(item.Id);
            return ids;
        }

        private void OnStateEvent(string _) => Refresh();

        // The prep countdown can hit zero mid-search: the quake placement needs the input back.
        private void OnNodeEntered(string _)
        {
            CancelSearch();
            Refresh();
        }
    }
}
