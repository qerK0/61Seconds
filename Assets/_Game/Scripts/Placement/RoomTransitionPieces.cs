using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SixtyOne.Placement
{
    /// <summary>
    /// The moving parts of one room for the tile transition: one sprite proxy per floor tile (the Tilemap renders
    /// as a single chunk, so it is hidden while its proxies move), every wall segment, and every prop. Floor proxies
    /// are built once per room and reused; walls and props are collected per transition (dropped items come and go).
    /// Every piece remembers its rest pose and colours and is restored exactly afterwards.
    /// </summary>
    public sealed class RoomTransitionPieces
    {
        /// <summary>One moving part.</summary>
        public sealed class Piece
        {
            public Transform Transform;
            public TransitionLayer Layer;
            public float Depth01;
            public Vector3 RestLocal;
            public SpriteRenderer[] Sprites;
            public Color[] SpriteColors;
            public TMP_Text[] Texts;
            public float[] TextAlphas;
        }

        private readonly Room room;
        private readonly List<GameObject> floorProxies = new List<GameObject>();
        private readonly List<TilemapRenderer> hiddenMaps = new List<TilemapRenderer>();
        private bool proxiesBuilt;

        public List<Piece> Pieces { get; } = new List<Piece>();

        public RoomTransitionPieces(Room room)
        {
            this.room = room;
        }

        /// <summary>Collects the pieces, swaps the floor Tilemaps for their tile proxies and stores rest poses.</summary>
        public void Begin()
        {
            Pieces.Clear();
            if (room == null) return;
            BuildFloorProxies();

            foreach (GameObject proxy in floorProxies) if (proxy != null) proxy.SetActive(true);
            foreach (TilemapRenderer map in hiddenMaps) if (map != null) map.enabled = false;
            foreach (GameObject proxy in floorProxies) if (proxy != null) Add(proxy.transform, TransitionLayer.Floor);

            Transform root = room.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                TransitionLayerTag tag = child.GetComponent<TransitionLayerTag>();
                if (tag != null && tag.Layer == TransitionLayer.Floor) continue;
                if (child.GetComponent<Grid>() != null) continue;
                if (tag != null && tag.Layer == TransitionLayer.Walls)
                {
                    for (int w = 0; w < child.childCount; w++) Add(child.GetChild(w), TransitionLayer.Walls);
                    continue;
                }

                // Lights and empty points (spawns) have no renderers and stay put.
                if (child.GetComponentInChildren<SpriteRenderer>(true) == null && child.GetComponentInChildren<TMP_Text>(true) == null) continue;
                Add(child, TransitionLayer.Props);
            }

            AssignDepths();
        }

        /// <summary>Puts every piece back to rest and shows the real Tilemap again.</summary>
        public void Restore()
        {
            foreach (Piece piece in Pieces) Apply(piece, Vector3.zero, 1f);
            foreach (GameObject proxy in floorProxies) if (proxy != null) proxy.SetActive(false);
            foreach (TilemapRenderer map in hiddenMaps) if (map != null) map.enabled = true;
            Pieces.Clear();
        }

        /// <summary>Offsets a piece from its rest pose (room-local units) and multiplies its alpha.</summary>
        public static void Apply(Piece piece, Vector3 localOffset, float alpha)
        {
            if (piece.Transform == null) return;
            piece.Transform.localPosition = piece.RestLocal + localOffset;
            for (int i = 0; i < piece.Sprites.Length; i++)
            {
                if (piece.Sprites[i] == null) continue;
                Color c = piece.SpriteColors[i];
                c.a *= alpha;
                piece.Sprites[i].color = c;
            }

            for (int i = 0; i < piece.Texts.Length; i++)
            {
                if (piece.Texts[i] != null) piece.Texts[i].alpha = piece.TextAlphas[i] * alpha;
            }
        }

        private void Add(Transform t, TransitionLayer layer)
        {
            SpriteRenderer[] sprites = t.GetComponentsInChildren<SpriteRenderer>(true);
            var colors = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
            TMP_Text[] texts = t.GetComponentsInChildren<TMP_Text>(true);
            var alphas = new float[texts.Length];
            for (int i = 0; i < texts.Length; i++) alphas[i] = texts[i].alpha;
            Pieces.Add(new Piece { Transform = t, Layer = layer, RestLocal = t.localPosition, Sprites = sprites, SpriteColors = colors, Texts = texts, TextAlphas = alphas });
        }

        private void AssignDepths()
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            var ys = new float[Pieces.Count];
            for (int i = 0; i < Pieces.Count; i++)
            {
                ys[i] = room.transform.InverseTransformPoint(Pieces[i].Transform.position).y;
                minY = Mathf.Min(minY, ys[i]);
                maxY = Mathf.Max(maxY, ys[i]);
            }

            for (int i = 0; i < Pieces.Count; i++) Pieces[i].Depth01 = TileTransitionPlan.Depth01(ys[i], minY, maxY);
        }

        // One SpriteRenderer per floor tile, under the Tilemap so position, scale and PPU match the real tile.
        private void BuildFloorProxies()
        {
            if (proxiesBuilt) return;
            proxiesBuilt = true;
            foreach (Tilemap map in room.GetComponentsInChildren<Tilemap>(true))
            {
                TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
                if (renderer == null) continue;
                hiddenMaps.Add(renderer);
                foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                {
                    if (!map.HasTile(cell)) continue;
                    var go = new GameObject("TileProxy_" + cell.x + "_" + cell.y);
                    go.transform.SetParent(map.transform, false);
                    go.transform.localPosition = map.GetCellCenterLocal(cell);
                    SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                    r.sprite = map.GetSprite(cell);
                    r.color = map.GetColor(cell) * map.color;
                    r.sharedMaterial = renderer.sharedMaterial;
                    r.sortingLayerID = renderer.sortingLayerID;
                    r.sortingOrder = renderer.sortingOrder;
                    go.SetActive(false);
                    floorProxies.Add(go);
                }
            }
        }
    }
}
