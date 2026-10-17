using System;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Activates the room of the current node and moves the family there (they follow the player between rooms
    /// and floors). Rooms of one flat share a zone (one node): entering the zone through a door shows the room
    /// behind that door; a node change inside the zone keeps the current room. A room change plays the
    /// RoomTransitionController (lateral on the same floor, descend when the floor stage changes); rewinds swap
    /// instantly. GameState.CurrentRoomId always names the room actually shown.
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

        [Tooltip("Where the wife appears around the player when arriving through a door (world units).")]
        [SerializeField] private Vector2 wifeArrivalOffset = new Vector2(0.38f, 0.1f);

        [Tooltip("Where the child appears around the player when arriving through a door (world units).")]
        [SerializeField] private Vector2 childArrivalOffset = new Vector2(-0.32f, 0.12f);

        /// <summary>Raised when a room becomes the shown room (after the transition midpoint).</summary>
        public event Action<Room> RoomShown;

        private int lastFloorStage = -1;
        private bool instantNext = true;
        private string pendingRoom;
        private Vector3? pendingPoint;

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
        public IReadOnlyList<Room> Rooms => rooms;

        /// <summary>Room with the id, or null.</summary>
        public Room FindRoom(string roomId)
        {
            return rooms.Find(r => r != null && r.RoomId == roomId);
        }

        /// <summary>Zone (flat) of a room id; unknown rooms are their own zone.</summary>
        public string ZoneOf(string roomId)
        {
            Room room = FindRoom(roomId);
            return room != null ? room.ZoneId : roomId;
        }

        public IEnumerable<Room> RoomsInZone(string zoneId)
        {
            foreach (Room room in rooms)
            {
                if (room != null && room.ZoneId == zoneId) yield return room;
            }
        }

        /// <summary>The next node entry that lands in this room's zone shows this room with the player at point.</summary>
        public void SetPendingArrival(string roomId, Vector3? point)
        {
            pendingRoom = roomId;
            pendingPoint = point;
        }

        public void ClearPendingArrival()
        {
            pendingRoom = null;
            pendingPoint = null;
        }

        /// <summary>Shows the node's room; preferredRoomId (a rewind's saved room) wins if it is in the same zone.</summary>
        public void ShowRoom(string roomId, string preferredRoomId)
        {
            Room target = FindRoom(roomId);
            if (target == null)
            {
                if (!string.IsNullOrEmpty(roomId)) Debug.LogWarning($"[Rooms] No room '{roomId}'.", this);
                return;
            }

            string zone = target.ZoneId;
            Vector3? arrival = null;
            Room pending = pendingRoom != null ? FindRoom(pendingRoom) : null;
            Room preferred = preferredRoomId != null ? FindRoom(preferredRoomId) : null;
            if (pending != null && pending.ZoneId == zone)
            {
                target = pending;
                arrival = pendingPoint;
            }
            else if (preferred != null && preferred.ZoneId == zone)
            {
                target = preferred;
            }
            else if (Current != null && Current.ZoneId == zone)
            {
                target = Current;
            }

            ClearPendingArrival();
            Show(target, arrival);
        }

        /// <summary>Another room of the same flat through a door (no node change).</summary>
        public void ShowLocal(string roomId, Vector3? arrival)
        {
            Room target = FindRoom(roomId);
            if (target != null) Show(target, arrival);
        }

        private void Show(Room target, Vector3? arrival)
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm != null) gm.State.CurrentRoomId = target.RoomId;
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
                Swap(target, arrival);
                return;
            }

            transition.Play(floorChanged ? TransitionKind.Descend : TransitionKind.Lateral, Current, target, () => Swap(target, arrival));
        }

        private void Swap(Room target, Vector3? arrival)
        {
            SetActiveRoom(target);
            if (target == Current) return;
            Current = target;
            SnapMembers(arrival);
            RoomShown?.Invoke(target);
        }

        private void SetActiveRoom(Room target)
        {
            foreach (Room room in rooms)
            {
                if (room != null) room.gameObject.SetActive(room == target);
            }
        }

        /// <summary>Puts each member on their stored anchor in this room, else around the arrival point, else at the room spawn.</summary>
        public void SnapMembers(Vector3? arrival = null)
        {
            if (Current == null) return;
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            foreach (FamilyMember member in members)
            {
                if (member == null) continue;
                string anchorId = gm != null ? gm.State.GetMember(member.Type).AnchorId : null;
                Anchor anchor = Current.FindAnchor(anchorId);
                if (anchor != null) member.PlaceAt(anchor);
                else if (arrival.HasValue) member.PlaceAt(Current.ClampToFloor(arrival.Value + Current.WorldOffset(ArrivalOffset(member.Type))));
                else member.PlaceAt(Current.GetSpawn(member.Type));
            }
        }

        private Vector2 ArrivalOffset(MemberType type)
        {
            switch (type)
            {
                case MemberType.Wife: return wifeArrivalOffset;
                case MemberType.Child: return childArrivalOffset;
                default: return Vector2.zero;
            }
        }
    }
}
