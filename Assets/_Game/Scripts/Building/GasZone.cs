using System;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>A room with a gas leak: poisons the player while its valve flag is false.</summary>
    [Serializable]
    public sealed class GasZone
    {
        [Tooltip("Room id (Room.RoomId / SceneNode.RoomId).")]
        [SerializeField] private string roomId;

        [Tooltip("Flag set by closing this room's valve (e.g. GasOff_F3_10).")]
        [SerializeField] private string valveFlag;

        [Tooltip("Poison gained per second while the valve is open (0..1 scale; 1 = death).")]
        [SerializeField] private float poisonPerSecond = 0.02f;

        public string RoomId => roomId;
        public string ValveFlag => valveFlag;
        public float PoisonPerSecond => poisonPerSecond;

        public GasZone()
        {
        }

        public GasZone(string room, string valve, float perSecond)
        {
            roomId = room;
            valveFlag = valve;
            poisonPerSecond = perSecond;
        }
    }
}
