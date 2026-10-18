using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using SixtyOne.UI;
using SixtyOne.World;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Scene-building primitives shared by the test scene: rooms, tiles, anchors, props, family, interactables.</summary>
    public static class SceneKit
    {
        public const int FloorOrder = -30000;
        public const int DecalOrder = -29000;
        public const int MarkerOrder = -28000;
        public const int LabelOrder = 30000;

        public static PlaceholderSprites Art { get; set; }

        public static Room NewRoom(string objectName, string roomId)
        {
            var go = new GameObject(objectName);
            Room room = go.AddComponent<Room>();
            SerializedWire.SetString(room, "roomId", roomId);
            return room;
        }

        public static void SetSpawns(Room room, Vector2 player, Vector2 wife, Vector2 child)
        {
            SerializedWire.Set(room, "playerSpawn", Point(room.transform, "Spawn_Player", player));
            SerializedWire.Set(room, "wifeSpawn", Point(room.transform, "Spawn_Wife", wife));
            SerializedWire.Set(room, "childSpawn", Point(room.transform, "Spawn_Child", child));
        }

        public static Transform Point(Transform parent, string pointName, Vector2 grid)
        {
            var go = new GameObject(pointName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(grid.x, grid.y);
            return go.transform;
        }

        public static void Tile(Transform parent, float gx, float gy, Color color)
        {
            var go = new GameObject($"Tile_{gx:0.#}_{gy:0.#}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Tile;
            r.color = color;
            r.sortingOrder = FloorOrder;
        }

        public static Anchor MakeAnchor(Transform parent, string id, float gx, float gy, AnchorTag tag, int capacity, Hazard hazard, string labelKey)
        {
            var go = new GameObject("Anchor_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);

            SpriteRenderer decal = go.AddComponent<SpriteRenderer>();
            decal.sprite = Art.AnchorDecal;
            decal.color = new Color(1f, 1f, 1f, 0.2f);
            decal.sortingOrder = DecalOrder;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
            collider.points = new[] { new Vector2(-0.5f, 0f), new Vector2(0f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, -0.25f) };

            TextMeshPro label = WorldLabel(go.transform, new Vector3(0f, 0.32f, 0f), 2f, new Color(1f, 0.4f, 0.35f, 1f), bold: true);
            label.gameObject.SetActive(false);

            Anchor anchor = go.AddComponent<Anchor>();
            SerializedWire.SetString(anchor, "id", id);
            SerializedWire.SetInt(anchor, "anchorTag", (int)tag);
            SerializedWire.SetInt(anchor, "capacity", capacity);
            SerializedWire.Set(anchor, "hazard", hazard);
            SerializedWire.SetTextKey(anchor, "labelKey", labelKey);
            SerializedWire.Set(anchor, "highlightRenderer", decal);
            SerializedWire.Set(anchor, "label", label);
            return anchor;
        }

        public static void Link(Anchor anchor, IEnumerable<Anchor> neighbours)
        {
            SerializedWire.SetArray(anchor, "adjacent", new List<Anchor>(neighbours).ToArray());
        }

        /// <summary>Prop art (SixtyOne > Art > Import Prop Art) used by Prop and the containers; null = coloured blocks.</summary>
        public static PropArtSet Props { get; set; }

        /// <summary>Speech-bubble accent per speaker id (name tab, row hover).</summary>
        public static Color SpeakerColor(string id)
        {
            switch (id)
            {
                case "sevim": return Hex(0xB0479B);
                case "cemil": return Hex(0xB07A12);
                case "robber": return Hex(0xB23A2E);
                case "wife": return Hex(0x2F6DB5);
                default: return Hex(0x333333);
            }
        }

        /// <summary>Speaker name key of an NPC idle id (null = it never talks, e.g. the dog).</summary>
        private static string SpeakerKey(string idle)
        {
            switch (idle)
            {
                case "sevim":
                case "cemil":
                case "robber":
                    return "npc." + idle + ".name";
                default:
                    return null;
            }
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        /// <summary>Bark played by the rabid dog when its QTE starts.</summary>
        public static AudioCue DogBark { get; set; }

        /// <summary>Fixtures that hang on a wall and keep their authored lift when they get real art.</summary>
        private static bool WallMounted(string objectName) => objectName == "FuseBox" || objectName == "WallLamp" || objectName == "Valve";

        /// <summary>The lift a prop really gets: mapped floor-standing art sits on the floor (lift 0).</summary>
        public static float EffectiveLift(string objectName, float lift)
        {
            PropArtSet set = Props != null ? Props : AssetDatabase.LoadAssetAtPath<PropArtSet>(PropArt.SetPath);
            bool mapped = set != null && (set.Find(objectName) != null || (objectName == "CeilingLamp" && set.ChandelierFrames.Length > 0));
            return mapped && !WallMounted(objectName) ? 0f : lift;
        }

        /// <summary>A code prop (BuildingLayout / layout overrides), flipped and tagged for the Layout Editor.</summary>
        public static GameObject Prop(Transform parent, BuildingLayout.PropSpec p)
        {
            GameObject go = Prop(parent, p.PropId, p.Name, p.Gx, p.Gy, p.Lift, p.Scale, p.Color, p.Topple, p.Drop);
            SpriteRenderer r = go.GetComponent<SpriteRenderer>();
            if (r != null && p.FlipX) r.flipX = !r.flipX;
            LayoutOverrides.Tag(go, p, EffectiveLift(p.Name, p.Lift));
            return go;
        }

        public static GameObject Prop(Transform parent, string propId, string objectName, float gx, float gy, float lift, Vector2 scale, Color color,
            float toppleAngle = -80f, float dropDistance = 0.8f)
        {
            // Mapped art (bottom-centre pivot, sized by its import) replaces the block; the chandelier gets its frames.
            Sprite mapped = Props != null ? Props.Find(objectName) : null;
            bool chandelier = objectName == "CeilingLamp" && Props != null && Props.ChandelierFrames.Length > 0;
            if (chandelier) mapped = Props.ChandelierFrames[0];
            if (mapped != null)
            {
                if (!WallMounted(objectName)) lift = 0f;
                scale = Vector2.one;
            }

            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = mapped != null ? mapped : Art.Block;
            r.color = mapped != null ? Color.white : color;
            if (mapped != null && RoomTiles.LitMaterial != null) r.sharedMaterial = RoomTiles.LitMaterial;
            if (chandelier) go.AddComponent<PropFrames>().Configure(r, Props.ChandelierFrames, 10f);
            // Elevator props (F2 corridor) use the elevator door art on their back-right wall.
            if (objectName == "Elevator" && DoorArt.Apply(r, DoorArt.Elevator, true))
            {
                Material lit = RoomTiles.LitMaterial;
                if (lit != null) r.sharedMaterial = lit;
            }

            if (mapped != null && PropArt.IsFloorDecal(objectName))
            {
                // Floor decal (the Flat 10 hole): on the floor layer, under anchors, characters and props.
                r.sortingOrder = DecalOrder - 100;
            }
            else
            {
                IsoSorter sorter = go.AddComponent<IsoSorter>();
                sorter.Mode = IsoSortMode.Static;
                SerializedWire.SetInt(sorter, "offset", Mathf.RoundToInt(lift * 100f));
                sorter.Apply();
            }

            if (!string.IsNullOrEmpty(propId))
            {
                HazardProp prop = go.AddComponent<HazardProp>();
                SerializedWire.SetString(prop, "propId", propId);
                SerializedWire.SetFloat(prop, "toppleAngle", toppleAngle);
                SerializedWire.SetFloat(prop, "dropDistance", dropDistance);
            }

            return go;
        }

        /// <summary>World-space TMP label using the shared readable material.</summary>
        public static TextMeshPro WorldLabel(Transform parent, Vector3 localPosition, float fontSize, Color color, bool bold = false)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = localPosition;
            TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            if (bold) label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(2.4f, 0.35f);
            label.sortingOrder = LabelOrder;
            WorldLabelStyle.Apply(label);
            return label;
        }

        /// <summary>A pickup (open surface) or a furniture interactable (drawer/cabinet) granting an item.</summary>
        public static WorldInteractable Interactable(Transform parent, string id, Item grants, float gx, float gy, float cost, bool surface, bool borrowed, Color color,
            bool prepOnly = false)
        {
            var go = new GameObject("Item_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.color = color;
            if (surface && grants != null && grants.Icon != null)
            {
                // Real item art lying on the surface (white = no tint), clickable over the whole icon.
                r.sprite = grants.Icon;
                r.color = Color.white;
                go.AddComponent<CircleCollider2D>().radius = ItemIconArt.WorldSize * 0.5f;
            }
            else if (surface)
            {
                r.sprite = Art.Dot;
                go.AddComponent<CircleCollider2D>().radius = 0.19f;
            }
            else
            {
                r.sprite = Art.Block;
                go.transform.localScale = new Vector3(0.7f, 0.55f, 1f);
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.5f, 1f);
                box.offset = new Vector2(0f, 0.5f);
            }

            TextMeshPro label = WorldLabel(go.transform, new Vector3(0f, surface ? 0.3f : 1.2f, 0f), 1.4f, Color.white);
            if (!surface) label.transform.localScale = new Vector3(1f / 0.7f, 1f / 0.55f, 1f);
            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            string labelKey = grants != null ? grants.NameKey.Key : "world." + id;
            SerializedWire.SetTextKey(localized, "key", labelKey);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            WorldInteractable interactable = surface ? go.AddComponent<PickupItem>() : go.AddComponent<WorldInteractable>();
            interactable.Configure(id, labelKey, cost, grants, null, default(TextKey), true, surface, null, r, borrowed, 0f, prepOnly);
            EditorUtility.SetDirty(interactable);
            return interactable;
        }

        /// <summary>
        /// A searchable piece of furniture (LootContainer): its prop art when one is mapped (bottom-centre pivot),
        /// else a tinted block; collider over the body, the container name above it, the approach point in front.
        /// </summary>
        public static LootContainer Container(Transform parent, BuildingLayout.RoomSpec room, BuildingLayout.ContainerSpec spec, IEnumerable<Item> items,
            IEnumerable<Item> hidden, Sprite art, AudioCue searchCue, GameContent content)
        {
            var go = new GameObject("Container_" + spec.Id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(spec.Gx, spec.Gy);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            SpriteRenderer r = bodyGo.AddComponent<SpriteRenderer>();
            float height;
            float width;
            if (art != null)
            {
                r.sprite = art;
                r.color = Color.white;
                height = art.bounds.size.y;
                width = art.bounds.size.x;
            }
            else
            {
                r.sprite = Art.Block;
                r.color = spec.Color;
                bodyGo.transform.localScale = new Vector3(spec.Scale.x, spec.Scale.y, 1f);
                height = spec.Scale.y;
                width = spec.Scale.x * 0.5f;
            }

            Material lit = RoomTiles.LitMaterial;
            if (lit != null) r.sharedMaterial = lit;

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(Mathf.Max(0.35f, width * 0.8f), Mathf.Max(0.3f, height));
            box.offset = new Vector2(0f, box.size.y * 0.5f);

            TextMeshPro label = WorldLabel(go.transform, new Vector3(0f, height + 0.15f, 0f), 1.4f, Color.white);
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", spec.NameKey);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            SerializedWire.SetBool(sorter, "includeChildren", true);
            sorter.Apply();

            Vector2 stand = BuildingLayout.ContainerApproach(room, spec);
            var approach = new GameObject("Approach").transform;
            approach.SetParent(parent, false);
            approach.localPosition = IsoMath.GridToWorld(stand.x, stand.y);
            approach.SetParent(go.transform, true);

            LootContainer container = go.AddComponent<LootContainer>();
            container.Configure(spec.Id, spec.NameKey, spec.Seconds, spec.Cost, items, hidden, spec.Borrowed, spec.PrepOnly, approach, r, searchCue, content);
            EditorUtility.SetDirty(container);
            return container;
        }

        /// <summary>
        /// The street's downed power line (StreetLayout): a dark cable over its blocked cells with one gap, a click
        /// collider over it (electrocution), flickering sparks on the cut end, and the assembly-area sign label.
        /// </summary>
        public static StreetPowerLine StreetExtras(Transform street)
        {
            var root = new GameObject("PowerLine");
            root.transform.SetParent(street, false);
            foreach (Vector2Int c in StreetLayout.PowerLine)
            {
                var seg = new GameObject($"Cable_{c.x}_{c.y}");
                seg.transform.SetParent(root.transform, false);
                seg.transform.localPosition = IsoMath.GridToWorld(c.x + 0.5f, c.y + 0.5f);
                seg.transform.localScale = new Vector3(0.9f, 0.08f, 1f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, -26.6f);
                SpriteRenderer r = seg.AddComponent<SpriteRenderer>();
                r.sprite = Art.Block;
                r.color = new Color(0.08f, 0.08f, 0.09f, 1f);
                r.sortingOrder = DecalOrder + 10;
                PolygonCollider2D poly = seg.AddComponent<PolygonCollider2D>();
                poly.points = new[] { new Vector2(-0.5f, -2f), new Vector2(0.5f, -2f), new Vector2(0.5f, 2f), new Vector2(-0.5f, 2f) };
            }

            var sparks = new List<SpriteRenderer>();
            foreach (Vector2Int c in StreetLayout.SparkCells)
            {
                var spark = new GameObject($"Spark_{c.x}_{c.y}");
                spark.transform.SetParent(root.transform, false);
                spark.transform.localPosition = IsoMath.GridToWorld(c.x + 0.5f, c.y + 0.5f) + new Vector3(0f, 0.08f, 0f);
                spark.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
                SpriteRenderer r = spark.AddComponent<SpriteRenderer>();
                r.sprite = Art.Dot;
                r.color = new Color(1f, 0.95f, 0.45f, 1f);
                r.sortingOrder = LabelOrder - 10;
                sparks.Add(r);
            }

            StreetPowerLine line = root.AddComponent<StreetPowerLine>();
            line.Configure(UiKeys.StreetPowerDeath, "tip.electric", sparks.ToArray());
            EditorUtility.SetDirty(line);

            Vector2Int sign = StreetLayout.SignCell;
            TextMeshPro label = WorldLabel(street, IsoMath.GridToWorld(sign.x + 1.0f, sign.y + 1.0f) + new Vector3(0f, 1.75f, 0f), 1.8f, new Color(0.6f, 1f, 0.7f, 1f), bold: true);
            label.rectTransform.sizeDelta = new Vector2(3.2f, 0.4f);
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", "street.sign");
            return line;
        }

        /// <summary>A balcony/window rope point: needs the bedsheet rope, climbs down to the target node.</summary>
        public static RopeDescentInteractable RopeDescent(Transform parent, string id, float gx, float gy, Item rope, Item familyItem, SceneNode target, NodePlayer nodePlayer)
        {
            var go = new GameObject("Rope_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Block;
            r.color = new Color(0.85f, 0.85f, 0.95f, 0.8f);
            go.transform.localScale = new Vector3(0.35f, 0.9f, 1f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            box.offset = new Vector2(0f, 0.5f);

            TextMeshPro label = WorldLabel(go.transform, new Vector3(0f, 1.3f, 0f), 1.4f, Color.white);
            label.transform.localScale = new Vector3(1f / 0.35f, 1f / 0.9f, 1f);
            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.RopeDescentLabel);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            RopeDescentInteractable descent = go.AddComponent<RopeDescentInteractable>();
            descent.Configure(id, UiKeys.RopeDescentLabel, rope, UiKeys.RopeDescentMissing, target, nodePlayer, 10f, 20f);
            descent.ConfigureFamilyRule(familyItem, UiKeys.RopeDescentLocked, r);
            EditorUtility.SetDirty(descent);
            return descent;
        }

        /// <summary>A door on a wall gap or a floor exit: floor mat (hover highlight), collider, hover arrow + room name.</summary>
        public static Door Door(Transform room, BuildingLayout.RoomSpec spec, BuildingLayout.DoorSpec door, Transform arrival, RoomNavigator navigator)
        {
            bool wall = BuildingLayout.IsWallEdge(door.Edge);
            Vector2 center = BuildingLayout.EdgeCenter(spec, door.Edge);
            Vector2 inside = BuildingLayout.EdgeInside(spec, door.Edge);

            var go = new GameObject($"Door_{door.Edge}_to_{door.TargetRoom}");
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(center.x, center.y);

            // Threshold mat just inside the door: the hover highlight.
            var mat = new GameObject("Mat");
            mat.transform.SetParent(room, false);
            mat.transform.localPosition = IsoMath.GridToWorld(inside.x, inside.y);
            mat.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            mat.transform.SetParent(go.transform, true);
            SpriteRenderer matRenderer = mat.AddComponent<SpriteRenderer>();
            matRenderer.sprite = Art.Tile;
            matRenderer.color = new Color(1f, 0.82f, 0.45f, 0.32f);
            matRenderer.sortingOrder = DecalOrder;

            if (wall)
            {
                // Door art flush in the gap (sorted with the wall, below props and characters); the art faces the
                // back-left wall, so right-wall gaps flip it. The collider covers it from floor to lintel.
                var art = new GameObject("Art");
                art.transform.SetParent(go.transform, false);
                SpriteRenderer artRenderer = art.AddComponent<SpriteRenderer>();
                if (DoorArt.Apply(artRenderer, DoorArt.Door, door.Edge.StartsWith("R")))
                {
                    artRenderer.sortingOrder = RoomTiles.WallOrder + 1;
                    Material litDoor = RoomTiles.LitMaterial;
                    if (litDoor != null) artRenderer.sharedMaterial = litDoor;
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(art);
                }

                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = DoorArt.ColliderSize;
                box.offset = DoorArt.ColliderOffset;
            }
            else
            {
                PolygonCollider2D poly = go.AddComponent<PolygonCollider2D>();
                Vector3 d = IsoMath.GridToWorld(inside.x, inside.y) - IsoMath.GridToWorld(center.x, center.y);
                poly.points = new[]
                {
                    new Vector2(-0.45f, 0f) + (Vector2)d * 0.5f, new Vector2(0f, 0.25f) + (Vector2)d * 0.5f,
                    new Vector2(0.45f, 0f) + (Vector2)d * 0.5f, new Vector2(0f, -0.25f) + (Vector2)d * 0.5f
                };
            }

            GameObject hover = HoverMarker(go.transform, new Vector3(0f, wall ? 1.35f : 0.6f, 0f), UiKeys.RoomName(door.TargetRoom), out TextMeshPro label);

            var approach = new GameObject("Approach").transform;
            approach.SetParent(room, false);
            approach.localPosition = IsoMath.GridToWorld(inside.x, inside.y);
            approach.SetParent(go.transform, true);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            Door component = go.AddComponent<Door>();
            component.ConfigureLink(navigator, approach, matRenderer, hover, label);
            component.ConfigureDoor(door.TargetRoom, arrival);
            EditorUtility.SetDirty(component);
            return component;
        }

        private static readonly Dictionary<string, CharacterArt.Frames> idleFrames = new Dictionary<string, CharacterArt.Frames>();

        /// <summary>An NPC's idle frames (imported once per editor session), or null when it has no art yet.</summary>
        private static CharacterArt.Frames IdleFrames(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (!idleFrames.TryGetValue(id, out CharacterArt.Frames frames) || frames.Sprites.Length == 0 || frames.Sprites[0] == null)
            {
                frames = CharacterArt.ImportIdle(id);
                idleFrames[id] = frames;
            }

            return frames.Sprites.Length > 0 ? frames : null;
        }

        /// <summary>Neutral NPC silhouette tint: dark, with a hint of the spec colour (never the white placeholder).</summary>
        private static Color Silhouette(Color hint) => Color.Lerp(new Color(0.16f, 0.16f, 0.19f, 1f), new Color(hint.r, hint.g, hint.b, 1f), 0.2f);

        /// <summary>
        /// A person or object (block) that plays a node choice; hover shows the choice label. A person with idle art
        /// loops it; without art it is a dark neutral silhouette with a soft shadow.
        /// </summary>
        public static ChoiceHotspot Hotspot(Transform room, BuildingLayout.HotspotSpec spec, RoomNavigator navigator)
        {
            var go = new GameObject("Hotspot_" + spec.Name);
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(spec.Gx, spec.Gy);

            var body = new GameObject("Body");
            body.transform.SetParent(go.transform, false);
            SpriteRenderer r = body.AddComponent<SpriteRenderer>();
            r.sprite = spec.Person ? Art.Character : Art.Block;
            r.color = spec.Color;
            Material lit = RoomTiles.LitMaterial;
            if (lit != null) r.sharedMaterial = lit;
            float height;
            bool small = false;
            if (spec.Person)
            {
                float ratio = CharacterArt.NpcHeightRatio(spec.Idle);
                small = ratio < 0.6f;
                height = CharacterArt.PlayerHeight * ratio;
                CharacterArt.Frames idle = IdleFrames(spec.Idle);
                if (idle != null)
                {
                    r.color = Color.white;
                    body.transform.localScale = Vector3.one * idle.BodyScale;
                    body.AddComponent<SpriteLoopAnimator>().Configure(r, idle.Sprites, CharacterArt.NpcIdleFps);
                    if (spec.Face.HasValue)
                    {
                        // Face a grid point (the player's arrival, the corridor centre); flip art that looks the other way.
                        float dx = IsoMath.GridToWorld(spec.Face.Value.x, spec.Face.Value.y).x - IsoMath.GridToWorld(spec.Gx, spec.Gy).x;
                        if (Mathf.Abs(dx) > 0.01f) r.flipX = dx > 0f != CharacterArt.NpcFacesRight(spec.Idle);
                    }
                }
                else
                {
                    // No art yet: the capsule sprite (0.75u tall at scale 1) as a dark silhouette at the NPC's height;
                    // small NPCs (the dog) are wider than tall.
                    r.color = Silhouette(spec.Color);
                    float sy = height / 0.75f;
                    body.transform.localScale = new Vector3(small ? sy * 2.2f : 1.1f, sy, 1f);
                }

                // Gifts that do not fit in the bag land at Sevim's feet (front, camera side).
                if (spec.Idle == "sevim")
                {
                    var drop = new GameObject("DropSpot");
                    drop.transform.SetParent(go.transform, false);
                    drop.transform.localPosition = IsoMath.GridToWorld(0.35f, -0.45f);
                    drop.AddComponent<DropAnchor>();
                }

                // Speech bubbles attach to this NPC's head.
                string speaker = SpeakerKey(spec.Idle);
                if (speaker != null) go.AddComponent<SpeakerAnchor>().Configure(speaker, SpeakerColor(spec.Idle));

                var shadow = new GameObject("Shadow");
                shadow.transform.SetParent(go.transform, false);
                shadow.transform.localScale = small ? new Vector3(0.55f, 0.45f, 1f) : new Vector3(0.45f, 0.45f, 1f);
                SpriteRenderer s = shadow.AddComponent<SpriteRenderer>();
                s.sprite = Art.Shadow;
                s.color = new Color(1f, 1f, 1f, 0.5f);
                s.sortingOrder = DecalOrder;
            }
            else if (spec.Art != null && Props != null && Props.Find(spec.Art) != null)
            {
                // Real prop art (the gas valve): bottom-centre pivot, sized by its import.
                r.sprite = Props.Find(spec.Art);
                r.color = Color.white;
                height = Mathf.Max(0.3f, r.sprite.bounds.size.y);
            }
            else if (spec.Name == "Elevator" && DoorArt.Apply(r, DoorArt.Elevator, true))
            {
                // Every elevator stands flush in a back-right wall segment (layout puts it on the wall line):
                // flipped, wall-fitted elevator door art.
                height = DoorArt.Height + DoorArt.Rise;
            }
            else
            {
                body.transform.localScale = new Vector3(spec.Scale.x, spec.Scale.y, 1f);
                height = spec.Scale.y;
            }

            if (string.IsNullOrEmpty(spec.ChoiceKey))
            {
                // Decorative person: no click, no hover, no choice.
                IsoSorter decor = go.AddComponent<IsoSorter>();
                decor.Mode = IsoSortMode.Static;
                SerializedWire.SetBool(decor, "includeChildren", true);
                decor.Apply();
                // The rabid dog trembles in its QTE, backs off from the whistle and runs after the thrown food.
                if (spec.Idle == "dog") go.AddComponent<RabidDogPresenter>().Configure(body.transform, DogBark);
                return null;
            }

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            float boxWidth = spec.Person ? (small ? 0.6f : 0.45f) : r.sprite == DoorArt.Elevator ? DoorArt.Width : spec.Art != null && r.sprite != Art.Block ? Mathf.Max(0.3f, r.sprite.bounds.size.x) : spec.Scale.x * 0.5f;
            box.size = new Vector2(boxWidth, height);
            box.offset = new Vector2(0f, height * 0.5f);

            GameObject hover = HoverMarker(go.transform, new Vector3(0f, height + 0.3f, 0f), spec.ChoiceKey, out TextMeshPro label);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            SerializedWire.SetBool(sorter, "includeChildren", true);
            sorter.Apply();

            // Stand in front of (camera side of) the person or object, not inside it.
            var approach = new GameObject("Approach").transform;
            approach.SetParent(room, false);
            approach.localPosition = IsoMath.GridToWorld(spec.Gx - 0.5f, spec.Gy - 0.5f);
            approach.SetParent(go.transform, true);

            ChoiceHotspot hotspot = go.AddComponent<ChoiceHotspot>();
            hotspot.ConfigureLink(navigator, approach, r, hover, label);
            hotspot.ConfigureHotspot(spec.ChoiceKey, spec.HideWhenGone, spec.AlsoKeys);
            EditorUtility.SetDirty(hotspot);
            return hotspot;
        }

        /// <summary>Hidden-until-hover arrow + localized label above a link.</summary>
        private static GameObject HoverMarker(Transform parent, Vector3 localPosition, string labelKey, out TextMeshPro label)
        {
            var hover = new GameObject("Hover");
            hover.transform.SetParent(parent, false);
            hover.transform.localPosition = localPosition;
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(hover.transform, false);
            arrow.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            SpriteRenderer a = arrow.AddComponent<SpriteRenderer>();
            a.sprite = Art.Arrow;
            a.color = new Color(1f, 0.82f, 0.4f, 1f);
            a.sortingOrder = LabelOrder;
            label = WorldLabel(hover.transform, new Vector3(0f, 0.28f, 0f), 1.6f, Color.white, bold: true);
            label.rectTransform.sizeDelta = new Vector2(3f, 0.4f);
            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", labelKey);
            hover.SetActive(false);
            return hover;
        }

        /// <summary>An animated family member: shadow + body (run frames, idle breathing), motor, optional follower.</summary>
        public static FamilyMember AnimatedMember(Transform parent, MemberType type, CharacterArt.Frames frames, MovementConfig movement, SixtyOne.Building.SurvivalController survival)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(parent, false);
            go.AddComponent<UnityEngine.Rendering.SortingGroup>();

            float height = CharacterArt.PlayerHeight * CharacterArt.TargetHeightRatio(type);
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localScale = new Vector3(0.55f, 0.55f, 1f) * Mathf.Lerp(0.7f, 1f, height / CharacterArt.PlayerHeight);
            SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = Art.Shadow;
            shadowRenderer.color = new Color(1f, 1f, 1f, 0.55f);
            shadowRenderer.sortingOrder = -1;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            bodyGo.transform.localScale = Vector3.one * frames.BodyScale;
            SpriteRenderer body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = frames.Sprites.Length > 0 ? frames.Sprites[Mathf.Clamp(CharacterArt.IdleFrame(type), 0, frames.Sprites.Length - 1)] : Art.Character;
            Material lit = RoomTiles.LitMaterial;
            if (lit != null) body.sharedMaterial = lit;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(height * 0.42f, height);
            collider.offset = new Vector2(0f, height * 0.5f);

            var marker = new GameObject("Selection");
            marker.transform.SetParent(go.transform, false);
            SpriteRenderer ring = marker.AddComponent<SpriteRenderer>();
            ring.sprite = Art.Ring;
            ring.color = new Color(1f, 0.9f, 0.2f, 1f);
            ring.sortingOrder = -2;
            marker.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            marker.SetActive(false);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Dynamic;

            CharacterMotor motor = go.AddComponent<CharacterMotor>();
            CharacterAnimator animator = go.AddComponent<CharacterAnimator>();
            animator.Configure(body, frames.Sprites, CharacterArt.IdleFrame(type), CharacterArt.FacesRight(type), motor, movement, survival);
            // Drop-Cover-Hold during quakes: its own scale so frame 0 matches the in-game height.
            CharacterArt.Frames crouch = CharacterArt.ImportCrouch(type);
            if (crouch.Sprites.Length > 0) animator.ConfigureCrouch(crouch.Sprites, frames.BodyScale > 0f ? crouch.BodyScale / frames.BodyScale : 1f);

            FamilyMember member = go.AddComponent<FamilyMember>();
            SerializedWire.SetInt(member, "type", (int)type);
            SerializedWire.Set(member, "body", body);
            SerializedWire.Set(member, "selectionMarker", marker);
            EditorUtility.SetDirty(animator);
            return member;
        }

        public static FamilyMember Member(Transform parent, MemberType type, Color color, float scale)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer body = go.AddComponent<SpriteRenderer>();
            body.sprite = Art.Character;
            body.color = color;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.36f, 0.74f);
            collider.offset = new Vector2(0f, 0.37f);

            var marker = new GameObject("Selection");
            marker.transform.SetParent(go.transform, false);
            SpriteRenderer ring = marker.AddComponent<SpriteRenderer>();
            ring.sprite = Art.Ring;
            ring.color = new Color(1f, 0.9f, 0.2f, 1f);
            ring.sortingOrder = MarkerOrder;
            marker.transform.localScale = new Vector3(0.6f, 0.6f, 1f) / scale;
            marker.SetActive(false);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Dynamic;

            FamilyMember member = go.AddComponent<FamilyMember>();
            SerializedWire.SetInt(member, "type", (int)type);
            SerializedWire.Set(member, "body", body);
            SerializedWire.Set(member, "selectionMarker", marker);
            return member;
        }
    }
}
