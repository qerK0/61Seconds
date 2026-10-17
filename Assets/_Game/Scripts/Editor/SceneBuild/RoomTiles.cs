using SixtyOne.Iso;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Real tile art for a room: an isometric Tilemap floor over the room footprint and wall segments along the
    /// two back edges (one 128x192 segment per tile edge), plus the camera view bounds.
    /// </summary>
    public static class RoomTiles
    {
        /// <summary>Walls draw above the floor and decals' floor layer but below every prop and character (IsoSorter).</summary>
        public const int WallOrder = -29500;

        // A wall segment's bottom edge midpoint sits 32 px above the sprite bottom (128x192 art).
        private const float WallBaseInset = 32f / TileArt.PixelsPerUnit;
        // Sprite top above the edge (includes the 32 px slant of a 2:1 face).
        private const float WallSpriteTop = (192f - 32f) / TileArt.PixelsPerUnit;

        // Vertical face height in any one pixel column (192 px cell - 64 px slant): the step between stacked rows.
        private const float WallFaceHeight = (192f - 64f) / TileArt.PixelsPerUnit;

        // Rows overlap by 1 px so no seam line shows between stacked faces.
        private const float RowOverlap = 1f / TileArt.PixelsPerUnit;

        /// <summary>Vertical height of a stacked wall in any one pixel column (the face height doors are fitted to).</summary>
        public static float WallColumnHeight(int rows) => Mathf.Max(1, rows) * WallFaceHeight - (Mathf.Max(1, rows) - 1) * RowOverlap;

        /// <summary>Wall top above the floor edge for a wall stacked <paramref name="rows"/> faces high.</summary>
        public static float WallTopAboveEdge(int rows) => WallSpriteTop + (Mathf.Max(1, rows) - 1) * (WallFaceHeight - RowOverlap);

        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        /// <summary>URP 2D lit sprite material (so Light2D / the cinematic darkness applies).</summary>
        public static Material LitMaterial => AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        public static void Floor(Transform room, BuildingLayout.RoomSpec spec, RoomTileSet set)
        {
            var gridGo = new GameObject("Floor", typeof(Grid));
            gridGo.transform.SetParent(room, false);
            gridGo.AddComponent<TransitionLayerTag>().Configure(TransitionLayer.Floor);
            Grid grid = gridGo.GetComponent<Grid>();
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            var mapGo = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
            mapGo.transform.SetParent(gridGo.transform, false);
            Tilemap map = mapGo.GetComponent<Tilemap>();
            map.color = set.FloorTint;
            for (int x = 0; x < spec.Width; x++)
            {
                for (int y = 0; y < spec.Height; y++)
                {
                    map.SetTile(new Vector3Int(x, y, 0), set.FloorTile);
                }
            }

            foreach (Vector2 extra in spec.ExtraTiles)
            {
                var cell = new Vector3Int(Mathf.FloorToInt(extra.x), Mathf.FloorToInt(extra.y), 0);
                map.SetTile(cell, set.FloorTile);
                map.SetTileFlags(cell, TileFlags.None);
                map.SetColor(cell, new Color(0.75f, 0.75f, 0.78f, 1f));
            }

            TilemapRenderer renderer = mapGo.GetComponent<TilemapRenderer>();
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortingOrder = SceneKit.FloorOrder;
            Material lit = LitMaterial;
            if (lit != null) renderer.sharedMaterial = lit;
        }

        /// <summary>
        /// Stacks <paramref name="rows"/> wall faces per segment (each row exactly one face height above the last,
        /// minus a 1 px overlap). Windows go only in the top row; gaps stay open for the full height.
        /// </summary>
        public static void Walls(Transform room, BuildingLayout.RoomSpec spec, RoomTileSet set, int rows = 2)
        {
            if (!set.HasWalls) return;
            var root = new GameObject("Walls").transform;
            root.SetParent(room, false);
            root.gameObject.AddComponent<TransitionLayerTag>().Configure(TransitionLayer.Walls);
            WallCracks cracks = root.gameObject.AddComponent<WallCracks>();

            // Back-left edge: grid y = Height, one segment per x. Back-right edge: grid x = Width, one per y.
            rows = Mathf.Max(1, rows);
            for (int row = 0; row < rows; row++)
            {
                bool top = row == rows - 1;
                var lift = new Vector3(0f, row * (WallFaceHeight - RowOverlap), 0f);
                for (int x = 0; x < spec.Width; x++) Segment(root, spec, set, false, x, IsoMath.GridToWorld(x + 0.5f, spec.Height) + lift, row, top, cracks);
                for (int y = 0; y < spec.Height; y++) Segment(root, spec, set, true, y, IsoMath.GridToWorld(spec.Width, y + 0.5f) + lift, row, top, cracks);
            }
        }

        private static void Segment(Transform parent, BuildingLayout.RoomSpec spec, RoomTileSet set, bool rightEdge, int index, Vector3 edgeMid,
            int row, bool topRow, WallCracks cracks)
        {
            string code = (rightEdge ? "R" : "L") + index;
            if (spec.Gaps.Contains(code)) return;

            bool window = topRow && spec.Windows.Contains(code);
            // Upper rows shift the variant index so cracks do not stack straight above each other.
            Sprite sprite = set.WallSprite(rightEdge, index + row * 3, window, out bool flipX);
            if (sprite == null) return;

            var go = new GameObject(row == 0 ? "Wall_" + code : "Wall_" + code + "_r" + row);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = edgeMid - new Vector3(0f, WallBaseInset, 0f);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.flipX = flipX;
            r.sortingOrder = WallOrder;
            Material lit = LitMaterial;
            if (lit != null) r.sharedMaterial = lit;

            // Deterministic light/dark variation so a long wall does not read as one stamp.
            float shade = 1f - 0.04f * ((index * 7 + row * 5 + (rightEdge ? 3 : 0)) % 3);
            Color c = set.WallTint * shade;
            c.a = 1f;
            r.color = c;

            // Plain segments can crack after a quake (windows keep their glass look).
            if (!window)
            {
                Sprite cracked = set.CrackedSprite(rightEdge, index + row * 3 + (rightEdge ? 1 : 0), out bool crackedFlip);
                cracks.Add(code + "_r" + row, r, sprite, flipX, cracked, crackedFlip);
            }
        }

        /// <summary>
        /// Dim cool fill over the whole room. Not a practical (the director never kills it), so after the quake
        /// the real (dark) floor and wall textures stay faintly readable under the low global light.
        /// </summary>
        public static void AmbientFill(Transform room, BuildingLayout.RoomSpec spec, float intensity)
        {
            var go = new GameObject("Light_AmbientFill");
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(spec.Width * 0.5f, spec.Height * 0.5f) + new Vector3(0f, 0.4f, 0f);
            UnityEngine.Rendering.Universal.Light2D light = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = new Color(0.72f, 0.78f, 0.95f, 1f);
            light.intensity = intensity;
            float reach = Mathf.Max(spec.Width, spec.Height) * 0.75f + 0.5f;
            light.pointLightInnerRadius = reach * 0.5f;
            light.pointLightOuterRadius = reach;
            light.falloffIntensity = 0.4f;
        }

        /// <summary>Floor diamond (+ extra tiles) and wall height, united with every sprite in the room.</summary>
        public static Rect ViewBounds(Transform room, BuildingLayout.RoomSpec spec, int wallRows = 2)
        {
            float minX = -spec.Height * 0.5f;
            float maxX = spec.Width * 0.5f;
            float minY = 0f;
            float maxY = (spec.Width + spec.Height) * 0.25f + (spec.Walls ? WallTopAboveEdge(wallRows) : 0f);
            foreach (Vector2 extra in spec.ExtraTiles)
            {
                Vector3 c = IsoMath.GridToWorld(extra.x, extra.y);
                minX = Mathf.Min(minX, c.x - 0.5f);
                maxX = Mathf.Max(maxX, c.x + 0.5f);
                minY = Mathf.Min(minY, c.y - 0.25f);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(minX, minY, 0f) + room.position, new Vector3(maxX, maxY, 0f) + room.position);
            foreach (SpriteRenderer r in room.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.sprite != null) bounds.Encapsulate(r.bounds);
            }

            Vector3 min = bounds.min - room.position;
            Vector3 size = bounds.size;
            return new Rect(min.x, min.y, size.x, size.y);
        }
    }
}
