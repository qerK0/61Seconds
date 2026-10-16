using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SixtyOne.Iso
{
    /// <summary>
    /// Pointer (mouse + touch) -> world clickables via Physics2D.OverlapPoint.
    /// Overlaps resolve to the highest sortingOrder. Ignored when the pointer is over UI.
    /// </summary>
    public sealed class IsoPicker : MonoBehaviour
    {
        [Tooltip("Camera used for screen -> world. Defaults to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Layers to test.")]
        [SerializeField] private LayerMask layers = ~0;

        /// <summary>Raised after a clickable was clicked.</summary>
        public event Action<IClickable> Clicked;

        private readonly List<Collider2D> hits = new List<Collider2D>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private ContactFilter2D filter;

        private void Awake()
        {
            filter = ContactFilter2D.noFilter;
            filter.SetLayerMask(layers);
            filter.useTriggers = true;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || SixtyOne.Core.InputGate.IsBlocked)
            {
                return;
            }

            Vector2 screen = pointer.position.ReadValue();
            if (IsOverUI(screen))
            {
                return;
            }

            IClickable target = Pick(screen);
            if (target == null)
            {
                return;
            }

            target.OnClicked();
            Clicked?.Invoke(target);
        }

        /// <summary>Top-most clickable under a screen position, or null.</summary>
        public IClickable Pick(Vector2 screenPosition)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return null;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            hits.Clear();
            Physics2D.OverlapPoint(world, filter, hits);

            IClickable best = null;
            int bestOrder = int.MinValue;
            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;
                IClickable clickable = hit.GetComponentInParent<IClickable>();
                if (clickable == null || !clickable.IsClickable) continue;

                int order = IsoUtil.GetSortingOrder(hit);
                if (order > bestOrder)
                {
                    bestOrder = order;
                    best = clickable;
                }
            }

            return best;
        }

        private bool IsOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            var data = new PointerEventData(eventSystem) { position = screenPosition };
            uiHits.Clear();
            eventSystem.RaycastAll(data, uiHits);
            return uiHits.Count > 0;
        }
    }
}
