using SixtyOne.Cinematic;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Iso
{
    /// <summary>
    /// Frames the active room inside the screen area the HUD leaves free (choice panel below, bag on the right,
    /// timer on top). Moves this rig (the camera is its child, so CameraShake and the cinematic tilt still work
    /// on the camera's local transform) and sets the un-zoomed orthographic size.
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
        [SerializeField] private Rect freeViewport = new Rect(0.02f, 0.41f, 0.83f, 0.42f);

        [Tooltip("World-unit margin around the room bounds.")]
        [SerializeField] private float padding = 0.08f;

        [Tooltip("Never zoom in closer than this orthographic size.")]
        [SerializeField] private float minOrthoSize = 2.2f;

        private Room framed;
        private float framedAspect;

        private void LateUpdate()
        {
            if (rooms == null || targetCamera == null) return;
            Room current = rooms.Current;
            if (current == null || (current == framed && Mathf.Approximately(framedAspect, targetCamera.aspect))) return;
            Frame(current);
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
            float sizeForHeight = h / (2f * freeViewport.height);
            float sizeForWidth = w / (2f * freeViewport.width * aspect);
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
