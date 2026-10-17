using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Dialogue;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.QTE;
using SixtyOne.Text;
using SixtyOne.UI;
using SixtyOne.World;
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
    /// GameState and UI, so checkpoints and rewinds need no scene loading. Room changes play the
    /// RoomTransitionController (old tiles rise, new tiles drop in). CameraRig (per-room framing) → Main Camera
    /// (shake/tilt), so framing and shake never fight. Rooms and the family sit under one World root scaled by
    /// RoomVisualConfig.WorldScale.
    /// </summary>
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Test_Building.unity";
        private const string OldScenePath = "Assets/_Game/Scenes/Test_Act1.unity";

        // Room fill light: keeps the real (dark) tile textures faintly visible after the quake kills the practicals.
        private const float AmbientFillIntensity = 0.6f;

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
            SixtyOne.UI.UITheme theme = FontAssetsGenerator.Generate();

            Camera camera = BuildCamera(out Transform cameraRig);
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
            MusicService music = systems.AddComponent<MusicService>();
            SerializedWire.Set(music, "gameManager", gameManager);
            SerializedWire.Set(music, "prepMusic", data.MusicPrep);
            SerializedWire.Set(music, "tensionMusic", data.MusicTension);
            SerializedWire.Set(music, "outroMusic", data.MusicOutro);
            AmbienceService ambience = systems.AddComponent<AmbienceService>();
            SerializedWire.Set(ambience, "gameManager", gameManager);
            SerializedWire.Set(ambience, "damagedBuilding", data.DripCue);
            SerializedWire.Set(ambience, "dogGrowl", data.DogGrowlCue);
            SerializedWire.Set(ambience, "dogBark", data.DogBarkCue);
            SerializedWire.Set(ambience, "heartbeat", data.HeartbeatCue);
            SerializedWire.SetString(ambience, "dogRoomId", BuildingLevelsGenerator.F2CorridorRoom);
            SerializedWire.SetString(ambience, "dogGoneFlag", BuildingLevelsGenerator.DogGone);

            IsoPicker picker = systems.AddComponent<IsoPicker>();
            SerializedWire.Set(picker, "worldCamera", camera);

            RoomSwitcher switcher = systems.AddComponent<RoomSwitcher>();
            PlacementController placement = systems.AddComponent<PlacementController>();
            DialogueController dialogue = systems.AddComponent<DialogueController>();
            QTEController qte = systems.AddComponent<QTEController>();
            RoomTransitionController transition = systems.AddComponent<RoomTransitionController>();
            DroppedItemsPresenter dropped = systems.AddComponent<DroppedItemsPresenter>();
            SurvivalController survival = systems.AddComponent<SurvivalController>();
            SixtyOne.Prep.PrepPhase prepPhase = systems.AddComponent<SixtyOne.Prep.PrepPhase>();
            EpilogueController epilogue = systems.AddComponent<EpilogueController>();
            NodePlayer nodePlayer = systems.AddComponent<NodePlayer>();
            RoomNavigator navigator = systems.AddComponent<RoomNavigator>();
            PlayerMover mover = systems.AddComponent<PlayerMover>();
            SerializedWire.Set(picker, "router", mover);

            var familyRoot = new GameObject("Family").transform;
            var family = new List<FamilyMember>();
            foreach (MemberType type in new[] { MemberType.Player, MemberType.Wife, MemberType.Child })
            {
                family.Add(SceneKit.AnimatedMember(familyRoot, type, CharacterArt.Import(type), data.Movement, survival));
            }

            var rooms = new List<Room>();
            var practicals = new List<Light2D>();
            TileArt.TileSets tileSets = TileArt.Generate();
            List<BuildingLayout.RoomSpec> specs = BuildingLayout.Rooms();
            DoorArt.Configure(WallRows(data));
            foreach (BuildingLayout.RoomSpec spec in specs)
            {
                rooms.Add(BuildRoom(spec, data, practicals, tileSets, nodePlayer, navigator));
            }

            BuildDoors(specs, rooms, navigator);

            for (int i = 1; i < rooms.Count; i++) rooms[i].gameObject.SetActive(false);

            // Room view bounds were measured at scale 1 (room-local); now scale everything together.
            ApplyWorldScale(rooms, familyRoot, data.Visual != null ? data.Visual.WorldScale : 1f);

            SerializedWire.Set(switcher, "gameManager", gameManager);
            SerializedWire.SetArray(switcher, "rooms", rooms.ToArray());
            SerializedWire.SetArray(switcher, "members", family.ToArray());
            SerializedWire.Set(switcher, "transition", transition);

            SerializedWire.Set(navigator, "gameManager", gameManager);
            SerializedWire.Set(navigator, "nodePlayer", nodePlayer);
            SerializedWire.Set(navigator, "rooms", switcher);
            SerializedWire.Set(navigator, "config", data.Movement);
            SerializedWire.Set(mover, "gameManager", gameManager);
            SerializedWire.Set(mover, "player", family[0].GetComponent<CharacterMotor>());
            SerializedWire.Set(mover, "rooms", switcher);
            SerializedWire.Set(mover, "config", data.Movement);
            FootstepsAudio steps = systems.AddComponent<FootstepsAudio>();
            SerializedWire.Set(steps, "player", family[0].GetComponent<CharacterMotor>());
            SerializedWire.Set(steps, "footsteps", data.Cues.Find(c => c != null && c.Id == "cue_footsteps"));
            for (int i = 1; i < family.Count; i++)
            {
                FamilyFollower follower = family[i].gameObject.AddComponent<FamilyFollower>();
                SerializedWire.Set(follower, "member", family[i]);
                SerializedWire.Set(follower, "motor", family[i].GetComponent<CharacterMotor>());
                SerializedWire.Set(follower, "leader", family[0].transform);
                SerializedWire.Set(follower, "rooms", switcher);
                SerializedWire.Set(follower, "config", data.Movement);
            }

            SerializedWire.Set(dropped, "gameManager", gameManager);
            SerializedWire.Set(dropped, "rooms", switcher);
            SerializedWire.Set(dropped, "content", data.Content);
            SerializedWire.Set(dropped, "fallbackSprite", SceneKit.Art.Dot);

            SerializedWire.Set(placement, "gameManager", gameManager);
            SerializedWire.Set(placement, "rooms", switcher);

            SerializedWire.Set(survival, "gameManager", gameManager);
            SerializedWire.Set(survival, "nodePlayer", nodePlayer);
            SerializedWire.Set(survival, "config", data.Level);
            SerializedWire.Set(survival, "dialogue", dialogue);
            SerializedWire.Set(survival, "prep", prepPhase);

            SerializedWire.Set(prepPhase, "gameManager", gameManager);
            SerializedWire.Set(prepPhase, "nodePlayer", nodePlayer);
            SerializedWire.Set(prepPhase, "config", data.Level);
            SerializedWire.Set(prepPhase, "rooms", switcher);
            SerializedWire.Set(prepPhase, "alertCue", data.Cues.Find(c => c != null && c.Id == "cue_afad_alert"));

            ChoicePanel choicePanel = TestSceneUi.Build(gameManager, nodePlayer, data.Content, camera, SceneKit.Art, survival, data.RecipeBook, prepPhase,
                data.Level.MaxInventorySlots, KeyItems(data), theme, out LetterboxBars letterbox, out QTEView qteView, out CanvasGroup transitionOverlay);
            // Voice blips follow the subtitle line (dialogue text lives there); the panel's own typewriter is the fallback.
            SixtyOne.UI.SubtitleView subtitles = choicePanel.transform.parent.GetComponentInChildren<SixtyOne.UI.SubtitleView>(true);
            SixtyOne.UI.TypewriterText typewriter = subtitles != null && subtitles.Typewriter != null
                ? subtitles.Typewriter
                : choicePanel.GetComponentInChildren<SixtyOne.UI.TypewriterText>(true);
            if (typewriter != null)
            {
                DialogueVoice voice = typewriter.gameObject.AddComponent<DialogueVoice>();
                SerializedWire.Set(voice, "typewriter", typewriter);
                SerializedWire.Set(voice, "profile", data.Voices);
            }

            SerializedWire.Set(qte, "gameManager", gameManager);
            SerializedWire.Set(qte, "view", qteView);
            SerializedWire.Set(qte, "cameraShake", camera.GetComponent<CameraShake>());
            SerializedWire.Set(transition, "config", data.Transition);
            SerializedWire.Set(transition, "overlay", transitionOverlay);
            var familyTransforms = new Object[family.Count];
            for (int i = 0; i < family.Count; i++) familyTransforms[i] = family[i].transform;
            SerializedWire.SetArray(transition, "characters", familyTransforms);
            CinematicDirector director = TestSceneCinematic.Build(cinematic, camera, gameManager, nodePlayer, survival, letterbox, family[0].transform, practicals,
                BuildingLevelsGenerator.ExitNodeIds);

            RoomCameraFraming framing = cameraRig.gameObject.AddComponent<RoomCameraFraming>();
            SerializedWire.Set(framing, "rooms", switcher);
            SerializedWire.Set(framing, "targetCamera", camera);
            SerializedWire.Set(framing, "director", director);
            SerializedWire.Set(choicePanel, "runner", family[0].GetComponent<CharacterMotor>());
            // Playfield = full screen left of the HUD column, above the panel peek strip, below the timer; the room fills 97% of it (RoomCameraFraming.fill).
            SerializedWire.SetRect(framing, "freeViewport", new Rect(0f, 0.065f, TestSceneUi.ColumnLeft - 0.01f, 0.845f));
            if (rooms.Count > 0) framing.Frame(rooms[0]);

            SerializedWire.Set(dialogue, "gameManager", gameManager);
            SerializedWire.Set(dialogue, "nodePlayer", nodePlayer);
            SerializedWire.Set(dialogue, "presenter", choicePanel);

            SerializedWire.Set(epilogue, "gameManager", gameManager);
            SerializedWire.Set(epilogue, "presenter", choicePanel);

            SerializedWire.Set(nodePlayer, "gameManager", gameManager);
            SerializedWire.Set(nodePlayer, "content", data.Content);
            SerializedWire.Set(nodePlayer, "presenter", choicePanel);
            SerializedWire.Set(nodePlayer, "roomPresenter", switcher);
            SerializedWire.Set(nodePlayer, "choiceFilter", navigator);
            SerializedWire.SetArray(nodePlayer, "nodeHandlers", new Object[] { dialogue, placement, qte, epilogue });

            AssetUtil.EnsureFolder("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[TestScene] Built " + ScenePath + " with " + rooms.Count + " rooms. Press Play.");
        }

        /// <summary>
        /// Puts every room and the family under one World root and scales it (RoomVisualConfig.WorldScale), so tiles,
        /// walls, props, anchors, hotspots, spawns and characters stay aligned and the tile PPU never changes.
        /// Light2D radii are world-space values, so they are scaled to match.
        /// </summary>
        private static void ApplyWorldScale(List<Room> rooms, Transform familyRoot, float scale)
        {
            var world = new GameObject("World").transform;
            foreach (Room room in rooms) room.transform.SetParent(world, false);
            familyRoot.SetParent(world, false);
            world.localScale = new Vector3(scale, scale, 1f);
            foreach (Light2D light in world.GetComponentsInChildren<Light2D>(true))
            {
                light.pointLightInnerRadius *= scale;
                light.pointLightOuterRadius *= scale;
            }
        }

        private static Item[] KeyItems(BuildingAssets data)
        {
            var items = new List<Item>();
            foreach (string id in BuildingLevelsGenerator.KeyItemIds) items.Add(data.Items[id]);
            return items.ToArray();
        }

        private static int WallRows(BuildingAssets data) => data.Visual != null ? data.Visual.WallRows : 2;

        private static Room BuildRoom(BuildingLayout.RoomSpec spec, BuildingAssets data, List<Light2D> practicals, TileArt.TileSets tileSets, NodePlayer nodePlayer,
            RoomNavigator navigator)
        {
            Room room = SceneKit.NewRoom("Room_" + spec.Id, spec.Id);
            Transform t = room.transform;
            var extraCells = new List<Vector2Int>();
            foreach (Vector2 extra in spec.ExtraTiles) extraCells.Add(new Vector2Int(Mathf.FloorToInt(extra.x), Mathf.FloorToInt(extra.y)));
            room.ConfigureLayout(spec.Zone, new FloorArea(spec.Width, spec.Height, extraCells));

            RoomTileSet tileSet = spec.Art == BuildingLayout.RoomArt.DamagedCorridor ? tileSets.DamagedCorridor
                : spec.Art == BuildingLayout.RoomArt.Corridor ? tileSets.Corridor : tileSets.Home;
            if (tileSet != null && tileSet.FloorTile != null)
            {
                RoomTiles.Floor(t, spec, tileSet);
                if (spec.Walls) RoomTiles.Walls(t, spec, tileSet, WallRows(data));
                RoomTiles.AmbientFill(t, spec, AmbientFillIntensity);
            }
            else
            {
                // No tile art at all: keep the placeholder diamonds so the scene still builds.
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
                else SceneKit.Interactable(t, i.Id, item, i.Gx, i.Gy, i.Cost, i.Surface, i.Borrowed, i.Color, i.PrepOnly);
            }

            foreach (BuildingLayout.RopeSpec rope in spec.Ropes)
            {
                SceneKit.RopeDescent(t, rope.Id, rope.Gx, rope.Gy, data.Items["bedsheet_rope"], data.Items["child_harness"], data.Nodes[rope.TargetNodeId], nodePlayer);
            }

            foreach (BuildingLayout.HotspotSpec h in spec.Hotspots)
            {
                SceneKit.Hotspot(t, h, navigator);
            }

            foreach (BuildingLayout.LightSpec l in spec.Lights)
            {
                practicals.Add(TestSceneCinematic.PracticalLight(t, l.Name, l.Gx, l.Gy, l.Lift, l.Color, l.Radius));
            }

            SceneKit.SetSpawns(room, spec.PlayerSpawn, spec.WifeSpawn, spec.ChildSpawn);
            SerializedWire.SetRect(room, "viewBounds", RoomTiles.ViewBounds(t, spec, WallRows(data)));
            return room;
        }

        /// <summary>Second pass (every room exists): doors with arrival points inside their target rooms.</summary>
        private static void BuildDoors(List<BuildingLayout.RoomSpec> specs, List<Room> rooms, RoomNavigator navigator)
        {
            for (int i = 0; i < specs.Count; i++)
            {
                foreach (BuildingLayout.DoorSpec door in specs[i].Doors)
                {
                    Room target = rooms.Find(r => r.RoomId == door.TargetRoom);
                    if (target == null)
                    {
                        Debug.LogError($"[TestScene] Door in '{specs[i].Id}' leads to unknown room '{door.TargetRoom}'.");
                        continue;
                    }

                    Transform arrival = SceneKit.Point(target.transform, "Arrive_from_" + specs[i].Id, door.Arrival);
                    SceneKit.Door(rooms[i].transform, specs[i], door, arrival, navigator);
                }
            }
        }

        private static Camera BuildCamera(out Transform rig)
        {
            // CameraRig moves per room (RoomCameraFraming); the camera keeps its local pose for shake and tilt.
            rig = new GameObject("CameraRig").transform;
            rig.position = new Vector3(0.3f, 0.4f, 0f);
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.SetParent(rig, false);
            go.transform.localPosition = new Vector3(0f, 0f, -10f);
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
