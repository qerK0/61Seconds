using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// Shows the items dropped in the current room (GameState.Dropped) as clickable WorldDroppedItems near the
    /// room's player spawn. Rebuilds on inventory changes, room changes and rewinds; never referenced by the UI.
    /// </summary>
    public sealed class DroppedItemsPresenter : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Room switcher (current room).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Item registry for icons.")]
        [SerializeField] private GameContent content;

        [Tooltip("Sprite used when an item has no icon.")]
        [SerializeField] private Sprite fallbackSprite;

        [Tooltip("Tint of dropped items.")]
        [SerializeField] private Color tint = new Color(1f, 0.95f, 0.8f, 1f);

        [Tooltip("World offset between dropped items.")]
        [SerializeField] private Vector2 spacing = new Vector2(0.32f, -0.12f);

        [Tooltip("Offset of the first dropped item from the player spawn.")]
        [SerializeField] private Vector2 firstOffset = new Vector2(0.45f, -0.1f);

        [Tooltip("Building-timer seconds to pick an item up again.")]
        [Min(0f)]
        [SerializeField] private float pickUpSeconds = 1f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        private void OnEnable()
        {
            InventoryEvents.OnInventoryChanged += Rebuild;
            NavigationEvents.OnTransitionComplete += OnRoomShown;
            GameEvents.NodeEntered += OnNode;
            GameEvents.Rewound += OnNode;
            Rebuild();
        }

        private void OnDisable()
        {
            InventoryEvents.OnInventoryChanged -= Rebuild;
            NavigationEvents.OnTransitionComplete -= OnRoomShown;
            GameEvents.NodeEntered -= OnNode;
            GameEvents.Rewound -= OnNode;
        }

        private void OnNode(string _) => Rebuild();
        private void OnRoomShown(string _) => Rebuild();

        /// <summary>Destroys and respawns the dropped items of the current room.</summary>
        public void Rebuild()
        {
            foreach (GameObject go in spawned) if (go != null) Destroy(go);
            spawned.Clear();

            GameManager gm = Gm;
            Room room = rooms != null ? rooms.Current : null;
            if (gm == null || room == null) return;

            IReadOnlyList<string> items = gm.State.Dropped.ItemsIn(room.RoomId);
            // Offsets are room-local (tuned at world scale 1), so they follow RoomVisualConfig.WorldScale.
            Vector3 origin = room.transform.InverseTransformPoint(room.GetSpawn(MemberType.Player)) + (Vector3)firstOffset;
            for (int i = 0; i < items.Count; i++)
            {
                spawned.Add(Spawn(room, items[i], origin + (Vector3)(spacing * i)));
            }
        }

        private GameObject Spawn(Room room, string itemId, Vector3 localPosition)
        {
            var go = new GameObject("Dropped_" + itemId);
            go.transform.SetParent(room.transform, false);
            go.transform.localPosition = localPosition;

            Item item = content != null ? content.FindItem(itemId) : null;
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = item != null && item.Icon != null ? item.Icon : fallbackSprite;
            r.color = tint;
            go.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            go.AddComponent<CircleCollider2D>().radius = 0.45f;

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            go.AddComponent<WorldDroppedItem>().Bind(itemId, room.RoomId, pickUpSeconds);
            return go;
        }
    }
}
