using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Drop-Cover-Hold for the family, automatic: when a quake starts shaking, every member who is alive and not
    /// Trapped crouches (CharacterAnimator) and holds; when it ends they stand back up. Placement quakes (opening,
    /// F3, F2) freeze movement while anyone is crouched (CrouchGate); the prep-zero onset rumble is ignored (the
    /// family still has to be placed). The street quake crouches whoever stands still: running to the open stands
    /// up at once. Deaths, Trapped and rewinds snap straight back to idle.
    /// </summary>
    public sealed class CrouchDirector : MonoBehaviour
    {
        [Tooltip("State owner. Defaults to GameManager.Instance.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current node (placement quake vs the street).")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Tells the real placement quake from the prep-zero onset rumble.")]
        [SerializeField] private PlacementController placement;

        [Tooltip("The family (found in the scene if empty).")]
        [SerializeField] private List<FamilyMember> members = new List<FamilyMember>();

        private readonly Dictionary<MemberType, CharacterAnimator> animators = new Dictionary<MemberType, CharacterAnimator>();
        private bool holding;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        private void Start()
        {
            if (members.Count == 0) members.AddRange(FindObjectsByType<FamilyMember>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (FamilyMember member in members)
            {
                CharacterAnimator animator = member != null ? member.GetComponent<CharacterAnimator>() : null;
                if (animator != null) animators[member.Type] = animator;
            }
        }

        private void OnEnable()
        {
            GameEvents.QuakeStarted += OnQuakeStarted;
            GameEvents.QuakeEnded += OnQuakeEnded;
            GameEvents.Rewound += OnRewound;
            GameEvents.Died += OnDied;
            GameEvents.MemberStateChanged += OnMemberStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.QuakeStarted -= OnQuakeStarted;
            GameEvents.QuakeEnded -= OnQuakeEnded;
            GameEvents.Rewound -= OnRewound;
            GameEvents.Died -= OnDied;
            GameEvents.MemberStateChanged -= OnMemberStateChanged;
            CrouchGate.Frozen = false;
        }

        private void OnQuakeStarted(QuakeInfo info)
        {
            SceneNode current = nodePlayer != null ? nodePlayer.Current : null;
            bool placementNode = current is PlacementNode;
            // Prep zero raises an onset rumble (still on the prep node, just before the placement node is entered):
            // the family has to be placed first. Only the placement's own quake (or the street) crouches.
            if (current is SixtyOne.Prep.PrepPhaseNode) return;
            if (placementNode && (placement == null || !placement.IsQuaking)) return;
            CrouchMode mode = placementNode ? CrouchMode.Hold : CrouchMode.WhenStill;
            GameState state = Gm != null ? Gm.State : null;
            foreach (KeyValuePair<MemberType, CharacterAnimator> pair in animators)
            {
                MemberHealth health = state != null ? state.GetMember(pair.Key).Health : MemberHealth.Healthy;
                if (health == MemberHealth.Dead || health == MemberHealth.Trapped) continue;
                pair.Value.StartCrouch(mode);
            }

            holding = mode == CrouchMode.Hold;
            CrouchGate.Frozen = holding;
        }

        private void OnQuakeEnded(QuakeInfo info)
        {
            foreach (CharacterAnimator animator in animators.Values) animator.StandUp();
        }

        private void Update()
        {
            // The freeze lasts until everyone is standing again (the stand-up plays out first).
            if (!holding) return;
            foreach (CharacterAnimator animator in animators.Values)
            {
                if (animator.IsCrouching) return;
            }

            holding = false;
            CrouchGate.Frozen = false;
        }

        private void OnRewound(string nodeId)
        {
            foreach (CharacterAnimator animator in animators.Values) animator.ResetCrouch();
            holding = false;
            CrouchGate.Frozen = false;
        }

        private void OnDied(DeathInfo info) => Reset(info.Victim);

        private void OnMemberStateChanged(MemberType member, MemberHealth health)
        {
            if (health == MemberHealth.Dead || health == MemberHealth.Trapped) Reset(member);
        }

        private void Reset(MemberType member)
        {
            if (animators.TryGetValue(member, out CharacterAnimator animator)) animator.ResetCrouch();
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(GameManager manager, NodePlayer player, PlacementController controller, IEnumerable<FamilyMember> family)
        {
            gameManager = manager;
            nodePlayer = player;
            placement = controller;
            members = new List<FamilyMember>(family);
        }
    }
}
