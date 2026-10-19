using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Tutorial
{
    /// <summary>
    /// The tutorial's pointer: a pulsing outline frame around a target and a bobbing arrow above it. The target is
    /// one or more UI rects (their union) or a world object (its collider / renderer bounds projected to the screen);
    /// it follows the target every frame and hides while the target is inactive. Unscaled time.
    /// </summary>
    public sealed class TutorialHighlight : MonoBehaviour
    {
        [Tooltip("The overlay canvas' root rect (screen -> canvas conversion).")]
        [SerializeField] private RectTransform canvasRect;

        [Tooltip("Outline frame (centre-anchored; its edge bars stretch with it).")]
        [SerializeField] private RectTransform frame;

        [Tooltip("Frame group (pulsing alpha).")]
        [SerializeField] private CanvasGroup frameGroup;

        [Tooltip("Arrow above the target (centre-anchored, points down).")]
        [SerializeField] private RectTransform arrow;

        [Tooltip("Arrow group (hidden with the frame).")]
        [SerializeField] private CanvasGroup arrowGroup;

        [Tooltip("Camera for world targets. Defaults to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Canvas units between the target and the frame.")]
        [SerializeField] private float padding = 10f;

        [Tooltip("Smallest frame size (canvas units) for a point-like world target.")]
        [SerializeField] private Vector2 minSize = new Vector2(90f, 90f);

        [Tooltip("Pulses per second.")]
        [SerializeField] private float pulseSpeed = 1.4f;

        [Tooltip("Arrow bob height (canvas units).")]
        [SerializeField] private float bob = 10f;

        [Tooltip("A target whose right edge comes this close (canvas units) to the screen's right edge gets the arrow on its left.")]
        [SerializeField] private float sideMargin = 330f;

        private readonly List<RectTransform> uiTargets = new List<RectTransform>();
        private readonly Vector3[] corners = new Vector3[4];
        private Transform worldTarget;

        /// <summary>Points at the union of these UI rects.</summary>
        public void PointAt(params RectTransform[] targets)
        {
            Clear();
            if (targets == null) return;
            foreach (RectTransform t in targets)
            {
                if (t != null) uiTargets.Add(t);
            }
        }

        /// <summary>Points at a world object (its 2D collider or renderer bounds).</summary>
        public void PointAtWorld(Transform target)
        {
            Clear();
            worldTarget = target;
        }

        public void Clear()
        {
            uiTargets.Clear();
            worldTarget = null;
            SetVisible(false);
        }

        private void Awake()
        {
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (!TryGetScreenRect(out Rect screen) || canvasRect == null)
            {
                SetVisible(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen.min, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen.max, null, out Vector2 max);
            Vector2 size = Vector2.Max(max - min + Vector2.one * padding * 2f, worldTarget != null ? minSize : Vector2.zero);
            Vector2 centre = (min + max) * 0.5f;

            float k = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            SetVisible(true);
            if (frame != null)
            {
                frame.anchoredPosition = centre;
                frame.sizeDelta = size * Mathf.Lerp(1f, 1.04f, k);
            }

            if (frameGroup != null) frameGroup.alpha = Mathf.Lerp(0.55f, 1f, k);
            if (arrow != null)
            {
                Rect canvas = canvasRect.rect;
                if (centre.x + size.x * 0.5f > canvas.xMax - sideMargin)
                {
                    // Targets in the right HUD column (bag, pockets, Defter): point from the left, never over the bag.
                    float halfX = size.x * 0.5f + arrow.sizeDelta.y * 0.5f + 6f;
                    arrow.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    arrow.anchoredPosition = centre - new Vector2(halfX + bob * k, 0f);
                }
                else
                {
                    // Above the target; below it when the target sits at the top of the screen.
                    float half = size.y * 0.5f + arrow.sizeDelta.y * 0.5f + 6f;
                    bool below = centre.y + half + arrow.sizeDelta.y > canvas.yMax;
                    arrow.localRotation = Quaternion.Euler(0f, 0f, below ? 180f : 0f);
                    arrow.anchoredPosition = centre + new Vector2(0f, (below ? -1f : 1f) * (half + bob * k));
                }
            }
        }

        private bool TryGetScreenRect(out Rect rect)
        {
            rect = default;
            if (uiTargets.Count > 0)
            {
                bool any = false;
                Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                foreach (RectTransform t in uiTargets)
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    // Overlay canvas: world corners are screen pixels.
                    t.GetWorldCorners(corners);
                    min = Vector2.Min(min, Vector2.Min(corners[0], corners[2]));
                    max = Vector2.Max(max, Vector2.Max(corners[0], corners[2]));
                    any = true;
                }

                if (!any) return false;
                rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                return true;
            }

            if (worldTarget == null || !worldTarget.gameObject.activeInHierarchy) return false;
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null) return false;
            Bounds bounds = WorldBounds(worldTarget);
            Vector3 a = cam.WorldToScreenPoint(bounds.min);
            Vector3 b = cam.WorldToScreenPoint(bounds.max);
            rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        private static Bounds WorldBounds(Transform target)
        {
            Collider2D collider = target.GetComponentInChildren<Collider2D>();
            if (collider != null && collider.enabled) return collider.bounds;
            Renderer renderer = target.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds : new Bounds(target.position, Vector3.zero);
        }

        private void SetVisible(bool visible)
        {
            if (frameGroup != null && !visible) frameGroup.alpha = 0f;
            if (arrowGroup != null) arrowGroup.alpha = visible ? 1f : 0f;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(RectTransform canvas, RectTransform outline, CanvasGroup outlineGroup, RectTransform pointer, CanvasGroup pointerGroup, Camera camera)
        {
            canvasRect = canvas;
            frame = outline;
            frameGroup = outlineGroup;
            arrow = pointer;
            arrowGroup = pointerGroup;
            worldCamera = camera;
        }
    }
}
