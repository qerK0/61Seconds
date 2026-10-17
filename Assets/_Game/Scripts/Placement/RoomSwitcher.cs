using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Activates the room of the current node and moves the family there (they follow the player between rooms
    /// and floors). A room change plays the RoomTransitionController (lateral on the same floor, descend when the
    /// floor stage changes); rewinds swap instantly.
    /// </summary>
    public sealed class RoomSwitcher : MonoBehaviour, IRoomPresenter
    {
        [Tooltip("Owner of the game state (member anchors).")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("All rooms in the scene.")]
        [SerializeField] private List<Room> rooms = new List<Room>();

        [Tooltip("The three family members.")]
        [SerializeField] private List<FamilyMember> members = new List<FamilyMember>();

        [Tooltip("Plays the room-to-room transition. Optional (instant swap without it).")]
        [SerializeField] private RoomTransitionController transition;

        private int lastFloorStage = -1;
        private bool instantNext = true;

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
        }

        // The rewind after a death re-enters a node: no transition for that.
        private void OnDied(DeathInfo info) => instantNext = true;

        /// <summary>Completes a running transition now (room swapped, input unlocked).</summary>
        public void FinishTransition()
        {
            if (transition != null && transition.IsPlaying) transition.Finish();
        }

        public Room Current { get; private set; }
        public IReadOnlyList<FamilyMember> Members => members;

        /// <summary>Room with the id, or null.</summary>
        public Room FindRoom(string roomId)
        {
            return rooms.Find(r => r != null && r.RoomId == roomId);
        }

        public void ShowRoom(string roomId)
        {
            Room target = rooms.Find(r => r != null && r.RoomId == roomId);
            if (target == null)
            {
                if (!string.IsNullOrEmpty(roomId)) Debug.LogWarning($"[Rooms] No room '{roomId}'.", this);
                return;
            }

            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            int floorStage = gm != null ? gm.State.Act : 0;
            bool floorChanged = lastFloorStage >= 0 && floorStage != lastFloorStage;
            lastFloorStage = floorStage;
            bool instant = instantNext;
            instantNext = false;

            if (target == Current)
            {
                SetActiveRoom(target);
                return;
            }

            if (transition == null || Current == null || instant)
            {
                FinishTransition();
                Swap(target);
                return;
            }

            transition.SetDirection(RoomTransitionController.LateralDirection(rooms.IndexOf(Current), rooms.IndexOf(target)));
            transition.Play(floorChanged ? TransitionKind.Descend : TransitionKind.Lateral, Current.RoomId, target.RoomId, () => Swap(target));
        }

        private void Swap(Room target)
        {
            SetActiveRoom(target);
            if (target == Current) return;
            Current = target;
            SnapMembers();
        }

        private void SetActiveRoom(Room target)
        {
            foreach (Room room in rooms)
            {
                if (room != null) room.gameObject.SetActive(room == target);
            }
        }

        /// <summary>Puts each member on their stored anchor in this room, or at the room spawn.</summary>
        public void SnapMembers()
        {
            if (Current == null) return;
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            foreach (FamilyMember member in members)
            {
                if (member == null) continue;
                string anchorId = gm != null ? gm.State.GetMember(member.Type).AnchorId : null;
                Anchor anchor = Current.FindAnchor(anchorId);
                if (anchor != null) member.PlaceAt(anchor);
                else member.PlaceAt(Current.GetSpawn(member.Type));
            }
        }
    }
}
