using UnityEngine;
using UnityEngine.Tilemaps;

namespace SixtyOne.World
{
    /// <summary>
    /// Art for one kind of room: the iso floor tile and the back-wall segments. Wall sprites are 128x192 segments
    /// covering one tile edge. "Left" segments run along the back-left edge ("/"), "right" segments along the
    /// back-right edge ("\"); if no right sprites are set, the left ones are flipped. Variants alternate by index.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Room Tile Set", fileName = "RoomTileSet")]
    public sealed class RoomTileSet : ScriptableObject
    {
        [Tooltip("Floor diamond (256x128, center pivot).")]
        [SerializeField] private Sprite floorSprite;

        [Tooltip("Tile asset wrapping the floor sprite (for the isometric Tilemap).")]
        [SerializeField] private TileBase floorTile;

        [Tooltip("Back-left edge segments (\"/\"), alternated along the wall.")]
        [SerializeField] private Sprite[] leftWallSprites = new Sprite[0];

        [Tooltip("Back-right edge segments (\"\\\"); empty = flip the left ones.")]
        [SerializeField] private Sprite[] rightWallSprites = new Sprite[0];

        [Tooltip("Optional window segment for the back-left edge.")]
        [SerializeField] private Sprite leftWindowSprite;

        [Tooltip("Optional window segment for the back-right edge.")]
        [SerializeField] private Sprite rightWindowSprite;

        [Tooltip("Cracked back-left segments (swapped in after quakes by WallCracks).")]
        [SerializeField] private Sprite[] crackedLeftSprites = new Sprite[0];

        [Tooltip("Cracked back-right segments; empty = flip the cracked left ones.")]
        [SerializeField] private Sprite[] crackedRightSprites = new Sprite[0];

        [Tooltip("Optional patch tile mixed into the floor (street: Floor Rocky 3 among Floor Rocky 17).")]
        [SerializeField] private TileBase patchTile;

        [Tooltip("Share of floor cells using the patch tile (seeded per cell).")]
        [Range(0f, 1f)]
        [SerializeField] private float patchChance;

        [Tooltip("Floor tint.")]
        [SerializeField] private Color floorTint = Color.white;

        [Tooltip("Wall tint.")]
        [SerializeField] private Color wallTint = Color.white;

        public Sprite FloorSprite => floorSprite;
        public TileBase FloorTile => floorTile;
        public Color FloorTint => floorTint;
        public Color WallTint => wallTint;
        public bool HasWalls => leftWallSprites != null && leftWallSprites.Length > 0;
        public TileBase PatchTile => patchTile;
        public float PatchChance => patchChance;

        /// <summary>Authoring helper: random patches of a second floor tile.</summary>
        public void ConfigurePatch(TileBase tile, float chance)
        {
            patchTile = tile;
            patchChance = chance;
        }

        /// <summary>Wall sprite for a segment; flipX is true when a left sprite stands in for a right edge.</summary>
        public Sprite WallSprite(bool rightEdge, int index, bool window, out bool flipX)
        {
            flipX = false;
            if (window)
            {
                if (rightEdge && rightWindowSprite != null) return rightWindowSprite;
                if (!rightEdge && leftWindowSprite != null) return leftWindowSprite;
                if (rightEdge && leftWindowSprite != null)
                {
                    flipX = true;
                    return leftWindowSprite;
                }
            }

            if (rightEdge && rightWallSprites != null && rightWallSprites.Length > 0)
            {
                return rightWallSprites[Mathf.Abs(index) % rightWallSprites.Length];
            }

            if (!HasWalls) return null;
            flipX = rightEdge;
            return leftWallSprites[Mathf.Abs(index) % leftWallSprites.Length];
        }

        /// <summary>Cracked variant for a segment, or null if the set has none; flipX as for WallSprite.</summary>
        public Sprite CrackedSprite(bool rightEdge, int index, out bool flipX)
        {
            flipX = false;
            if (rightEdge && crackedRightSprites != null && crackedRightSprites.Length > 0)
            {
                return crackedRightSprites[Mathf.Abs(index) % crackedRightSprites.Length];
            }

            if (crackedLeftSprites == null || crackedLeftSprites.Length == 0) return null;
            flipX = rightEdge;
            return crackedLeftSprites[Mathf.Abs(index) % crackedLeftSprites.Length];
        }

        /// <summary>Authoring helper: the cracked wall variants.</summary>
        public void ConfigureCracks(Sprite[] left, Sprite[] right)
        {
            crackedLeftSprites = left ?? new Sprite[0];
            crackedRightSprites = right ?? new Sprite[0];
        }

        /// <summary>Authoring helper for the tile-set generator.</summary>
        public void Configure(Sprite floor, TileBase tile, Sprite[] left, Sprite[] right, Sprite leftWindow, Sprite rightWindow, Color floorColor, Color wallColor)
        {
            floorSprite = floor;
            floorTile = tile;
            leftWallSprites = left ?? new Sprite[0];
            rightWallSprites = right ?? new Sprite[0];
            leftWindowSprite = leftWindow;
            rightWindowSprite = rightWindow;
            floorTint = floorColor;
            wallTint = wallColor;
        }
    }
}
