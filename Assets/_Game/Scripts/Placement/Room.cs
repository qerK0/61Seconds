using System.Collections.Generic;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>One isometric room: its anchors, hazard props and member spawn points.</summary>
    public sealed class Room : MonoBehaviour
    {
        [Tooltip("Matches SceneNode.RoomId.")]
        [SerializeField] private string roomId;

        [Tooltip("Where the player stands when entering the room unplaced.")]
        [SerializeField] private Transform playerSpawn;

        [Tooltip("Where the wife stands when entering the room unplaced.")]
        [SerializeField] private Transform wifeSpawn;

        [Tooltip("Where the child stands when entering the room unplaced.")]
        [SerializeField] private Transform childSpawn;

        [Tooltip("World-space rect the camera must show for this room (floor + walls + tall props).")]
        [SerializeField] private Rect viewBounds = new Rect(-2f, 0f, 4f, 3f);

        [Tooltip("Rooms of one flat that share a node (entry room id). Empty = this room's own id.")]
        [SerializeField] private string zoneId;

        [Tooltip("Walkable floor in grid cells (click-and-run is clamped to it).")]
        [SerializeField] private FloorArea floor = new FloorArea(4, 4);

        private Anchor[] anchors;
        private RoomLink[] links;

        /// <summary>World rect to frame (stored room-local, so it follows the room's position and world scale).</summary>
        public Rect ViewBounds
        {
            get
            {
                Vector3 min = transform.TransformPoint(new Vector3(viewBounds.xMin, viewBounds.yMin, 0f));
                Vector3 max = transform.TransformPoint(new Vector3(viewBounds.xMax, viewBounds.yMax, 0f));
                return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
            }
        }

        /// <summary>World units per room-local unit (RoomVisualConfig.WorldScale, applied to the World root).</summary>
        public float Scale => Mathf.Abs(transform.lossyScale.x);

        /// <summary>A room-local offset (tuned at scale 1) as a world vector.</summary>
        public Vector3 WorldOffset(Vector2 localOffset) => transform.TransformVector(new Vector3(localOffset.x, localOffset.y, 0f));
        private HazardProp[] props;

        public string RoomId => string.IsNullOrEmpty(roomId) ? name : roomId;
        public string ZoneId => string.IsNullOrEmpty(zoneId) ? RoomId : zoneId;
        public FloorArea Floor => floor;

        /// <summary>Doors and choice hotspots in this room.</summary>
        public RoomLink[] Links
        {
            get
            {
                if (links == null) links = GetComponentsInChildren<RoomLink>(true);
                return links;
            }
        }

        /// <summary>Nearest walkable world point (margin in grid cells from the walls).</summary>
        public Vector3 ClampToFloor(Vector3 world, float margin = 0.2f)
        {
            Vector3 local = floor.ClampLocal(transform.InverseTransformPoint(world), margin);
            Vector3 clamped = transform.TransformPoint(new Vector3(local.x, local.y, 0f));
            return new Vector3(clamped.x, clamped.y, world.z);
        }

        /// <summary>World waypoints from one point to another on this room's floor (the last one is the clamped target).</summary>
        public List<Vector3> PlanPath(Vector3 fromWorld, Vector3 toWorld, float margin = 0.2f)
        {
            return new List<Vector3> { ClampToFloor(toWorld, margin) };
        }

        /// <summary>Where to stand to use something whose approach point is given (clamped onto the floor).</summary>
        public Vector3 ApproachTarget(Vector3 approachWorld, float margin = 0.2f)
        {
            return ClampToFloor(approachWorld, margin);
        }

        public Anchor[] Anchors
        {
            get
            {
                if (anchors == null) anchors = GetComponentsInChildren<Anchor>(true);
                return anchors;
            }
        }

        public HazardProp[] Props
        {
            get
            {
                if (props == null) props = GetComponentsInChildren<HazardProp>(true);
                return props;
            }
        }

        public Anchor FindAnchor(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId)) return null;
            foreach (Anchor a in Anchors)
            {
                if (a.Id == anchorId) return a;
            }

            return null;
        }

        public HazardProp FindProp(string propId)
        {
            if (string.IsNullOrEmpty(propId)) return null;
            foreach (HazardProp p in Props)
            {
                if (p.PropId == propId) return p;
            }

            return null;
        }

        /// <summary>Spawn point for a member (room position if none set).</summary>
        public Vector3 GetSpawn(MemberType member)
        {
            Transform t = member == MemberType.Player ? playerSpawn : member == MemberType.Wife ? wifeSpawn : childSpawn;
            return t != null ? t.position : transform.position;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void ConfigureLayout(string zone, FloorArea area)
        {
            zoneId = zone;
            floor = area;
        }

        public void ResetProps()
        {
            foreach (HazardProp p in Props) p.ResetPose();
        }
    }
}
