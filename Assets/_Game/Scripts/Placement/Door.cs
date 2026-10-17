using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A doorway on a wall gap (or a stair/exit spot) that leads to another room. Into a room of the same flat
    /// it switches rooms directly (MovementConfig.DoorSeconds); into another zone it plays the current node's
    /// move choice that leads there (that choice's cost, conditions and outcomes apply).
    /// </summary>
    public sealed class Door : RoomLink
    {
        [Tooltip("Room this door leads to (Room.RoomId).")]
        [SerializeField] private string targetRoomId;

        [Tooltip("Where the player appears in the target room (usually just inside its matching door).")]
        [SerializeField] private Transform arrivalPoint;

        public string TargetRoomId => targetRoomId;
        public Transform ArrivalPoint => arrivalPoint;

        /// <summary>Authoring helper for the scene builder.</summary>
        public void ConfigureDoor(string targetRoom, Transform arrival)
        {
            targetRoomId = targetRoom;
            arrivalPoint = arrival;
        }
    }
}
