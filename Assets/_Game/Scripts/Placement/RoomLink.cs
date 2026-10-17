using SixtyOne.Iso;
using TMPro;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A clickable world spot that plays a node choice or changes room (Door, ChoiceHotspot). The player runs to
    /// its approach point first; hovering brightens it and shows a small arrow + label (dimmed when unavailable).
    /// </summary>
    public abstract class RoomLink : MonoBehaviour, IClickable, IApproachable, IHoverable
    {
        [Tooltip("Decides and runs what using this link does.")]
        [SerializeField] private RoomNavigator navigator;

        [Tooltip("Where the player stands to use it. Defaults to this transform.")]
        [SerializeField] private Transform approachPoint;

        [Tooltip("Sprite brightened on hover. Optional.")]
        [SerializeField] private SpriteRenderer highlight;

        [Tooltip("Arrow + label shown while hovered. Optional.")]
        [SerializeField] private GameObject hoverRoot;

        [Tooltip("Hover label (tinted grey when the link is unavailable). Optional.")]
        [SerializeField] private TMP_Text hoverLabel;

        [Tooltip("Highlight tint multiplier on hover.")]
        [SerializeField] private Color hoverTint = new Color(1.6f, 1.45f, 1.1f, 1f);

        private Color baseColor = Color.white;
        private Color labelColor = Color.white;

        public RoomNavigator Navigator => navigator;
        public Vector3 ApproachPoint => approachPoint != null ? approachPoint.position : transform.position;
        public bool IsClickable => isActiveAndEnabled && navigator != null && navigator.CanUseLinks;

        protected virtual void Awake()
        {
            if (highlight != null) baseColor = highlight.color;
            if (hoverLabel != null) labelColor = hoverLabel.color;
            if (hoverRoot != null) hoverRoot.SetActive(false);
        }

        protected virtual void OnDisable()
        {
            SetHovered(false);
        }

        public void OnClicked()
        {
            if (IsClickable) navigator.Use(this);
        }

        public void SetHovered(bool hovered)
        {
            if (highlight != null) highlight.color = hovered ? baseColor * hoverTint : baseColor;
            if (hoverRoot != null) hoverRoot.SetActive(hovered);
            if (hovered && hoverLabel != null && navigator != null)
            {
                LinkAction action = navigator.Decide(this).Action;
                bool open = action == LinkAction.Choose || action == LinkAction.LocalSwitch;
                hoverLabel.color = open ? labelColor : new Color(labelColor.r * 0.6f, labelColor.g * 0.6f, labelColor.b * 0.6f, labelColor.a * 0.8f);
            }
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void ConfigureLink(RoomNavigator owner, Transform approach, SpriteRenderer highlightRenderer, GameObject hover, TMP_Text label)
        {
            navigator = owner;
            approachPoint = approach;
            highlight = highlightRenderer;
            hoverRoot = hover;
            hoverLabel = label;
        }
    }
}
