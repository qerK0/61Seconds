using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using SixtyOne.Tutorial;
using SixtyOne.UI;
using SixtyOne.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Tutorial > Build Tutorial Scene: Scenes/Tutorial.unity from code, like Build Test Scene but from the
    /// tutorial's own layout (TutorialLayout; never RoomLayoutOverrides) and content (Data/Tutorial): a salon + hall
    /// flat, Mehmet alone, the real HUD (bag, Craft Defteri, LED clock, Panik, Enerji, loot window, speech bubbles,
    /// pause menu with "Öğreticiyi atla"), the cinematic look and the TutorialDirector. The building data is only read
    /// (TutorialAssets). Puts Tutorial.unity in Build Settings after MainMenu and Test_Building.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Tutorial.unity";

        [MenuItem("SixtyOne/Tutorial/Build Tutorial Scene", priority = 32)]
        public static void Build()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                Debug.LogError("[Tutorial] TextMeshPro resources missing: Window > TextMeshPro > Import TMP Essential Resources.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Tutorial] Leave Play Mode before building the scene.");
                return;
            }

            // Never save (or silently drop) other scenes' edits: Test_Building must stay exactly as it is on disk.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (open.isDirty && open.path != ScenePath)
                {
                    Debug.LogError($"[Tutorial] '{open.path}' has unsaved changes. Save or discard them, then build the tutorial again.");
                    return;
                }
            }

            BuildingAssets data = TutorialAssets.Load();
            if (data == null) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene unloads unused assets: load the data again for wiring.
            data = TutorialAssets.Load();
            GameContent content = TutorialContentGenerator.Generate();
            BrightnessConfigAsset.Ensure();
            SceneKit.Art = PlaceholderArt.Generate();
            SceneKit.Props = PropArt.Generate();
            TestSceneCraftNotebook.RustleCue = data.PaperCue;
            CinematicAssets cinematic = TutorialAssets.Cinematic();
            UITheme theme = FontAssetsGenerator.LoadTheme();

            Camera camera = TestSceneBuilder.BuildCamera(out Transform cameraRig);
            TestSceneBuilder.BuildEventSystem();

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
            RoomTransitionController transition = systems.AddComponent<RoomTransitionController>();
            DroppedItemsPresenter dropped = systems.AddComponent<DroppedItemsPresenter>();
            SurvivalController survival = systems.AddComponent<SurvivalController>();
            // No clock, drain, poison or panic in the tutorial (Awake still prepares the state).
            survival.enabled = false;
            NodePlayer nodePlayer = systems.AddComponent<NodePlayer>();
            RoomNavigator navigator = systems.AddComponent<RoomNavigator>();
            PlayerMover mover = systems.AddComponent<PlayerMover>();
            SerializedWire.Set(picker, "router", mover);

            // Mehmet alone.
            var familyRoot = new GameObject("Family").transform;
            FamilyMember player = SceneKit.AnimatedMember(familyRoot, MemberType.Player, CharacterArt.Import(MemberType.Player), data.Movement, survival);
            var family = new List<FamilyMember> { player };
            systems.AddComponent<CrouchDirector>().Configure(gameManager, nodePlayer, null, family);

            var rooms = new List<Room>();
            var practicals = new List<Light2D>();
            TileArt.TileSets tileSets = TileArt.Generate();
            List<BuildingLayout.RoomSpec> specs = TutorialLayout.Rooms();
            DoorArt.Configure(TestSceneBuilder.WallRows(data));
            foreach (BuildingLayout.RoomSpec spec in specs)
            {
                rooms.Add(TestSceneBuilder.BuildRoom(spec, data, practicals, tileSets, nodePlayer, navigator));
            }

            TestSceneBuilder.BuildDoors(specs, rooms, navigator);
            Transform marker = BuildMarker(rooms[0].transform, TutorialLayout.Marker, theme);
            // The tutorial flat is not part of the Layout Editor: no LayoutObject tags (they would key RoomLayoutOverrides).
            foreach (Room room in rooms)
            {
                foreach (LayoutObject tag in room.GetComponentsInChildren<LayoutObject>(true)) Object.DestroyImmediate(tag);
            }

            for (int i = 1; i < rooms.Count; i++) rooms[i].gameObject.SetActive(false);
            TestSceneBuilder.ApplyWorldScale(rooms, familyRoot, data.Visual != null ? data.Visual.WorldScale : 1f);

            SerializedWire.Set(switcher, "gameManager", gameManager);
            SerializedWire.SetArray(switcher, "rooms", rooms.ToArray());
            SerializedWire.SetArray(switcher, "members", new Object[] { player });
            SerializedWire.Set(switcher, "transition", transition);
            SerializedWire.Set(navigator, "gameManager", gameManager);
            SerializedWire.Set(navigator, "nodePlayer", nodePlayer);
            SerializedWire.Set(navigator, "rooms", switcher);
            SerializedWire.Set(navigator, "config", data.Movement);
            CharacterMotor motor = player.GetComponent<CharacterMotor>();
            SerializedWire.Set(mover, "gameManager", gameManager);
            SerializedWire.Set(mover, "player", motor);
            SerializedWire.Set(mover, "rooms", switcher);
            SerializedWire.Set(mover, "config", data.Movement);
            FootstepsAudio steps = systems.AddComponent<FootstepsAudio>();
            SerializedWire.Set(steps, "player", motor);
            SerializedWire.Set(steps, "footsteps", TutorialAssets.Cue(data, "cue_footsteps"));
            SerializedWire.Set(dropped, "gameManager", gameManager);
            SerializedWire.Set(dropped, "rooms", switcher);
            SerializedWire.Set(dropped, "content", content);
            SerializedWire.Set(dropped, "fallbackSprite", SceneKit.Art.Dot);

            SerializedWire.Set(survival, "gameManager", gameManager);
            SerializedWire.Set(survival, "nodePlayer", nodePlayer);
            SerializedWire.Set(survival, "config", data.Level);

            DialoguePresenter presenter = TestSceneUi.Build(gameManager, nodePlayer, content, camera, SceneKit.Art, survival, data.RecipeBook, null,
                data.Level.MaxInventorySlots, KeyItems(data), theme, out LetterboxBars letterbox, out _, out CanvasGroup transitionOverlay, tutorial: true);
            Transform canvas = presenter.transform.parent;

            SerializedWire.Set(transition, "config", data.Transition);
            SerializedWire.Set(transition, "overlay", transitionOverlay);
            SerializedWire.SetArray(transition, "characters", new Object[] { player.transform });
            CinematicDirector director = TestSceneCinematic.Build(cinematic, camera, gameManager, nodePlayer, survival, letterbox, player.transform, practicals, new string[0]);

            RoomCameraFraming framing = cameraRig.gameObject.AddComponent<RoomCameraFraming>();
            SerializedWire.Set(framing, "rooms", switcher);
            SerializedWire.Set(framing, "targetCamera", camera);
            SerializedWire.Set(framing, "director", director);
            SerializedWire.Set(framing, "followTarget", player.transform);
            SerializedWire.SetRect(framing, "freeViewport", new Rect(0f, 0.065f, TestSceneUi.ColumnLeft - 0.01f, 0.845f));
            framing.Frame(rooms[0]);

            SerializedWire.Set(presenter, "runner", motor);
            player.gameObject.AddComponent<SpeakerAnchor>().Configure(SpeakerAnchor.PlayerKey, SceneKit.SpeakerColor("player"));
            LootWindow lootWindow = canvas.GetComponentInChildren<LootWindow>(true);
            if (lootWindow != null) SerializedWire.Set(lootWindow, "runner", motor);

            SerializedWire.Set(nodePlayer, "gameManager", gameManager);
            SerializedWire.Set(nodePlayer, "content", content);
            SerializedWire.Set(nodePlayer, "presenter", presenter);
            SerializedWire.Set(nodePlayer, "roomPresenter", switcher);
            SerializedWire.Set(nodePlayer, "choiceFilter", navigator);
            // Every tutorial node is explorable (no placement / dialogue / QTE handlers).
            SerializedWire.SetArray(nodePlayer, "nodeHandlers", new Object[0]);

            // Tutorial UI over the HUD, under the pause menu.
            var click = TutorialAssets.Cue(data, "cue_choice_click");
            UiFactory.Theme = theme;
            UiFactory.Rounded = SceneKit.Art.Rounded;
            TutorialUi.Parts ui = TutorialUi.Build(canvas, camera, click);
            UiFactory.Theme = null;
            PauseMenu pause = canvas.GetComponentInChildren<PauseMenu>(true);
            if (pause != null) pause.transform.SetAsLastSibling();

            TutorialDirector tutorial = systems.AddComponent<TutorialDirector>();
            tutorial.ConfigureSystems(gameManager, nodePlayer, switcher, camera.GetComponent<CameraShake>(), survival, player.transform);
            tutorial.ConfigureUi(ui.Hint, ui.Highlight, pause, pause != null ? Find<Button>(pause.transform, "Panel/SkipTutorial") : null, ui.Complete, ui.Play, ui.Menu);
            tutorial.ConfigureTargets(marker, rooms[0].GetComponentInChildren<LootContainer>(true).transform,
                Find<RectTransform>(canvas, "LootWindow"),
                Find<RectTransform>(canvas, "RightColumn/Column/Bag/Backpack"),
                new[] { Find<RectTransform>(canvas, "RightColumn/Column/Bag/Backpack/PocketLeft"), Find<RectTransform>(canvas, "RightColumn/Column/Bag/Backpack/PocketRight") },
                Find<RectTransform>(canvas, "RightColumn/Column/Bag/NotebookButton"),
                Find<RectTransform>(canvas, "CraftNotebook"),
                Find<RectTransform>(canvas, "BuildingTimer"),
                Find<RectTransform>(canvas, "PanicMeter"),
                new[] { Find<RectTransform>(canvas, "RightColumn/Column/StaminaCaption"), Find<RectTransform>(canvas, "RightColumn/Column/Stamina") },
                rooms[0].GetComponentInChildren<Door>(true).transform,
                rooms[1].transform.Find("Hotspot_" + TutorialLayout.ExitName));
            tutorial.ConfigureBag(Find<CollapsiblePanel>(canvas, "RightColumn"));
            tutorial.ConfigureAudio(TutorialAssets.Cue(data, "cue_qte_success"), TutorialAssets.Cue(data, "cue_denied"), click, data.MusicPrep);
            EditorUtility.SetDirty(tutorial);

            AssetUtil.EnsureFolder("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            MainMenuBuilder.SetBuildScenes();
            Debug.Log("[Tutorial] Built " + ScenePath + " with " + rooms.Count + " rooms. Press Play.");
        }

        /// <summary>The walking step's glowing floor diamond (a decal under everything that stands on the floor).</summary>
        private static Transform BuildMarker(Transform salon, Vector2 grid, UITheme theme)
        {
            var go = new GameObject("TutorialMarker");
            go.transform.SetParent(salon, false);
            go.transform.localPosition = IsoMath.GridToWorld(grid.x, grid.y);
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            // The anchor ring (an outlined diamond), so it never reads as one of the floor rugs.
            r.sprite = SceneKit.Art.AnchorDecal;
            Color accent = theme != null ? theme.Accent : new Color(1f, 0.76f, 0.29f, 1f);
            r.color = new Color(accent.r, accent.g, accent.b, 0.9f);
            r.sortingOrder = SceneKit.DecalOrder + 1;
            go.AddComponent<TutorialMarker>().Configure(r);
            return go.transform;
        }

        private static Item[] KeyItems(BuildingAssets data)
        {
            var items = new List<Item>();
            foreach (string id in BuildingLevelsGenerator.KeyItemIds)
            {
                if (data.Items.TryGetValue(id, out Item item)) items.Add(item);
            }

            return items.ToArray();
        }

        private static T Find<T>(Transform root, string path) where T : Component
        {
            Transform t = root.Find(path);
            if (t == null) Debug.LogError("[Tutorial] UI object '" + path + "' not found.");
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
