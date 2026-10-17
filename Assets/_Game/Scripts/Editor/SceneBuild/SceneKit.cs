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
            if (surface)
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
