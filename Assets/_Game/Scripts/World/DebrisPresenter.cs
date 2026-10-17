using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// Debris and wall cracks that grow with every quake (GameState.QuakeCount). When a room is shown or a rewind
    /// lands, the room's debris (DebrisPlanner) and cracks (CrackRules) are applied instantly; right after a quake
    /// resolves the new debris drops in from above with a dust puff and a low furniture crash. Debris cells are
    /// handed to the room's GridNav, so paths go around them.
    /// </summary>
    public sealed class DebrisPresenter : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Current room.")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Debris density and crack chance per quake.")]
        [SerializeField] private LevelConfig config;

        [Tooltip("Debris sprites (enkaz 1 / enkaz 2); one is picked per cell, seeded, with a random flip.")]
        [SerializeField] private Sprite[] debrisSprites = new Sprite[0];

        [Tooltip("Used when no debris art is imported (a tinted block).")]
        [SerializeField] private Sprite fallbackSprite;

        [Tooltip("Small soft sprite for the dust puff.")]
        [SerializeField] private Sprite dustSprite;

        [Tooltip("Lit sprite material (so the post-quake darkness applies). Optional.")]
        [SerializeField] private Material spriteMaterial;

        [Tooltip("Played once (quietly) when new debris lands after a quake.")]
        [SerializeField] private AudioCue crashCue;

        [Tooltip("Volume scale of the crash.")]
        [Range(0f, 1f)]
        [SerializeField] private float crashVolume = 0.35f;

        [Tooltip("Room-local height new debris falls from.")]
        [SerializeField] private float dropHeight = 1.6f;

        [Tooltip("Seconds of the fall.")]
        [SerializeField] private float dropSeconds = 0.35f;

        [Tooltip("Dust sprites per landing.")]
        [SerializeField] private int dustPerPiece = 5;

        private sealed class RoomDebris
        {
            public Transform Root;
            public readonly Dictionary<Vector2Int, GameObject> Pieces = new Dictionary<Vector2Int, GameObject>();
        }

        private readonly Dictionary<Room, RoomDebris> byRoom = new Dictionary<Room, RoomDebris>();
        private readonly Dictionary<string, HashSet<Vector2Int>> planCache = new Dictionary<string, HashSet<Vector2Int>>();
        private readonly Dictionary<Room, HashSet<Vector2Int>> extras = new Dictionary<Room, HashSet<Vector2Int>>();

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        private void OnEnable()
        {
            if (rooms != null) rooms.RoomShown += OnRoomShown;
            GameEvents.Rewound += OnRewound;
            GameEvents.QuakeEnded += OnQuakeEnded;
        }

        private void OnDisable()
        {
            if (rooms != null) rooms.RoomShown -= OnRoomShown;
            GameEvents.Rewound -= OnRewound;
            GameEvents.QuakeEnded -= OnQuakeEnded;
        }

        private void OnRoomShown(Room room) => Refresh(room, false);
        private void OnRewound(string _)
        {
            extras.Clear();
            Refresh(rooms != null ? rooms.Current : null, false);
        }

        /// <summary>
        /// Scripted rubble (street façade drops) that blocks paths on top of the planned debris until the next
        /// rewind; cells that would cut a required path stay walkable (DebrisPlanner.SafeExtras). The caller draws it.
        /// </summary>
        public void SetExtraBlocked(Room room, IEnumerable<Vector2Int> cells)
        {
            if (room == null) return;
            extras[room] = cells != null ? new HashSet<Vector2Int>(cells) : new HashSet<Vector2Int>();
            Refresh(room, false);
        }
        private void OnQuakeEnded(QuakeInfo _) => Refresh(rooms != null ? rooms.Current : null, true);

        /// <summary>Applies the debris and cracks for the current quake count; animate drops new pieces in.</summary>
        public void Refresh(Room room, bool animate)
        {
            GameManager gm = Gm;
            if (room == null || gm == null || config == null) return;
            GameState state = gm.State;

            HashSet<Vector2Int> cells = PlanFor(room, state.QuakeCount, state.DebrisSeed);
            var blocked = new HashSet<Vector2Int>(cells);
            if (extras.TryGetValue(room, out HashSet<Vector2Int> extra) && extra.Count > 0)
            {
                foreach (Vector2Int c in DebrisPlanner.SafeExtras(room.BaseNav(), room.RequiredCells(), cells, extra)) blocked.Add(c);
            }

            room.SetDebris(blocked);
            Sync(room, cells, animate, state.DebrisSeed);

            WallCracks cracks = room.GetComponentInChildren<WallCracks>(true);
            if (cracks != null)
            {
                float chance = CrackRules.Chance(config.CrackChancePerQuake, state.QuakeCount, config.CrackDepthBonus, room.Depth);
                cracks.Apply(room.RoomId, state.DebrisSeed, chance);
            }
        }

        private HashSet<Vector2Int> PlanFor(Room room, int quakeCount, int seed)
        {
            string key = room.RoomId + "|" + quakeCount + "|" + seed;
            if (planCache.TryGetValue(key, out HashSet<Vector2Int> cached)) return cached;
            HashSet<Vector2Int> cells = DebrisPlanner.Plan(room.BaseNav(), room.RequiredCells(), room.RoomId, quakeCount, seed,
                config.DebrisDensityPerQuake, config.DebrisDepthBonus, room.Depth);
            planCache[key] = cells;
            return cells;
        }

        private void Sync(Room room, HashSet<Vector2Int> cells, bool animate, int seed)
        {
            RoomDebris debris = DebrisOf(room);

            // A rewind can take debris away again (the snapshot is from before the quake).
            var stale = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, GameObject> pair in debris.Pieces)
            {
                if (!cells.Contains(pair.Key)) stale.Add(pair.Key);
            }

            foreach (Vector2Int cell in stale)
            {
                if (debris.Pieces[cell] != null) Destroy(debris.Pieces[cell]);
                debris.Pieces.Remove(cell);
            }

            bool landed = false;
            var ordered = new List<Vector2Int>(cells);
            ordered.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            int index = 0;
            foreach (Vector2Int cell in ordered)
            {
                if (debris.Pieces.ContainsKey(cell)) continue;
                GameObject piece = Spawn(room, debris.Root, cell, seed);
                debris.Pieces[cell] = piece;
                if (animate && isActiveAndEnabled)
                {
                    StartCoroutine(Drop(piece.transform, index++ * 0.06f));
                    landed = true;
                }
            }

            if (landed && crashCue != null && AudioService.Instance != null) AudioService.Instance.Play(crashCue, crashVolume, 1f);
        }

        private RoomDebris DebrisOf(Room room)
        {
            if (byRoom.TryGetValue(room, out RoomDebris debris) && debris.Root != null) return debris;
            debris = new RoomDebris();
            var root = new GameObject("Debris");
            root.transform.SetParent(room.transform, false);
            debris.Root = root.transform;
            byRoom[room] = debris;
            return debris;
        }

        private GameObject Spawn(Room room, Transform parent, Vector2Int cell, int seed)
        {
            var go = new GameObject($"Debris_{cell.x}_{cell.y}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(cell.x + 0.5f, cell.y + 0.5f);

            int roll = DebrisPlanner.Hash(room.RoomId, $"debris:{cell.x},{cell.y}", seed);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            bool hasArt = debrisSprites != null && debrisSprites.Length > 0;
            r.sprite = hasArt ? debrisSprites[roll % debrisSprites.Length] : fallbackSprite;
            r.flipX = (roll / 7) % 2 == 1;
            r.color = hasArt ? Color.white : new Color(0.5f, 0.47f, 0.43f, 1f);
            if (!hasArt) go.transform.localScale = new Vector3(0.6f, 0.3f, 1f);
            if (spriteMaterial != null) r.sharedMaterial = spriteMaterial;

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();
            return go;
        }

        private IEnumerator Drop(Transform piece, float delay)
        {
            if (piece == null) yield break;
            Vector3 rest = piece.localPosition;
            piece.localPosition = rest + new Vector3(0f, dropHeight, 0f);
            SpriteRenderer r = piece.GetComponent<SpriteRenderer>();
            if (r != null) r.enabled = false;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (piece == null) yield break;
            if (r != null) r.enabled = true;
            yield return Tween.MoveLocal(piece, rest, dropSeconds, Ease.InQuad);
            if (piece == null) yield break;
            piece.localPosition = rest;
            IsoSorter sorter = piece.GetComponent<IsoSorter>();
            if (sorter != null) sorter.Apply();
            DustPuff(piece);
        }

        private void DustPuff(Transform piece)
        {
            if (dustSprite == null || dustPerPiece <= 0 || piece.parent == null) return;
            for (int i = 0; i < dustPerPiece; i++)
            {
                var go = new GameObject("Dust");
                go.transform.SetParent(piece.parent, false);
                go.transform.localPosition = piece.localPosition + new Vector3(0f, 0.05f, 0f);
                go.transform.localScale = Vector3.one * 0.12f;
                SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                r.sprite = dustSprite;
                r.color = new Color(0.78f, 0.74f, 0.68f, 0.7f);
                r.sortingOrder = IsoMath.SortingOrderForY(go.transform.position.y, 100f, 5);
                float angle = (i / (float)dustPerPiece) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                Vector3 drift = new Vector3(Mathf.Cos(angle) * 0.35f, Mathf.Sin(angle) * 0.17f + 0.08f, 0f);
                StartCoroutine(Puff(go, drift));
            }
        }

        private static IEnumerator Puff(GameObject go, Vector3 drift)
        {
            Transform t = go.transform;
            SpriteRenderer r = go.GetComponent<SpriteRenderer>();
            Vector3 start = t.localPosition;
            Color c = r.color;
            yield return Tween.Run(0.55f, k =>
            {
                if (t == null) return;
                t.localPosition = start + drift * k;
                t.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.32f, k);
                if (r != null) r.color = new Color(c.r, c.g, c.b, c.a * (1f - k));
            }, Ease.OutQuad);
            if (go != null) Destroy(go);
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(GameManager manager, RoomSwitcher switcher, LevelConfig level, Sprite[] sprites, Sprite fallback, Sprite dust, Material material, AudioCue crash)
        {
            gameManager = manager;
            rooms = switcher;
            config = level;
            debrisSprites = sprites ?? new Sprite[0];
            fallbackSprite = fallback;
            dustSprite = dust;
            spriteMaterial = material;
            crashCue = crash;
        }
    }
}
