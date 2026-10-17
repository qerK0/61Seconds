using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
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

        [Tooltip("Floor cells that are never walkable (e.g. the street's downed power line). Debris is added at runtime.")]
        [SerializeField] private List<Vector2Int> blockedCells = new List<Vector2Int>();

        [Tooltip("How many floors below the top floor (0 = floor 4, 1 = floor 3, 2 = floor 2, 3 = street): more debris and cracks lower down.")]
        [Min(0)]
        [SerializeField] private int depth;

        private Anchor[] anchors;
        private GridNav nav;
        private readonly HashSet<Vector2Int> debris = new HashSet<Vector2Int>();
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

        /// <summary>Floors below the top floor (debris density and crack odds grow with it).</summary>
        public int Depth => depth;

        /// <summary>Static blocked cells (never walkable).</summary>
        public IReadOnlyList<Vector2Int> StaticBlocked => blockedCells;

        /// <summary>Debris cells currently lying in the room.</summary>
        public IReadOnlyCollection<Vector2Int> Debris => debris;

        /// <summary>The room's walkable grid (floor cells minus static blocks and debris).</summary>
        public GridNav Nav
        {
            get
            {
                if (nav == null)
                {
                    nav = new GridNav(floor.Width, floor.Height, floor.ExtraCells);
                    nav.SetBlocked(blockedCells);
                    nav.SetBlocked(debris);
                }

                return nav;
            }
        }

        /// <summary>Replaces the debris cells (DebrisPresenter); paths avoid them from now on.</summary>
        public void SetDebris(IEnumerable<Vector2Int> cells)
        {
            debris.Clear();
            if (cells != null) foreach (Vector2Int c in cells) debris.Add(c);
            nav = null;
        }

        /// <summary>Room-local grid point of a world point.</summary>
        public Vector2 WorldToGrid(Vector3 world) => IsoMath.WorldToGrid(transform.InverseTransformPoint(world));

        /// <summary>World point of a room-local grid point (z kept).</summary>
        public Vector3 GridToWorld(Vector2 grid, float z = 0f)
        {
            Vector3 world = transform.TransformPoint(IsoMath.GridToWorld(grid.x, grid.y));
            return new Vector3(world.x, world.y, z);
        }

        /// <summary>Grid cell under a world point.</summary>
        public Vector2Int CellOf(Vector3 world) => GridNav.CellOf(WorldToGrid(world));

        /// <summary>
        /// World waypoints from one point to another along the floor grid (around debris and blocked cells). A
        /// blocked or unreachable target goes to the nearest reachable free cell. The last waypoint is the target.
        /// Empty when already there.
        /// </summary>
        public List<Vector3> PlanPath(Vector3 fromWorld, Vector3 toWorld, float margin = 0.2f)
        {
            Vector3 to = ClampToFloor(toWorld, margin);
            List<Vector2> grid = Nav.Plan(WorldToGrid(fromWorld), WorldToGrid(to));
            var path = new List<Vector3>(grid.Count);
            foreach (Vector2 g in grid) path.Add(GridToWorld(g, fromWorld.z));
            return path;
        }

        /// <summary>
        /// Where to stand to use something: its approach point (clamped onto the floor), or the centre of the nearest
        /// free cell when debris covers it.
        /// </summary>
        public Vector3 ApproachTarget(Vector3 approachWorld, float margin = 0.2f)
        {
            Vector3 clamped = ClampToFloor(approachWorld, margin);
            Vector2Int cell = CellOf(clamped);
            if (Nav.IsWalkable(cell)) return clamped;
            return GridToWorld(GridNav.Center(Nav.NearestFree(cell)), approachWorld.z);
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
        public void ConfigureLayout(string zone, FloorArea area, int floorDepth = 0, IEnumerable<Vector2Int> blocked = null)
        {
            zoneId = zone;
            floor = area;
            depth = floorDepth;
            blockedCells = blocked != null ? new List<Vector2Int>(blocked) : new List<Vector2Int>();
            nav = null;
        }

        public void ResetProps()
        {
            foreach (HazardProp p in Props) p.ResetPose();
        }
    }
}
