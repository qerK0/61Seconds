using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>Activates the room of the current node and moves the family there.</summary>
    public sealed class RoomSwitcher : MonoBehaviour, IRoomPresenter
    {
        [Tooltip("Owner of the game state (member anchors).")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("All rooms in the scene.")]
        [SerializeField] private List<Room> rooms = new List<Room>();

        [Tooltip("The three family members.")]
        [SerializeField] private List<FamilyMember> members = new List<FamilyMember>();

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

            foreach (Room room in rooms)
            {
                if (room != null) room.gameObject.SetActive(room == target);
            }

            if (target == Current) return;
            Current = target;
            SnapMembers();
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
