using System;
using System.Collections.Generic;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Hand-placed room edits from the Layout Editor (Data/Layout/RoomLayoutOverrides.asset). One entry per room id,
    /// each a list of placed objects. An object with a code id (BuildingLayout) replaces that object's position,
    /// flip, scale, art and data, or removes it; a new id adds an object; a door entry moves the door to another wall
    /// gap (its link stays in code). Applied by every Build Test Scene. Plain text
    /// YAML, so git diffs show every change.
    /// </summary>
    public sealed class RoomLayoutOverrides : ScriptableObject
    {
        /// <summary>One hand-placed (or hand-edited / removed) object.</summary>
        [Serializable]
        public sealed class PlacedObject
        {
            public string id;
            public LayoutKind kind;
            [Tooltip("Deletes the code object with this id.")]
            public bool removed;
            public float gx;
            public float gy;
            public float lift;
            public bool flipX;
            [Tooltip("PropArtSet key (props, container art).")]
            public string propName;
            public bool blocksNav;

            [Header("Pickup / hidden item")]
            public string itemId;
            public float cost = 1f;
            public bool borrowed;
            public bool surface = true;
            public bool prepOnly;

            [Header("Container")]
            public string containerType;
            public List<string> contents = new List<string>();
            public List<string> hiddenContents = new List<string>();
            public float searchSeconds = 1.5f;
            public float buildingCost = 3f;

            [Header("Hazard prop")]
            public bool hazardProp;
            public string hazardPropId;
            public float topple = -80f;
            public float drop = 0.8f;

            [Header("Light")]
            public float radius = 2.4f;
            public Color lightColor = new Color(1f, 0.78f, 0.5f, 1f);

            [Header("Block (unmapped art)")]
            public Vector2 scale = Vector2.one;
            public Color tint = Color.white;

            [Header("Transform")]
            [Tooltip("Uniform scale on top of the built size (older entries without it load as 1).")]
            public float objectScale = 1f;
            [Tooltip("Sorting nudge (+ = in front).")]
            public int sortNudge;

            [Header("Door")]
            [Tooltip("Wall-gap edge code the door sits in.")]
            public string doorEdge;
            [Tooltip("Door / Elevator / Stairwell / Stairs.")]
            public string doorArt;

            /// <summary>The scale to build with: entries saved before scale existed (0) count as 1.</summary>
            public float Scale1 => objectScale > 0.01f ? objectScale : 1f;
        }

        /// <summary>All edits of one room.</summary>
        [Serializable]
        public sealed class RoomEntry
        {
            public string roomId;
            public List<PlacedObject> objects = new List<PlacedObject>();
        }

        [SerializeField] private List<RoomEntry> rooms = new List<RoomEntry>();

        public IReadOnlyList<RoomEntry> Rooms => rooms;

        /// <summary>The room's entry, or null.</summary>
        public RoomEntry Find(string roomId) => rooms.Find(r => r.roomId == roomId);

        /// <summary>Replaces a room's edits (empty list = drops the room entry).</summary>
        public void SetRoom(string roomId, List<PlacedObject> objects)
        {
            rooms.RemoveAll(r => r.roomId == roomId);
            if (objects != null && objects.Count > 0)
            {
                rooms.Add(new RoomEntry { roomId = roomId, objects = objects });
                rooms.Sort((a, b) => string.CompareOrdinal(a.roomId, b.roomId));
            }
        }
    }
}
