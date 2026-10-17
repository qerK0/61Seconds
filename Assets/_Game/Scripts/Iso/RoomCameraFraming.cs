using SixtyOne.Cinematic;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Iso
{
    /// <summary>
    /// Frames the active room so it fills ~97% (fill) of the playfield: the whole screen minus the slim bag column
    /// on the right and the timer strip on top (the choice panel is a sliding overlay, not reserved space). Moves this rig (the camera is its child, so CameraShake and the cinematic tilt still work
    /// on the camera's local transform) and sets the un-zoomed orthographic size. Long rooms (Room.FollowCamera, the
    /// street) switch to follow mode: a fixed size showing ~Room.FollowCells cells, the rig following the player
    /// along the long axis with smooth damping, clamped to the street's ends.
    /// </summary>
    public sealed class RoomCameraFraming : MonoBehaviour
    {
        [Tooltip("Source of the active room.")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Camera to size (child of this rig).")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Optional: owns the cinematic zoom; receives the base size instead of the camera.")]
        [SerializeField] private CinematicDirector director;

        [Tooltip("Free screen area in viewport coordinates (0..1) the room must fit into.")]
        [SerializeField] private Rect freeViewport = new Rect(0f, 0f, 0.915f, 0.93f);

        [Tooltip("World-unit margin around the room bounds.")]
        [SerializeField] private float padding = 0f;

        [Tooltip("Never zoom in closer than this orthographic size.")]
        [SerializeField] private float minOrthoSize = 1.0f;

        [Tooltip("Share of the free area the room bounds fill (0.97 = 97%: the room dominates the screen).")]
        [Range(0.5f, 1f)]
        [SerializeField] private float fill = 0.97f;

        [Tooltip("Follow-mode rooms (the street): who the camera follows.")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Follow mode: smoothing time of the camera along the street.")]
        [Min(0f)]
        [SerializeField] private float followSmoothTime = 0.35f;

        [Tooltip("Follow mode: world height (room-local units) kept visible above the floor band for the façades.")]
        [SerializeField] private float followHeadroom = 1.2f;

        private Room framed;
        private float framedAspect;
        private float followSize;
        private Vector3 followVelocity;

        private void LateUpdate()
        {
            if (rooms == null || targetCamera == null) return;
            Room current = rooms.Current;
            if (current == null) return;
            bool changed = current != framed || !Mathf.Approximately(framedAspect, targetCamera.aspect);
            if (current.FollowCamera && followTarget != null)
            {
                if (changed) BeginFollow(current);
                else Follow(current, true);
                return;
            }

            if (changed) Frame(current);
        }

        /// <summary>
        /// Follow mode: a fixed ortho size showing about Room.FollowCells cells of the long axis (at least the street's
        /// depth band plus the façades), snapped onto the player.
        /// </summary>
        private void BeginFollow(Room room)
        {
            framed = room;
            framedAspect = targetCamera.aspect;
            float aspect = Mathf.Max(0.1f, targetCamera.aspect);
            Vector3 axis = AxisVector(room, out float length);
            float cellAlong = length / Mathf.Max(1, room.Floor.Width);
            float visibleWidth = room.FollowCells * cellAlong * Mathf.Abs(axis.x);
            float sizeForWidth = visibleWidth / (2f * freeViewport.width * fill * aspect);
            float band = room.Floor.Height * 0.25f * room.Scale + followHeadroom * room.Scale;
            float sizeForHeight = band / (2f * freeViewport.height * fill);
            followSize = Mathf.Max(minOrthoSize, sizeForWidth, sizeForHeight);
            if (director != null) director.BaseOrthoSize = followSize;
            else targetCamera.orthographicSize = followSize;
            followVelocity = Vector3.zero;
            Follow(room, false);
        }

        /// <summary>Moves the rig along the street's centre line to the player, clamped to the street ends.</summary>
        private void Follow(Room room, bool smooth)
        {
            float aspect = Mathf.Max(0.1f, targetCamera.aspect);
            Vector3 start = room.GridToWorld(new Vector2(0f, room.Floor.Height * 0.5f));
            Vector3 axis = AxisVector(room, out float length);
            float t = Vector3.Dot(followTarget.position - start, axis);
            float halfVisible = followSize * aspect * freeViewport.width * fill;
            float halfAlong = halfVisible / Mathf.Max(0.1f, Mathf.Abs(axis.x));
            t = length <= halfAlong * 2f ? length * 0.5f : Mathf.Clamp(t, halfAlong, length - halfAlong);
            Vector3 centre = start + axis * t + new Vector3(0f, followHeadroom * 0.5f * room.Scale, 0f);

            Vector2 offsetViewport = freeViewport.center - new Vector2(0.5f, 0.5f);
            Vector2 offsetWorld = new Vector2(offsetViewport.x * 2f * followSize * aspect, offsetViewport.y * 2f * followSize);
            var goal = new Vector3(centre.x - offsetWorld.x, centre.y - offsetWorld.y, transform.position.z);
            transform.position = smooth ? Vector3.SmoothDamp(transform.position, goal, ref followVelocity, followSmoothTime) : goal;
        }

        /// <summary>Unit world direction of the floor's long axis (grid x) and its world length.</summary>
        private static Vector3 AxisVector(Room room, out float length)
        {
            Vector3 a = room.GridToWorld(new Vector2(0f, 0f));
            Vector3 b = room.GridToWorld(new Vector2(room.Floor.Width, 0f));
            Vector3 d = b - a;
            length = d.magnitude;
            return length > 0.0001f ? d / length : Vector3.right;
        }

        /// <summary>Snaps the rig and size so the room fills the free viewport area.</summary>
        public void Frame(Room room)
        {
            framed = room;
            framedAspect = targetCamera.aspect;
            Rect b = room.ViewBounds;
            float w = b.width + padding * 2f;
            float h = b.height + padding * 2f;
            float aspect = Mathf.Max(0.1f, targetCamera.aspect);

            // Ortho size = half screen height; the free area covers freeViewport of the screen.
            float sizeForHeight = h / (2f * freeViewport.height * fill);
            float sizeForWidth = w / (2f * freeViewport.width * fill * aspect);
            float size = Mathf.Max(minOrthoSize, sizeForHeight, sizeForWidth);

            // Put the bounds centre at the centre of the free area.
            Vector2 offsetViewport = freeViewport.center - new Vector2(0.5f, 0.5f);
            Vector2 offsetWorld = new Vector2(offsetViewport.x * 2f * size * aspect, offsetViewport.y * 2f * size);
            Vector2 centre = b.center - offsetWorld;
            transform.position = new Vector3(centre.x, centre.y, transform.position.z);

            if (director != null) director.BaseOrthoSize = size;
            else targetCamera.orthographicSize = size;
        }
    }
}
