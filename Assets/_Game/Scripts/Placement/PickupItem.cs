using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A world item. Click adds it to the bag if a slot is free (else "bag full" message).
    /// Trap items (valuables) kill instead. Visibility follows the Picked_ flag so rewinds restore it.
    /// </summary>
    public sealed class PickupItem : MonoBehaviour, IClickable
    {
        [Tooltip("Item data.")]
        [SerializeField] private Item item;

        [Tooltip("Only clickable during these node ids. Empty = always.")]
        [SerializeField] private List<string> activeNodeIds = new List<string>();

        private Renderer[] renderers;
        private Collider2D[] colliders;

        public Item Item => item;

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                if (!isActiveAndEnabled || item == null || gm == null || gm.IsDead) return false;
                if (gm.State.Flags.Get(FlagNames.Picked(item.Id))) return false;
                return activeNodeIds.Count == 0 || activeNodeIds.Contains(gm.State.CurrentNodeId);
            }
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || item == null) return;

            if (item.IsTrap)
            {
                gm.Die(item.TrapDeath.ToInfo());
                return;
            }

            if (StateOps.TryAddItem(gm.State, item.Id))
            {
                StateOps.SetFlag(gm.State, FlagNames.Picked(item.Id), true);
                Refresh();
            }
        }

        /// <summary>Shows the item unless it was picked in the current timeline.</summary>
        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            bool visible = item == null || gm == null || !gm.State.Flags.Get(FlagNames.Picked(item.Id));
            if (renderers == null) Awake();
            foreach (Renderer r in renderers) if (r != null) r.enabled = visible;
            foreach (Collider2D c in colliders) if (c != null) c.enabled = visible;
        }

        private void OnRewound(string nodeId)
        {
            Refresh();
        }
    }
}
