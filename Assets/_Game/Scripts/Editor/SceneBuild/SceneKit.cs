using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
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

        public static GameObject Prop(Transform parent, string propId, string objectName, float gx, float gy, float lift, Vector2 scale, Color color,
            float toppleAngle = -80f, float dropDistance = 0.8f)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = Art.Block;
            r.color = color;
            // Elevator props (F2 corridor) use the elevator door art on their back-right wall.
            if (objectName == "Elevator" && DoorArt.Apply(r, DoorArt.Elevator, true))
            {
                Material lit = RoomTiles.LitMaterial;
                if (lit != null) r.sharedMaterial = lit;
            }

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            SerializedWire.SetInt(sorter, "offset", Mathf.RoundToInt(lift * 100f));
            sorter.Apply();

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
                box.size = new Vector2(0.62f, 1.15f);
                box.offset = new Vector2(0f, 0.5f);
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

        /// <summary>A person (capsule) or object (block) that plays a node choice; hover shows the choice label.</summary>
        private static CharacterArt.Frames sevimFrames;

        /// <summary>Sevim's idle frames, imported once per scene build.</summary>
        private static CharacterArt.Frames SevimFrames => sevimFrames != null && sevimFrames.Sprites.Length > 0 && sevimFrames.Sprites[0] != null
            ? sevimFrames
            : sevimFrames = CharacterArt.ImportSevim();

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
            CharacterArt.Frames idle = spec.Idle == "sevim" ? SevimFrames : null;
            if (spec.Person)
            {
                body.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
                height = 0.8f;
                if (idle != null && idle.Sprites.Length > 0)
                {
                    body.transform.localScale = Vector3.one * idle.BodyScale;
                    height = CharacterArt.PlayerHeight * CharacterArt.SevimHeightRatio;
                    body.AddComponent<SpriteLoopAnimator>().Configure(r, idle.Sprites, 9f);
                }

                var shadow = new GameObject("Shadow");
                shadow.transform.SetParent(go.transform, false);
                shadow.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
                SpriteRenderer s = shadow.AddComponent<SpriteRenderer>();
                s.sprite = Art.Shadow;
                s.color = new Color(1f, 1f, 1f, 0.5f);
                s.sortingOrder = DecalOrder;
            }
            else if (spec.Name == "Elevator" && DoorArt.Apply(r, DoorArt.Elevator, true))
            {
                // Every elevator stands against a back-right wall: flipped elevator door art.
                height = DoorArt.Height;
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
                return null;
            }

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(spec.Person ? 0.45f : spec.Scale.x * 0.5f, height);
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
            hotspot.ConfigureHotspot(spec.ChoiceKey, spec.HideWhenGone);
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
