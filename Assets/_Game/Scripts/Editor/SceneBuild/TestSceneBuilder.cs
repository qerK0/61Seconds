using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Dialogue;
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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Build Test Scene. Builds Assets/_Game/Scenes/Test_Building.unity from code: ONE scene with
    /// every floor's rooms (RoomSwitcher shows one room at a time; floors are groups of rooms), the family,
    /// all systems and the full UI, wired to the generated building data. One scene keeps a single GameManager,
    /// GameState and UI, so checkpoints and rewinds need no scene loading.
    /// </summary>
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Test_Building.unity";
        private const string OldScenePath = "Assets/_Game/Scenes/Test_Act1.unity";

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
            if (AssetDatabase.LoadMainAssetAtPath(OldScenePath) != null) AssetDatabase.DeleteAsset(OldScenePath);

            BuildingAssets data = BuildingLevelsGenerator.Generate();
            SceneKit.Art = PlaceholderArt.Generate();
            CinematicAssets cinematic = CinematicAssetsGenerator.Generate(SceneKit.Art.Dot.texture);

            Camera camera = BuildCamera();
            BuildEventSystem();

            var systems = new GameObject("Game");
            GameManager gameManager = systems.AddComponent<GameManager>();
            SerializedWire.Set(gameManager, "settings", data.Settings);

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
            DialogueController dialogue = systems.AddComponent<DialogueController>();
            SurvivalController survival = systems.AddComponent<SurvivalController>();
            NodePlayer nodePlayer = systems.AddComponent<NodePlayer>();

            var familyRoot = new GameObject("Family").transform;
            var family = new List<FamilyMember>
            {
                SceneKit.Member(familyRoot, MemberType.Player, new Color(0.35f, 0.6f, 1f, 1f), 1f),
                SceneKit.Member(familyRoot, MemberType.Wife, new Color(1f, 0.5f, 0.75f, 1f), 0.95f),
                SceneKit.Member(familyRoot, MemberType.Child, new Color(1f, 0.85f, 0.3f, 1f), 0.65f)
            };

            var rooms = new List<Room>();
            var practicals = new List<Light2D>();
            foreach (BuildingLayout.RoomSpec spec in BuildingLayout.Rooms())
            {
                rooms.Add(BuildRoom(spec, data, practicals));
            }

            for (int i = 1; i < rooms.Count; i++) rooms[i].gameObject.SetActive(false);

            SerializedWire.Set(switcher, "gameManager", gameManager);
            SerializedWire.SetArray(switcher, "rooms", rooms.ToArray());
            SerializedWire.SetArray(switcher, "members", family.ToArray());

            SerializedWire.Set(placement, "gameManager", gameManager);
            SerializedWire.Set(placement, "rooms", switcher);

            SerializedWire.Set(survival, "gameManager", gameManager);
            SerializedWire.Set(survival, "nodePlayer", nodePlayer);
            SerializedWire.Set(survival, "config", data.Level);
            SerializedWire.Set(survival, "dialogue", dialogue);

            ChoicePanel choicePanel = TestSceneUi.Build(gameManager, nodePlayer, data.Content, camera, SceneKit.Art, survival, out LetterboxBars letterbox);
            TestSceneCinematic.Build(cinematic, camera, gameManager, nodePlayer, survival, letterbox, family[0].transform, practicals,
                BuildingLevelsGenerator.ExitNodeIds);

            SerializedWire.Set(dialogue, "gameManager", gameManager);
            SerializedWire.Set(dialogue, "nodePlayer", nodePlayer);
            SerializedWire.Set(dialogue, "presenter", choicePanel);

            SerializedWire.Set(nodePlayer, "gameManager", gameManager);
            SerializedWire.Set(nodePlayer, "content", data.Content);
            SerializedWire.Set(nodePlayer, "presenter", choicePanel);
            SerializedWire.Set(nodePlayer, "roomPresenter", switcher);
            SerializedWire.SetArray(nodePlayer, "nodeHandlers", new Object[] { dialogue, placement });

            AssetUtil.EnsureFolder("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[TestScene] Built " + ScenePath + " with " + rooms.Count + " rooms. Press Play.");
        }

        private static Room BuildRoom(BuildingLayout.RoomSpec spec, BuildingAssets data, List<Light2D> practicals)
        {
            Room room = SceneKit.NewRoom("Room_" + spec.Id, spec.Id);
            Transform t = room.transform;

            for (int x = 0; x < spec.Width; x++)
            {
                for (int y = 0; y < spec.Height; y++)
                {
                    SceneKit.Tile(t, x + 0.5f, y + 0.5f, spec.Floor);
                }
            }

            foreach (Vector2 extra in spec.ExtraTiles)
            {
                SceneKit.Tile(t, extra.x, extra.y, spec.Floor * 0.8f);
            }

            var anchors = new Dictionary<string, Anchor>();
            foreach (BuildingLayout.AnchorSpec a in spec.Anchors)
            {
                Hazard hazard = a.HazardId != null ? data.Hazards[a.HazardId] : null;
                anchors[a.Id] = SceneKit.MakeAnchor(t, a.Id, a.Gx, a.Gy, a.Tag, a.Capacity, hazard, a.LabelKey);
            }

            foreach (BuildingLayout.AnchorSpec a in spec.Anchors)
            {
                var neighbours = new List<Anchor>();
                foreach (string id in a.Adjacent) neighbours.Add(anchors[id]);
                SceneKit.Link(anchors[a.Id], neighbours);
            }

            foreach (BuildingLayout.PropSpec p in spec.Props)
            {
                SceneKit.Prop(t, p.PropId, p.Name, p.Gx, p.Gy, p.Lift, p.Scale, p.Color, p.Topple, p.Drop);
            }

            foreach (BuildingLayout.ItemSpec i in spec.Items)
            {
                Item item = data.Items[i.ItemId];
                if (i.Hidden) HiddenItems.Build(t, i, item);
                else SceneKit.Interactable(t, i.Id, item, i.Gx, i.Gy, i.Cost, i.Surface, i.Borrowed, i.Color);
            }

            foreach (BuildingLayout.LightSpec l in spec.Lights)
            {
                practicals.Add(TestSceneCinematic.PracticalLight(t, l.Name, l.Gx, l.Gy, l.Lift, l.Color, l.Radius));
            }

            SceneKit.SetSpawns(room, spec.PlayerSpawn, spec.WifeSpawn, spec.ChildSpawn);
            return room;
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
    }
}
