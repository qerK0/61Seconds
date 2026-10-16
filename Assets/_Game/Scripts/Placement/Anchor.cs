using System;
using System.Collections.Generic;
using SixtyOne.Iso;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SixtyOne.Placement
{
    /// <summary>A spot in a room where family members can be placed before a quake.</summary>
    public sealed class Anchor : MonoBehaviour, IClickable
    {
        [Tooltip("Unique id across all rooms (e.g. lr_table).")]
        [SerializeField] private string id;

        [Tooltip("Safe or Trap.")]
        [SerializeField] private AnchorTag anchorTag = AnchorTag.Safe;

        [Tooltip("How many members fit (a table holds 2).")]
        [Min(1)]
        [SerializeField] private int capacity = 1;

        [Tooltip("Hazard that hits members here. Leave empty for Safe anchors.")]
        [SerializeField] private Hazard hazard;

        [Tooltip("Neighbouring anchors (child adjacency rule).")]
        [SerializeField] private List<Anchor> adjacent = new List<Anchor>();

        [Tooltip("Label revealed after someone died here once.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("Feet position characters snap to. Defaults to this transform.")]
        [SerializeField] private Transform groundPoint;

        [Tooltip("Decal tinted when the anchor is a valid target.")]
        [SerializeField] private SpriteRenderer highlightRenderer;

        [Tooltip("Decal color when idle.")]
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.2f);

        [Tooltip("Decal color when highlighted.")]
        [SerializeField] private Color highlightColor = new Color(0.35f, 1f, 0.45f, 0.75f);

        [Tooltip("Optional world label (TextMeshPro) for the revealed name.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Hook for art/FX when highlight toggles.")]
        [SerializeField] private UnityEvent<bool> onHighlightChanged = new UnityEvent<bool>();

        /// <summary>Raised when IsoPicker clicks this anchor.</summary>
        public event Action<Anchor> Clicked;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public AnchorTag Tag => anchorTag;
        public int Capacity => capacity;
        public Hazard Hazard => hazard;
        public IReadOnlyList<Anchor> Adjacent => adjacent;
        public TextKey LabelKey => labelKey;
        public Vector3 GroundPoint => groundPoint != null ? groundPoint.position : transform.position;
        public bool IsHighlighted { get; private set; }

        // Only while something listens (placement), so anchors never block prep interactables underneath.
        public bool IsClickable => isActiveAndEnabled && Clicked != null;

        private void Awake()
        {
            SetHighlight(false);
        }

        public void OnClicked()
        {
            Clicked?.Invoke(this);
        }

        /// <summary>Shows/hides the valid-target highlight.</summary>
        public void SetHighlight(bool on)
        {
            IsHighlighted = on;
            if (highlightRenderer != null)
            {
                highlightRenderer.color = on ? highlightColor : idleColor;
            }

            onHighlightChanged?.Invoke(on);
        }

        /// <summary>Shows the label once revealed.</summary>
        public void RefreshLabel(bool revealed)
        {
            if (label == null) return;
            bool show = revealed && !labelKey.IsEmpty;
            label.gameObject.SetActive(show);
            if (show) label.text = labelKey.Display;
        }

        /// <summary>Ids of adjacent anchors.</summary>
        public List<string> AdjacentIds()
        {
            var ids = new List<string>(adjacent.Count);
            foreach (Anchor a in adjacent)
            {
                if (a != null) ids.Add(a.Id);
            }

            return ids;
        }

        /// <summary>Pure-data snapshot for rules/resolver.</summary>
        public AnchorInfo ToInfo()
        {
            return new AnchorInfo(Id, anchorTag, capacity, hazard, AdjacentIds());
        }
    }
}
