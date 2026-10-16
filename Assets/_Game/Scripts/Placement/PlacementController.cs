using System;
using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Runs a PlacementNode: select member -> highlight valid anchors -> place.
    /// On timeout: quake, hazard tweens, resolve. First Death/Trapped result rewinds; injuries persist.
    /// </summary>
    public sealed class PlacementController : MonoBehaviour, INodeHandler
    {
        [Tooltip("Owner of the game state.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Provides the current room and the family members.")]
        [SerializeField] private RoomSwitcher rooms;

        private PlacementNode node;
        private Room room;
        private PlacementLayout layout;
        private Action<SceneNode> onFinished;
        private FamilyMember selected;
        private Coroutine quakeRoutine;
        private bool active;
        private bool quaking;

        public bool IsActive => active;
        private GameState State => gameManager.State;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
        }

        public bool CanHandle(SceneNode sceneNode) => sceneNode is PlacementNode;

        public void Begin(SceneNode sceneNode, Action<SceneNode> done)
        {
            Cancel();
            node = (PlacementNode)sceneNode;
            onFinished = done;
            room = rooms != null ? rooms.Current : null;
            if (room == null)
            {
                Debug.LogError($"[Placement] No room shown for node '{node.Id}' (room id '{node.RoomId}').", this);
                return;
            }

            layout = BuildLayout(room, node);
            room.ResetProps();
            rooms.SnapMembers();
            foreach (Anchor anchor in room.Anchors)
            {
                anchor.RefreshLabel(State.RevealedAnchors.Contains(anchor.Id));
            }

            ClearHighlights();
            Subscribe(true);
            active = true;
            GameEvents.RaiseMessage(UiKeys.PlacementHint);
        }

        public void OnTimeout()
        {
            if (active && !quaking)
            {
                quakeRoutine = StartCoroutine(QuakeRoutine());
            }
        }

        public void Cancel()
        {
            if (quakeRoutine != null)
            {
                StopCoroutine(quakeRoutine);
                quakeRoutine = null;
            }

            if (active) Subscribe(false);
            active = false;
            quaking = false;
            Deselect();
            ClearHighlights();
        }

        /// <summary>Current member -> anchor ids, counting only anchors in this room.</summary>
        public Dictionary<MemberType, string> CurrentPlacements()
        {
            var placements = new Dictionary<MemberType, string>();
            foreach (FamilyMemberState member in State.Members)
            {
                placements[member.Type] = layout != null && layout.Contains(member.AnchorId) ? member.AnchorId : null;
            }

            return placements;
        }

        private static PlacementLayout BuildLayout(Room targetRoom, PlacementNode placementNode)
        {
            var result = new PlacementLayout { UnplacedHazard = placementNode.UnplacedHazard };
            foreach (Anchor anchor in targetRoom.Anchors)
            {
                result.Add(anchor.ToInfo());
            }

            return result;
        }

        private void Subscribe(bool on)
        {
            foreach (FamilyMember member in rooms.Members)
            {
                if (member == null) continue;
                if (on) member.Clicked += OnMemberClicked; else member.Clicked -= OnMemberClicked;
            }

            if (room == null) return;
            foreach (Anchor anchor in room.Anchors)
            {
                if (on) anchor.Clicked += OnAnchorClicked; else anchor.Clicked -= OnAnchorClicked;
            }
        }

        private void OnMemberClicked(FamilyMember member)
        {
            if (!active || quaking || gameManager.IsDead) return;
            if (selected == member)
            {
                Deselect();
                ClearHighlights();
                return;
            }

            Dictionary<MemberType, string> placements = CurrentPlacements();
            if (!PlacementRules.CanSelect(member.Type, placements))
            {
                GameEvents.RaiseMessage(UiKeys.PlacementPlayerLast);
                return;
            }

            Deselect();
            selected = member;
            member.SetSelected(true);
            foreach (Anchor anchor in room.Anchors)
            {
                anchor.SetHighlight(PlacementRules.CanPlace(layout, placements, member.Type, anchor.Id, out _));
            }
        }

        private void OnAnchorClicked(Anchor anchor)
        {
            if (!active || quaking || selected == null || gameManager.IsDead) return;

            if (!PlacementRules.CanPlace(layout, CurrentPlacements(), selected.Type, anchor.Id, out string reasonKey))
            {
                GameEvents.RaiseMessage(reasonKey);
                return;
            }

            State.GetMember(selected.Type).AnchorId = anchor.Id;
            selected.PlaceAt(anchor);
            Deselect();
            ClearHighlights();
        }

        private IEnumerator QuakeRoutine()
        {
            quaking = true;
            Deselect();
            ClearHighlights();

            Dictionary<MemberType, string> placements = CurrentPlacements();
            List<PlacementResult> results = PlacementResolver.Resolve(layout, placements, node.Act);
            var info = new QuakeInfo(node.Act, node.QuakeSeconds);

            GameEvents.RaiseQuakeStarted(info);
            AnimateHazards(placements);
            yield return new WaitForSeconds(node.QuakeSeconds);
            GameEvents.RaiseQuakeEnded(info);

            quakeRoutine = null;
            quaking = false;
            Subscribe(false);
            active = false;

            foreach (PlacementResult result in results)
            {
                if (result.Effect == HazardEffect.Injured)
                {
                    StateOps.SetHealth(State, result.Member, MemberHealth.Injured);
                    StateOps.SetFlag(State, FlagNames.Injured(result.Member), true);
                }
            }

            PlacementResult fatal = PlacementResolver.FirstFatal(results);
            if (fatal != null)
            {
                gameManager.Die(BuildDeath(fatal));
                yield break;
            }

            Action<SceneNode> done = onFinished;
            onFinished = null;
            done?.Invoke(node.NextNode);
        }

        private void AnimateHazards(Dictionary<MemberType, string> placements)
        {
            var played = new HashSet<Hazard>();
            foreach (Anchor anchor in room.Anchors)
            {
                PlayHazard(anchor.Hazard, played);
            }

            if (placements.ContainsValue(null))
            {
                PlayHazard(node.UnplacedHazard, played);
            }
        }

        private void PlayHazard(Hazard hazard, HashSet<Hazard> played)
        {
            if (hazard == null || !hazard.IsActiveInAct(node.Act) || !played.Add(hazard)) return;
            HazardProp prop = room.FindProp(hazard.TargetPropId);
            if (prop != null) prop.Play(hazard.Motion);
            AudioService.PlayCue(hazard.Cue);
        }

        private DeathInfo BuildDeath(PlacementResult result)
        {
            if (result.ChildAlone || result.Hazard == null)
            {
                DeathInfo info = node.ChildAloneDeath.ToInfo(result.AnchorId);
                info.Victim = result.Member;
                return info;
            }

            return new DeathInfo
            {
                Victim = result.Member,
                TextKey = result.Hazard.TextKeyFor(result.Member).Key,
                TipId = result.Hazard.Tip != null ? result.Hazard.Tip.Id : null,
                AnchorId = result.AnchorId,
                Cue = null
            };
        }

        private void Deselect()
        {
            if (selected != null) selected.SetSelected(false);
            selected = null;
        }

        private void ClearHighlights()
        {
            if (room == null) return;
            foreach (Anchor anchor in room.Anchors) anchor.SetHighlight(false);
        }
    }
}
