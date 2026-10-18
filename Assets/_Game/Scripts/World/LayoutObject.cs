using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>What a hand-placeable room object is (Layout Editor).</summary>
    public enum LayoutKind
    {
        Prop = 0,
        Pickup = 1,
        Container = 2,
        HiddenItem = 3,
        Light = 4,
        /// <summary>A door or stairwell door: moves along its wall (edge codes); link, choice and costs stay in code.</summary>
        Door = 5
    }

    /// <summary>
    /// Editor tag on every hand-placeable object the scene builder makes (props, pickups, containers, hidden items,
    /// lights) and on objects spawned from the Layout Editor palette. It carries the object's layout data, so
    /// "Save room" can read the room back into RoomLayoutOverrides. Pure data: no runtime behaviour.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LayoutObject : MonoBehaviour
    {
        [Tooltip("Stable id inside the room (code ids are kept; new objects get a new id).")]
        public string Id;

        [Tooltip("What this object is.")]
        public LayoutKind Kind;

        [Tooltip("True if BuildingLayout (code) defines this object; false for objects added in the Layout Editor.")]
        public bool FromCode;

        [Tooltip("Placeholder made by the Layout Editor palette; the real object appears after Save + Rebuild scene.")]
        public bool Proxy;

        [Tooltip("Height above the floor (room-local units) the builder used.")]
        public float Lift;

        [Tooltip("Mirror the sprite.")]
        public bool FlipX;

        [Tooltip("Prop / container art name (a PropArtSet key).")]
        public string PropName;

        [Tooltip("Static object that blocks its nav cell.")]
        public bool BlocksNav;

        [Header("Pickup / hidden item")]
        [Tooltip("Item id (Data/Items).")]
        public string ItemId;

        [Tooltip("Building-time cost to take it.")]
        public float Cost = 1f;

        [Tooltip("Neighbour's: taking it is 'ödünç al' (borrow ledger).")]
        public bool Borrowed;

        [Tooltip("Lies on an open surface (icon) instead of inside furniture.")]
        public bool Surface = true;

        [Tooltip("Our own flat: only during the pre-quake prep.")]
        public bool PrepOnly;

        [Header("Container")]
        [Tooltip("wardrobe / wardrobe_v2 / cabinet / kitchen_cabinet / bookshelf / drawer / toolbox / nightstand.")]
        public string ContainerType;

        [Tooltip("Item ids inside.")]
        public List<string> Contents = new List<string>();

        [Tooltip("Hidden extras (inside once searched in an earlier loop).")]
        public List<string> HiddenContents = new List<string>();

        [Tooltip("Real seconds the search ring takes.")]
        public float SearchSeconds = 1.5f;

        [Tooltip("Building seconds the first search costs.")]
        public float BuildingCost = 3f;

        [Header("Hazard prop")]
        [Tooltip("Animated by a hazard (topple / drop) with this prop id.")]
        public bool HazardProp;

        [Tooltip("Prop id the hazard looks for (e.g. balcony_rail).")]
        public string HazardPropId;

        public float Topple = -80f;
        public float Drop = 0.8f;

        [Header("Light")]
        public float Radius = 2.4f;
        public Color LightColor = new Color(1f, 0.78f, 0.5f, 1f);

        [Header("Block (unmapped prop art)")]
        public Vector2 Scale = Vector2.one;
        public Color Tint = Color.white;

        [Header("Transform (every placed object)")]
        [Tooltip("Uniform scale on top of the built size (1 = as built). The Scene scale tool and the window slider change it.")]
        public float ObjectScale = 1f;

        [Tooltip("Transform scale at ObjectScale 1 (what the builder made); the saved scale is localScale / this.")]
        public Vector2 BaseScale = Vector2.one;

        [Tooltip("Sorting nudge added to the iso sort order (+ = in front, - = behind).")]
        public int SortNudge;

        [Header("Door")]
        [Tooltip("Wall-gap edge code (L0.., R0..) the door sits in.")]
        public string DoorEdge;

        [Tooltip("Door / Elevator / Stairwell (door art + MERDİVEN sign).")]
        public string DoorArt;

        [Tooltip("Read-only: the room this door leads to (code-defined, like its choice and costs).")]
        public string DoorTarget;

        [Tooltip("Read-only: two-way door (the arrival on the other side follows it) or a one-way exit.")]
        public bool DoorTwoWay;
    }
}
