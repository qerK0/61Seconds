using SixtyOne.Iso;
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
        private const float WallTopAboveEdge = (192f - 32f) / TileArt.PixelsPerUnit;

        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";

        /// <summary>URP 2D lit sprite material (so Light2D / the cinematic darkness applies).</summary>
        public static Material LitMaterial => AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        public static void Floor(Transform room, BuildingLayout.RoomSpec spec, RoomTileSet set)
        {
            var gridGo = new GameObject("Floor", typeof(Grid));
            gridGo.transform.SetParent(room, false);
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

        public static void Walls(Transform room, BuildingLayout.RoomSpec spec, RoomTileSet set)
        {
            if (!set.HasWalls) return;
            var root = new GameObject("Walls").transform;
            root.SetParent(room, false);

            // Back-left edge: grid y = Height, one segment per x. Back-right edge: grid x = Width, one per y.
            for (int x = 0; x < spec.Width; x++) Segment(root, spec, set, false, x, IsoMath.GridToWorld(x + 0.5f, spec.Height));
            for (int y = 0; y < spec.Height; y++) Segment(root, spec, set, true, y, IsoMath.GridToWorld(spec.Width, y + 0.5f));
        }

        private static void Segment(Transform parent, BuildingLayout.RoomSpec spec, RoomTileSet set, bool rightEdge, int index, Vector3 edgeMid)
        {
            string code = (rightEdge ? "R" : "L") + index;
            if (spec.Gaps.Contains(code)) return;

            bool window = spec.Windows.Contains(code);
            Sprite sprite = set.WallSprite(rightEdge, index, window, out bool flipX);
            if (sprite == null) return;

            var go = new GameObject("Wall_" + code);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = edgeMid - new Vector3(0f, WallBaseInset, 0f);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.flipX = flipX;
            r.sortingOrder = WallOrder;
            Material lit = LitMaterial;
            if (lit != null) r.sharedMaterial = lit;

            // Deterministic light/dark variation so a long wall does not read as one stamp.
            float shade = 1f - 0.04f * ((index * 7 + (rightEdge ? 3 : 0)) % 3);
            Color c = set.WallTint * shade;
            c.a = 1f;
            r.color = c;
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
        public static Rect ViewBounds(Transform room, BuildingLayout.RoomSpec spec)
        {
            float minX = -spec.Height * 0.5f;
            float maxX = spec.Width * 0.5f;
            float minY = 0f;
            float maxY = (spec.Width + spec.Height) * 0.25f + (spec.Walls ? WallTopAboveEdge : 0f);
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
