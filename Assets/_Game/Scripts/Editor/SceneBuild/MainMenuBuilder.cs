using System.Collections.Generic;
using System.IO;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Build Main Menu (re-runnable): Scenes/MainMenu.unity with a dark, slowly drifting blurred shot of
    /// the salon (Art/UI/MenuBackground.png when present; capture it in Play Mode with SixtyOne > Capture Menu
    /// Background), the "61 SECONDS" EB Garamond title with a thin rule, Başla / Ayarlar / Çıkış, the shared
    /// settings panel, prep music and click cues. Puts MainMenu first and Test_Building second in Build Settings.
    /// </summary>
    public static class MainMenuBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        public const string BackgroundPath = "Assets/_Game/Art/UI/MenuBackground.png";
        private const string AudioDir = "Assets/_Game/Data/Audio";

        [MenuItem("SixtyOne/Build Main Menu", priority = 12)]
        public static void Build()
        {
            // The generated test scene never needs a prompt (it is rebuilt from code): save it silently, so the
            // menu item also works from MCP without a modal dialog.
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path == TestSceneBuilder.ScenePath) EditorSceneManager.SaveScene(active);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PlaceholderSprites art = PlaceholderArt.Generate();
            UITheme theme = FontAssetsGenerator.Generate();
            var textSettings = AssetDatabase.LoadAssetAtPath<TextDBSettings>("Assets/_Game/Data/TextDBSettings.asset");
            AudioCue click = AssetDatabase.LoadAssetAtPath<AudioCue>($"{AudioDir}/cue_choice_click.asset");
            AudioCue music = AssetDatabase.LoadAssetAtPath<AudioCue>($"{AudioDir}/cue_music_prep.asset");

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camGo.AddComponent<AudioListener>();

            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var systems = new GameObject("Menu");
            TextDBLoader loader = systems.AddComponent<TextDBLoader>();
            SerializedWire.Set(loader, "settings", textSettings);
            AudioService audio = systems.AddComponent<AudioService>();
            SerializedWire.SetArray(audio, "library", new[] { click, music });

            UiFactory.Theme = theme;
            UiFactory.Rounded = art.Rounded;
            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            // Background: blurred (low-res, bilinear) salon shot, darkened, drifting slowly.
            Sprite bgSprite = ImportBackground();
            Image bg = UiFactory.MakeImage("Background", root, Vector2.zero, Vector2.one, bgSprite != null ? new Color(0.34f, 0.34f, 0.38f, 1f) : new Color(0.06f, 0.06f, 0.07f, 1f));
            if (bgSprite != null)
            {
                bg.sprite = bgSprite;
                bg.preserveAspect = false;
            }

            bg.gameObject.AddComponent<MenuDrift>();
            Image vignette = UiFactory.MakeImage("Shade", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f));
            vignette.sprite = art.Dot;

            // Title, rule and buttons hide together while the settings panel is open.
            RectTransform front = UiFactory.MakeRect("Front", root, Vector2.zero, Vector2.one);
            CanvasGroup buttons = front.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI title = MenuUi.KeyLabel("Title", front, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.86f), UiKeys.MenuTitle, 132f, TextAlignmentOptions.Center, true, true);
            title.characterSpacing = 6f;
            UiFactory.MakeImage("Rule", front, new Vector2(0.4f, 0.615f), new Vector2(0.6f, 0.618f), new Color(MenuUi.Ink.r, MenuUi.Ink.g, MenuUi.Ink.b, 0.7f));

            RectTransform column = UiFactory.MakeRect("Buttons", front, new Vector2(0.4f, 0.22f), new Vector2(0.6f, 0.55f));
            Button start = MenuUi.Button("Start", column, new Vector2(0f, 0.7f), new Vector2(1f, 0.96f), UiKeys.MenuStart, 40f);
            Button settings = MenuUi.Button("Settings", column, new Vector2(0f, 0.37f), new Vector2(1f, 0.63f), UiKeys.MenuSettings, 40f);
            Button quit = MenuUi.Button("Quit", column, new Vector2(0f, 0.04f), new Vector2(1f, 0.3f), UiKeys.MenuQuit, 40f);
            SettingsPanel settingsPanel = MenuUi.Settings(root, click);

            MainMenu menu = systems.AddComponent<MainMenu>();
            SerializedWire.Set(menu, "start", start);
            SerializedWire.Set(menu, "settings", settings);
            SerializedWire.Set(menu, "quit", quit);
            SerializedWire.Set(menu, "buttons", buttons);
            SerializedWire.Set(menu, "settingsPanel", settingsPanel);
            SerializedWire.Set(menu, "music", music);
            SerializedWire.Set(menu, "clickCue", click);
            UiFactory.Theme = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            Debug.Log("[MainMenu] Built " + ScenePath + (bgSprite == null ? " (no background shot yet: run SixtyOne > Capture Menu Background in Play Mode)." : "."));
        }

        /// <summary>MainMenu first, Test_Building second; other scenes keep their place after them.</summary>
        public static void SetBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(TestSceneBuilder.ScenePath, true)
            };
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                if (s.path != ScenePath && s.path != TestSceneBuilder.ScenePath) scenes.Add(s);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Play Mode: renders the current room (no UI) small, so the upscaled image reads blurred.</summary>
        [MenuItem("SixtyOne/Capture Menu Background", priority = 13)]
        public static void CaptureBackground()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[MainMenu] No main camera to capture.");
                return;
            }

            const int w = 192, h = 108;
            var rt = new RenderTexture(w, h, 24);
            RenderTexture previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = previous;
            RenderTexture.active = null;
            rt.Release();

            AssetUtil.EnsureFolder("Assets/_Game/Art/UI");
            File.WriteAllBytes(BackgroundPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(BackgroundPath);
            Debug.Log("[MainMenu] Captured " + BackgroundPath + ". Run SixtyOne > Build Main Menu (outside Play Mode).");
        }

        private static Sprite ImportBackground()
        {
            if (!(AssetImporter.GetAtPath(BackgroundPath) is TextureImporter importer)) return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled
                || importer.filterMode != FilterMode.Bilinear)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
        }
    }
}
