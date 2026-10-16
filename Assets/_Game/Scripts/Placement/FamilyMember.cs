using System;
using SixtyOne.Core;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>A clickable family member in the world. Placement rules live in PlacementRules.</summary>
    public sealed class FamilyMember : MonoBehaviour, IClickable
    {
        [Tooltip("Player, Wife or Child.")]
        [SerializeField] private MemberType type;

        [Tooltip("Main sprite (tinted for injured/trapped).")]
        [SerializeField] private SpriteRenderer body;

        [Tooltip("Shown while selected.")]
        [SerializeField] private GameObject selectionMarker;

        [Tooltip("Tint when injured.")]
        [SerializeField] private Color injuredTint = new Color(1f, 0.55f, 0.55f, 1f);

        [Tooltip("Tint when dead/trapped.")]
        [SerializeField] private Color downTint = new Color(0.35f, 0.35f, 0.35f, 1f);

        /// <summary>Raised when IsoPicker clicks this member.</summary>
        public event Action<FamilyMember> Clicked;

        private Color baseColor = Color.white;

        public MemberType Type => type;
        public bool IsClickable => isActiveAndEnabled;

        private void Awake()
        {
            if (body != null) baseColor = body.color;
            SetSelected(false);
        }

        private void OnEnable()
        {
            GameEvents.MemberStateChanged += OnMemberStateChanged;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.MemberStateChanged -= OnMemberStateChanged;
            GameEvents.Rewound -= OnRewound;
        }

        public void OnClicked()
        {
            Clicked?.Invoke(this);
        }

        /// <summary>Snaps to an anchor's ground point (small offset so shared anchors don't overlap).</summary>
        public void PlaceAt(Anchor anchor)
        {
            PlaceAt(anchor.GroundPoint + SlotOffset(type));
        }

        /// <summary>Snaps to a world point (instant, no walk).</summary>
        public void PlaceAt(Vector3 point)
        {
            transform.position = new Vector3(point.x, point.y, transform.position.z);
        }

        public void SetSelected(bool selected)
        {
            if (selectionMarker != null) selectionMarker.SetActive(selected);
        }

        /// <summary>Applies the health tint.</summary>
        public void ShowHealth(MemberHealth health)
        {
            if (body == null) return;
            switch (health)
            {
                case MemberHealth.Injured: body.color = baseColor * injuredTint; break;
                case MemberHealth.Trapped:
                case MemberHealth.Dead: body.color = baseColor * downTint; break;
                default: body.color = baseColor; break;
            }
        }

        private static Vector3 SlotOffset(MemberType member)
        {
            switch (member)
            {
                case MemberType.Player: return new Vector3(-0.22f, 0.04f, 0f);
                case MemberType.Wife: return new Vector3(0.22f, 0.04f, 0f);
                default: return new Vector3(0f, -0.1f, 0f);
            }
        }

        private void OnMemberStateChanged(MemberType member, MemberHealth health)
        {
            if (member == type) ShowHealth(health);
        }

        private void OnRewound(string nodeId)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null) ShowHealth(gm.State.GetMember(type).Health);
        }
    }
}
