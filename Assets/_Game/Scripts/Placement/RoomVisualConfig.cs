using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// One world scale for the whole building, applied by the scene builder to the single World root that holds
    /// every room (floor Tilemaps, wall segments, props, anchors, hotspots, doors, spawns) and the family. Because
    /// everything is scaled together, nothing drifts out of alignment and tile PPU never changes (no seams).
    /// Runtime offsets and speeds tuned at scale 1 read the scale from the hierarchy (Room.Scale).
    /// Note: RoomCameraFraming fits each room to the playfield, so rooms already larger than the minimum zoom keep
    /// their on-screen size; the scale mainly grows small rooms (zoom limit) and everything relative to the
    /// unscaled HUD, world-space light radii are scaled by the builder.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Room Visual Config", fileName = "RoomVisualConfig")]
    public sealed class RoomVisualConfig : ScriptableObject
    {
        [Tooltip("Uniform scale of all room visuals and characters (1 = original tile size).")]
        [Range(0.5f, 3f)]
        [SerializeField] private float worldScale = 1.5f;

        [Tooltip("How many wall-face rows the scene builder stacks on each back-wall segment (the upper row carries the windows).")]
        [Range(1, 3)]
        [SerializeField] private int wallRows = 2;

        public float WorldScale => worldScale;
        public int WallRows => wallRows;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(float scale, int rows = 2)
        {
            worldScale = Mathf.Clamp(scale, 0.5f, 3f);
            wallRows = Mathf.Clamp(rows, 1, 3);
        }
    }
}
