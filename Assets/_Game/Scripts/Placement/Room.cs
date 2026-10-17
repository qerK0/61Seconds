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

        private Anchor[] anchors;

        /// <summary>World rect to frame (relative to the room's position).</summary>
        public Rect ViewBounds
        {
            get
            {
                Rect r = viewBounds;
                r.position += (Vector2)transform.position;
                return r;
            }
        }
        private HazardProp[] props;

        public string RoomId => string.IsNullOrEmpty(roomId) ? name : roomId;

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

        public void ResetProps()
        {
            foreach (HazardProp p in Props) p.ResetPose();
        }
    }
}
