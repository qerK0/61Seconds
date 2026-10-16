using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Build Test Scene. Generates Assets/_Game/Scenes/Test_Act1.unity entirely from code:
    /// camera, systems, EventSystem, full UI, placeholder isometric rooms and the family, wired to Act 1 data.
    /// </summary>
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Test_Act1.unity";

        private const int FloorOrder = -30000;
        private const int DecalOrder = -29000;
        private const int MarkerOrder = -28000;
        private const int LabelOrder = 30000;

        private static PlaceholderSprites art;

        [MenuItem("SixtyOne/Build Test Scene", priority = 11)]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                EditorUtility.DisplayDialog("TextMeshPro resources missing",
                    "Import them first: Window > TextMeshPro > Import TMP Essential Resources. Then run SixtyOne > Build Test Scene again.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // NewScene(Single) unloads unused assets, so open it before loading the data we wire in.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SampleAct1Assets data = SampleAct1Generator.Generate();
            art = PlaceholderArt.Generate();

            Camera camera = BuildCamera();
            BuildEventSystem();

            var systems = new GameObject("Game");
            GameManager gameManager = systems.AddComponent<GameManager>();

            TextDBLoader loader = systems.AddComponent<TextDBLoader>();
            SerializedWire.Set(loader, "settings", data.TextSettings);

            AudioService audio = systems.AddComponent<AudioService>();
            SerializedWire.SetArray(audio, "library", data.Cues.ToArray());
            AudioEventHooks hooks = systems.AddComponent<AudioEventHooks>();
            SerializedWire.Set(hooks, "map", data.AudioMap);
            SerializedWire.Set(hooks, "service", audio);

            IsoPicker picker = systems.AddComponent<IsoPicker>();
            SerializedWire.Set(picker, "worldCamera", camera);

            RoomSwitcher switcher = systems.AddComponent<RoomSwitcher>();
            PlacementController placement = systems.AddComponent<PlacementController>();
            NodePlayer nodePlayer = systems.AddComponent<NodePlayer>();

            List<FamilyMember> family = BuildFamily();
            var rooms = new List<Room>
            {
                BuildLivingRoom(data),
                BuildSimpleRoom("Room_Stairwell", SampleAct1Generator.Stairwell, 3, new Color(0.55f, 0.55f, 0.6f, 1f), stairs: true),
                BuildSimpleRoom("Room_Floor4", SampleAct1Generator.Floor4, 4, new Color(0.75f, 0.6f, 0.45f, 1f), stairs: false)
            };

            for (int i = 1; i < rooms.Count; i++) rooms[i].gameObject.SetActive(false);

            SerializedWire.Set(switcher, "gameManager", gameManager);
            SerializedWire.SetArray(switcher, "rooms", rooms.ToArray());
            SerializedWire.SetArray(switcher, "members", family.ToArray());

            SerializedWire.Set(placement, "gameManager", gameManager);
            SerializedWire.Set(placement, "rooms", switcher);

            ChoicePanel choicePanel = TestSceneUi.Build(gameManager, nodePlayer, data.Content, camera, art);

            SerializedWire.Set(nodePlayer, "gameManager", gameManager);
            SerializedWire.Set(nodePlayer, "content", data.Content);
            SerializedWire.Set(nodePlayer, "presenter", choicePanel);
            SerializedWire.Set(nodePlayer, "roomPresenter", switcher);
            SerializedWire.SetArray(nodePlayer, "nodeHandlers", new Object[] { placement });

            AssetUtil.EnsureFolder("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[TestScene] Built " + ScenePath + ". Press Play.");
        }

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(0.3f, 0.4f, -10f);
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.09f, 0.12f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            go.AddComponent<AudioListener>();
            go.AddComponent<CameraShake>();
            return camera;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static List<FamilyMember> BuildFamily()
        {
            var root = new GameObject("Family").transform;
            return new List<FamilyMember>
            {
                BuildMember(root, MemberType.Player, new Color(0.35f, 0.6f, 1f, 1f), 1f),
                BuildMember(root, MemberType.Wife, new Color(1f, 0.5f, 0.75f, 1f), 0.95f),
                BuildMember(root, MemberType.Child, new Color(1f, 0.85f, 0.3f, 1f), 0.65f)
            };
        }

        private static FamilyMember BuildMember(Transform parent, MemberType type, Color color, float scale)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;

            SpriteRenderer body = go.AddComponent<SpriteRenderer>();
            body.sprite = art.Character;
            body.color = color;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.36f, 0.74f);
            collider.offset = new Vector2(0f, 0.37f);

            var marker = new GameObject("Selection");
            marker.transform.SetParent(go.transform, false);
            SpriteRenderer ring = marker.AddComponent<SpriteRenderer>();
            ring.sprite = art.Ring;
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

        private static Room BuildLivingRoom(SampleAct1Assets data)
        {
            Room room = NewRoom("Room_LivingRoom", SampleAct1Generator.LivingRoom);
            Transform t = room.transform;
            var floorColor = new Color(0.78f, 0.74f, 0.66f, 1f);

            for (int x = 0; x < 6; x++)
            {
                for (int y = 0; y < 6; y++)
                {
                    Tile(t, x + 0.5f, y + 0.5f, floorColor);
                }
            }

            // Balcony slab outside the right wall.
            Tile(t, 6.5f, 0.5f, new Color(0.6f, 0.6f, 0.62f, 1f));
            Tile(t, 6.5f, 1.5f, new Color(0.6f, 0.6f, 0.62f, 1f));

            Dictionary<string, Hazard> hz = data.Hazards;
            Anchor table = MakeAnchor(t, "lr_table", 2.5f, 2.5f, AnchorTag.Safe, 2, null, "anchor.table");
            Anchor innerWall = MakeAnchor(t, "lr_inner_wall", 1.0f, 5.3f, AnchorTag.Safe, 1, null, "anchor.inner_wall");
            Anchor doorway = MakeAnchor(t, "lr_doorway", 4.0f, 5.4f, AnchorTag.Trap, 1, hz["hz.doorway"], "anchor.doorway");
            Anchor wardrobe = MakeAnchor(t, "lr_wardrobe", 5.3f, 4.0f, AnchorTag.Trap, 1, hz["hz.wardrobe"], "anchor.wardrobe");
            Anchor window = MakeAnchor(t, "lr_window", 5.3f, 1.6f, AnchorTag.Trap, 1, hz["hz.window"], "anchor.window");
            Anchor balcony = MakeAnchor(t, "lr_balcony", 6.5f, 1.0f, AnchorTag.Trap, 1, hz["hz.balcony"], "anchor.balcony");

            Link(table, innerWall, doorway, wardrobe, window);
            Link(innerWall, table, doorway);
            Link(doorway, innerWall, table);
            Link(wardrobe, table, window);
            Link(window, wardrobe, balcony, table);
            Link(balcony, window);

            // Props. Lifted props get an order offset so they sort by their floor position.
            Prop(t, null, "Table", 2.5f, 2.5f, -0.12f, new Vector2(1.5f, 0.45f), new Color(0.55f, 0.35f, 0.2f, 1f));
            Prop(t, "wardrobe", "Wardrobe", 5.85f, 4.0f, 0f, new Vector2(1.3f, 1.9f), new Color(0.4f, 0.25f, 0.15f, 1f), toppleAngle: 80f);
            Prop(t, "window", "Window", 5.95f, 1.6f, 0.35f, new Vector2(0.9f, 1.0f), new Color(0.6f, 0.85f, 1f, 0.9f));
            Prop(t, "door_frame", "DoorLintel", 4.0f, 5.95f, 1.0f, new Vector2(1.0f, 0.25f), new Color(0.35f, 0.22f, 0.12f, 1f), dropDistance: 0.9f);
            Prop(t, "balcony_rail", "BalconyRail", 6.95f, 1.0f, 0f, new Vector2(1.0f, 0.5f), new Color(0.5f, 0.5f, 0.55f, 1f), dropDistance: 0.6f);
            Prop(t, "ceiling_lamp", "CeilingLamp", 2.5f, 3.4f, 1.6f, new Vector2(0.5f, 0.35f), new Color(1f, 0.95f, 0.6f, 1f), dropDistance: 1.4f);

            // Pickups, only clickable during the post-quake exploration node.
            string[] exploreOnly = { SampleAct1Generator.NodeExplore };
            Pickup(t, data.Items["wrench"], 1.2f, 2.0f, new Color(0.7f, 0.7f, 0.75f, 1f), exploreOnly);
            Pickup(t, data.Items["gloves"], 3.6f, 1.0f, new Color(1f, 0.85f, 0.2f, 1f), exploreOnly);
            Pickup(t, data.Items["first_aid"], 4.4f, 3.0f, new Color(1f, 0.25f, 0.25f, 1f), exploreOnly);
            Pickup(t, data.Items["flashlight"], 1.8f, 4.2f, new Color(0.3f, 0.3f, 0.35f, 1f), exploreOnly);
            Pickup(t, data.Items["gold"], 3.0f, 4.4f, new Color(1f, 0.75f, 0f, 1f), exploreOnly);
            Pickup(t, data.Items["laptop"], 3.2f, 3.4f, new Color(0.2f, 0.2f, 0.25f, 1f), exploreOnly);

            SetSpawns(room, new Vector2(0.9f, 0.9f), new Vector2(1.5f, 0.5f), new Vector2(0.5f, 1.5f));
            return room;
        }

        private static Room BuildSimpleRoom(string objectName, string roomId, int size, Color floorColor, bool stairs)
        {
            Room room = NewRoom(objectName, roomId);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Tile(room.transform, x + 0.5f, y + 0.5f, floorColor);
                }
            }

            if (stairs)
            {
                for (int i = 0; i < 3; i++)
                {
                    Prop(room.transform, null, "Step" + i, 2.6f, 0.6f + i * 0.8f, i * 0.15f, new Vector2(0.9f, 0.3f + i * 0.15f), new Color(0.45f, 0.45f, 0.5f, 1f));
                }
            }

            float c = size * 0.5f;
            SetSpawns(room, new Vector2(c - 0.6f, c - 0.6f), new Vector2(c, c - 0.9f), new Vector2(c - 0.9f, c));
            return room;
        }

        private static Room NewRoom(string objectName, string roomId)
        {
            var go = new GameObject(objectName);
            Room room = go.AddComponent<Room>();
            SerializedWire.SetString(room, "roomId", roomId);
            return room;
        }

        private static void SetSpawns(Room room, Vector2 player, Vector2 wife, Vector2 child)
        {
            SerializedWire.Set(room, "playerSpawn", Point(room.transform, "Spawn_Player", player));
            SerializedWire.Set(room, "wifeSpawn", Point(room.transform, "Spawn_Wife", wife));
            SerializedWire.Set(room, "childSpawn", Point(room.transform, "Spawn_Child", child));
        }

        private static Transform Point(Transform parent, string pointName, Vector2 grid)
        {
            var go = new GameObject(pointName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(grid.x, grid.y);
            return go.transform;
        }

        private static void Tile(Transform parent, float gx, float gy, Color color)
        {
            var go = new GameObject($"Tile_{gx:0.#}_{gy:0.#}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = art.Tile;
            r.color = color;
            r.sortingOrder = FloorOrder;
        }

        private static Anchor MakeAnchor(Transform parent, string id, float gx, float gy, AnchorTag tag, int capacity, Hazard hazard, string labelKey)
        {
            var go = new GameObject("Anchor_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);

            SpriteRenderer decal = go.AddComponent<SpriteRenderer>();
            decal.sprite = art.AnchorDecal;
            decal.color = new Color(1f, 1f, 1f, 0.2f);
            decal.sortingOrder = DecalOrder;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
            collider.points = new[] { new Vector2(-0.5f, 0f), new Vector2(0f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, -0.25f) };

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
            label.fontSize = 2f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.4f, 0.35f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(2.2f, 0.4f);
            label.sortingOrder = LabelOrder;
            labelGo.SetActive(false);

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

        private static void Link(Anchor anchor, params Anchor[] neighbours)
        {
            SerializedWire.SetArray(anchor, "adjacent", neighbours);
        }

        private static void Prop(Transform parent, string propId, string objectName, float gx, float gy, float lift, Vector2 scale, Color color, float toppleAngle = -80f, float dropDistance = 0.8f)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = art.Block;
            r.color = color;

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            SerializedWire.SetInt(sorter, "offset", Mathf.RoundToInt(lift * 100f));
            sorter.Apply();

            if (string.IsNullOrEmpty(propId)) return;

            HazardProp prop = go.AddComponent<HazardProp>();
            SerializedWire.SetString(prop, "propId", propId);
            SerializedWire.SetFloat(prop, "toppleAngle", toppleAngle);
            SerializedWire.SetFloat(prop, "dropDistance", dropDistance);
        }

        private static void Pickup(Transform parent, Item item, float gx, float gy, Color color, string[] activeNodeIds)
        {
            var go = new GameObject("Pickup_" + item.Id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = art.Dot;
            r.color = color;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.19f;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            TextMeshPro label = labelGo.AddComponent<TextMeshPro>();
            label.fontSize = 1.6f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            label.sortingOrder = LabelOrder;
            LocalizedText localized = labelGo.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", item.NameKey.Key);

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            PickupItem pickup = go.AddComponent<PickupItem>();
            SerializedWire.Set(pickup, "item", item);
            SerializedWire.SetStringArray(pickup, "activeNodeIds", activeNodeIds);
        }
    }
}
