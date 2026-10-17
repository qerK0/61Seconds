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
    /// Hover goes to IHoverable; clicks (also on empty floor) go through the optional IWorldClickRouter
    /// (click-and-run) after the panic input delay.
    /// </summary>
    public sealed class IsoPicker : MonoBehaviour
    {
        [Tooltip("Camera used for screen -> world. Defaults to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Layers to test.")]
        [SerializeField] private LayerMask layers = ~0;

        [Tooltip("Component implementing IWorldClickRouter (click-and-run). Optional: without it targets are clicked directly.")]
        [SerializeField] private MonoBehaviour router;

        /// <summary>Raised after a clickable was clicked.</summary>
        public event Action<IClickable> Clicked;

        private readonly List<Collider2D> hits = new List<Collider2D>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private ContactFilter2D filter;
        private IHoverable hovered;

        private IWorldClickRouter Router => router as IWorldClickRouter;

        private void Awake()
        {
            filter = ContactFilter2D.noFilter;
            filter.SetLayerMask(layers);
            filter.useTriggers = true;
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 screen = pointer.position.ReadValue();
            bool blocked = SixtyOne.Core.InputGate.IsBlocked || IsOverUI(screen);
            IClickable target = blocked ? null : Pick(screen);
            SetHover(target as IHoverable);

            if (blocked || !pointer.press.wasPressedThisFrame) return;

            Vector3 world = ScreenToWorld(screen);
            IWorldClickRouter route = Router;
            if (target == null && (route == null || !route.AcceptsFloorClicks)) return;

            // Panic input delay: instant feedback now, the action after the delay (if still valid).
            SixtyOne.Core.InputDelayRouter.Run(() =>
            {
                if (SixtyOne.Core.InputGate.IsBlocked) return;
                if (route != null && route.Route(target, world)) return;
                if (target == null || !target.IsClickable) return;
                target.OnClicked();
                Clicked?.Invoke(target);
            }, screen);
        }

        private void OnDisable()
        {
            SetHover(null);
        }

        private void SetHover(IHoverable next)
        {
            if (ReferenceEquals(next, hovered)) return;
            if (hovered is UnityEngine.Object o && o != null) hovered.SetHovered(false);
            hovered = next;
            hovered?.SetHovered(true);
        }

        /// <summary>World point under a screen position (z = 0 plane).</summary>
        public Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return Vector3.zero;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            world.z = 0f;
            return world;
        }

        /// <summary>Top-most clickable under a screen position, or null.</summary>
        public IClickable Pick(Vector2 screenPosition)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return null;

            Vector3 world = ScreenToWorld(screenPosition);
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
