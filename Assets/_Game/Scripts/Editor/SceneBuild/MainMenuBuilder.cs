using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
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
    /// SixtyOne > Build Main Menu (re-runnable): Scenes/MainMenu.unity, "İSTANBUL AFET BÖLGESİ".
    /// <list type="bullet">
    /// <item>The Istanbul map (Art/Menu/menu_bg.png) with a slow Ken Burns move, drifting smoke, pulsing alert pins,
    /// a seismograph trace, corner brackets and ticking monospace readouts, scanlines and a vignette.</item>
    /// <item>The distressed, glitching title and a left column: Oyunu Başlat (framed primary), Ayarlar / Galeri /
    /// Çıkış (red rows) and a scrolling hazard stripe.</item>
    /// <item>The restyled settings panel, the Galeri of Deprem Notları, the quit confirmation, the intro and the
    /// fader. Puts MainMenu first and Test_Building second in Build Settings.</item>
    /// </list>
    /// </summary>
    public static class MainMenuBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        public const string TitleMaterialPath = MenuArt.OutFolder + "/Oswald-Bold Title.mat";

        [MenuItem("SixtyOne/Build Main Menu", priority = 12)]
        public static void Build()
        {
            // The generated test scene never needs a prompt (it is rebuilt from code): save it silently, so the
            // menu item also works from MCP without a modal dialog.
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path == TestSceneBuilder.ScenePath) EditorSceneManager.SaveScene(active);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PlaceholderSprites placeholders = PlaceholderArt.Generate();
            UITheme theme = FontAssetsGenerator.Generate();
            MenuArt.Set art = MenuArt.Generate();
            MenuAudio.Cues cues = MenuAudio.Load();
            var textSettings = AssetDatabase.LoadAssetAtPath<TextDBSettings>("Assets/_Game/Data/TextDBSettings.asset");
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/_Game/Data/GameContent.asset");

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
            SerializedWire.SetArray(audio, "library", cues.All);

            UiFactory.Theme = theme;
            UiFactory.Rounded = placeholders.Rounded;
            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform canvas = canvasGo.transform;

            // Everything but the fader shakes together; the background bleeds 30 px past the edges so a shake
            // never shows black.
            RectTransform shake = UiFactory.MakeRect("Shake", canvas, Vector2.zero, Vector2.one);
            shake.gameObject.AddComponent<UiShake>();
            Image bg = MenuKit.Image("Background", shake, Vector2.zero, Vector2.one, art.MenuBackground, Color.white);
            bg.rectTransform.offsetMin = new Vector2(-30f, -30f);
            bg.rectTransform.offsetMax = new Vector2(30f, 30f);
            bg.gameObject.AddComponent<KenBurns>().Configure(new Vector2(1f, 1.06f), new Vector2(-48f, 18f), 40f);

            BuildPins(shake, art);
            MenuKit.Particles("Smoke", shake, art.Dot, 26, new Vector2(80f, 220f), new Color(0.75f, 0.72f, 0.7f, 0.07f));
            MenuKit.Particles("Ash", shake, art.Dot, 40, new Vector2(4f, 10f), new Color(0.9f, 0.86f, 0.82f, 0.35f));
            BuildHud(shake, art, cues);

            // Title (glitches) and the dust puff behind it.
            RectTransform titleRoot = UiFactory.MakeRect("Title", shake, new Vector2(0.05f, 0.62f), new Vector2(0.42f, 0.9f));
            CanvasGroup titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
            Image dust = MenuKit.Image("DustPuff", titleRoot, new Vector2(-0.1f, 0.05f), new Vector2(0.9f, 0.85f), art.Dot, new Color(0.78f, 0.74f, 0.7f, 0.5f));
            CanvasGroup dustGroup = dust.gameObject.AddComponent<CanvasGroup>();
            dustGroup.alpha = 0f;
            GlitchText glitch = BuildTitle(titleRoot, art);

            // Left column: Oyunu Başlat, Ayarlar, Galeri, Çıkış, hazard stripe.
            RectTransform column = UiFactory.MakeRect("Buttons", shake, new Vector2(0.055f, 0.2f), new Vector2(0.28f, 0.6f));
            CanvasGroup buttons = column.gameObject.AddComponent<CanvasGroup>();
            Button start = MenuKit.Primary("Start", column, new Vector2(0f, 0.74f), new Vector2(1f, 0.97f), UiKeys.MenuStart, 46f, art, cues);
            Button settings = MenuKit.Secondary("Settings", column, new Vector2(0f, 0.55f), new Vector2(1f, 0.68f), UiKeys.MenuSettings, 34f, art, cues);
            Button gallery = MenuKit.Secondary("Gallery", column, new Vector2(0f, 0.39f), new Vector2(1f, 0.52f), UiKeys.MenuGallery, 34f, art, cues);
            Button quit = MenuKit.Secondary("Quit", column, new Vector2(0f, 0.23f), new Vector2(1f, 0.36f), UiKeys.MenuQuit, 34f, art, cues);
            RawImage stripe = MenuKit.Tiled("HazardStripe", column, new Vector2(0f, 0.11f), new Vector2(1f, 0.17f), art.Hazard, new Color(1f, 1f, 1f, 0.9f), new Vector2(430f / 32f / 2f, 1f));
            stripe.gameObject.AddComponent<UvScroll>().Configure(stripe, new Vector2(0.25f, 0f));
            MenuKit.VerticalNavigation(start, settings, gallery, quit);

            // Panels (centre) and the quit confirmation.
            SettingsPanel settingsPanel = MenuUi.Settings(shake, cues.Click);
            RestylePanel(settingsPanel.gameObject, art);
            TipGalleryPanel galleryPanel = BuildGallery(shake, art, cues, content);
            CanvasGroup confirm = MenuUi.Confirm(shake, out Button yes, out Button no);
            SerializedWire.SetTextKey(confirm.transform.Find("Question").GetComponent<LocalizedText>(), "key", UiKeys.MenuQuitConfirm);
            RestylePanel(confirm.gameObject, art);
            MenuKit.VerticalNavigation(yes, no);
            yes.navigation = Horizontal(no);
            no.navigation = Horizontal(yes);

            MenuKit.Overlays(shake, art, 0.1f, grain: false);
            ScreenFader fader = MenuKit.Fader(canvas, 0.6f);

            MenuIntro intro = systems.AddComponent<MenuIntro>();
            intro.Configure(fader, titleRoot, titleGroup, dustGroup, new[] { (RectTransform)start.transform, (RectTransform)settings.transform, (RectTransform)gallery.transform, (RectTransform)quit.transform, stripe.rectTransform }, cues.Thud);

            MainMenu menu = systems.AddComponent<MainMenu>();
            SerializedWire.Set(menu, "start", start);
            SerializedWire.Set(menu, "settings", settings);
            SerializedWire.Set(menu, "gallery", gallery);
            SerializedWire.Set(menu, "quit", quit);
            SerializedWire.Set(menu, "buttons", buttons);
            SerializedWire.Set(menu, "settingsPanel", settingsPanel);
            SerializedWire.Set(menu, "galleryPanel", galleryPanel);
            SerializedWire.Set(menu, "confirm", confirm);
            SerializedWire.Set(menu, "confirmYes", yes);
            SerializedWire.Set(menu, "confirmNo", no);
            SerializedWire.Set(menu, "fader", fader);
            SerializedWire.Set(menu, "intro", intro);
            SerializedWire.Set(menu, "music", cues.MusicPrep);
            SerializedWire.Set(menu, "rumbleLoop", cues.RumbleLoop);
            SerializedWire.Set(menu, "startRumble", cues.QuakeRumble);
            SerializedWire.Set(menu, "clickCue", cues.Click);
            es.GetComponent<EventSystem>().firstSelectedGameObject = null;
            UiFactory.Theme = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            SetBuildScenes();
            Debug.Log("[MainMenu] Built " + ScenePath + ".");
        }

        private static Navigation Horizontal(Selectable other) => new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = other, selectOnRight = other };

        /// <summary>"İSTANBUL" (distressed material, red / cyan glitch copies, a masked slice copy) over "AFET BÖLGESİ".</summary>
        private static GlitchText BuildTitle(RectTransform root, MenuArt.Set art)
        {
            // A low red haze behind the letters (the mockup's burning glow).
            MenuKit.Image("Haze", root, new Vector2(-0.12f, 0.1f), new Vector2(1.05f, 1.05f), art.Dot, new Color(0.85f, 0.08f, 0.06f, 0.22f));
            RectTransform big = UiFactory.MakeRect("Istanbul", root, new Vector2(0f, 0.32f), new Vector2(1f, 1f));
            TextMeshProUGUI red = MenuKit.Label("GlitchRed", big, Vector2.zero, Vector2.one, UiKeys.MenuTitle, MenuKit.Font.DisplayBold, 150f, new Color(1f, 0.1f, 0.15f, 0.75f), TextAlignmentOptions.BottomLeft);
            TextMeshProUGUI cyan = MenuKit.Label("GlitchCyan", big, Vector2.zero, Vector2.one, UiKeys.MenuTitle, MenuKit.Font.DisplayBold, 150f, new Color(0.1f, 0.95f, 1f, 0.6f), TextAlignmentOptions.BottomLeft);
            TextMeshProUGUI main = MenuKit.Label("Text", big, Vector2.zero, Vector2.one, UiKeys.MenuTitle, MenuKit.Font.DisplayBold, 150f, MenuKit.OffWhite, TextAlignmentOptions.BottomLeft);
            Material distressed = TitleMaterial(main.font, art);
            if (distressed != null) main.fontSharedMaterial = distressed;
            foreach (TextMeshProUGUI t in new[] { red, cyan, main }) t.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform band = UiFactory.MakeRect("Slice", big, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            band.sizeDelta = new Vector2(0f, 26f);
            band.gameObject.AddComponent<RectMask2D>();
            RectTransform sliceText = UiFactory.MakeRect("SliceText", band, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            // Same height as "Istanbul" (1080 x 0.28 of the title root x 0.68), centred on the band = the title centre.
            sliceText.sizeDelta = new Vector2(0f, 1080f * 0.28f * 0.68f);
            TextMeshProUGUI slice = MenuKit.Label("SliceLabel", sliceText, Vector2.zero, Vector2.one, UiKeys.MenuTitle, MenuKit.Font.DisplayBold, 150f, MenuKit.OffWhite, TextAlignmentOptions.BottomLeft);
            slice.textWrappingMode = TextWrappingModes.NoWrap;
            if (distressed != null) slice.fontSharedMaterial = distressed;

            TextMeshProUGUI sub = MenuKit.Label("AfetBolgesi", root, new Vector2(0.005f, 0.04f), new Vector2(1f, 0.32f), UiKeys.MenuSubtitle, MenuKit.Font.DisplayMedium, 64f, MenuKit.OffWhite, TextAlignmentOptions.TopLeft, 8f);
            sub.textWrappingMode = TextWrappingModes.NoWrap;
            sub.outlineColor = MenuKit.DarkRed;
            sub.outlineWidth = 0.12f;

            GlitchText glitch = root.gameObject.AddComponent<GlitchText>();
            glitch.Configure(main, red, cyan, band, (RectTransform)slice.transform, new Vector2(5f, 8f), 0.15f, 10f);
            return glitch;
        }

        /// <summary>
        /// The distressed title material: Oswald Bold SDF with the scratch face texture, a dark red #5A0A0A outline and
        /// a dark red inner shadow (underlay, inner).
        /// </summary>
        private static Material TitleMaterial(TMP_FontAsset font, MenuArt.Set art)
        {
            if (font == null || font.material == null) return null;
            Shader shader = Shader.Find("TextMeshPro/Distance Field");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
            if (m == null)
            {
                m = new Material(font.material);
                AssetDatabase.CreateAsset(m, TitleMaterialPath);
            }
            else
            {
                m.CopyPropertiesFromMaterial(font.material);
            }

            if (shader != null) m.shader = shader;
            m.SetTexture("_MainTex", font.material.GetTexture("_MainTex"));
            m.SetTexture("_FaceTex", art.Scratch);
            // The face texture darkens the whole face in this shader; an over-1 face colour brings the letters back
            // to off-white and leaves only the specks and scratches dark.
            m.SetColor("_FaceColor", new Color(1.45f, 1.45f, 1.45f, 1f));
            m.SetFloat("_FaceUVSpeedX", 0f);
            m.SetFloat("_FaceUVSpeedY", 0f);
            m.SetColor("_OutlineColor", MenuKit.DarkRed);
            m.SetFloat("_OutlineWidth", 0.14f);
            m.SetFloat("_OutlineSoftness", 0.05f);
            m.EnableKeyword("UNDERLAY_INNER");
            m.SetColor("_UnderlayColor", new Color(0.35f, 0.04f, 0.04f, 0.85f));
            m.SetFloat("_UnderlayOffsetX", 0.35f);
            m.SetFloat("_UnderlayOffsetY", -0.45f);
            m.SetFloat("_UnderlayDilate", 0.1f);
            m.SetFloat("_UnderlaySoftness", 0.35f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Pulsing red alert pins over the map's districts (staggered).</summary>
        private static void BuildPins(Transform parent, MenuArt.Set art)
        {
            Vector2[] at = { new Vector2(0.555f, 0.6f), new Vector2(0.685f, 0.645f), new Vector2(0.82f, 0.5f), new Vector2(0.6f, 0.33f), new Vector2(0.77f, 0.24f), new Vector2(0.47f, 0.44f) };
            for (int i = 0; i < at.Length; i++)
            {
                RectTransform pin = UiFactory.MakeRect("Pin", parent, at[i], at[i]);
                pin.sizeDelta = new Vector2(18f, 18f);
                Image ring = MenuKit.Image("Ring", pin, Vector2.zero, Vector2.one, art.Ring, new Color(1f, 0.15f, 0.12f, 0.8f));
                Image dot = MenuKit.Image("Dot", pin, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), art.Disc, new Color(1f, 0.2f, 0.15f, 1f));
                Image halo = MenuKit.Image("Halo", pin, Vector2.zero, Vector2.one, art.Dot, new Color(1f, 0.1f, 0.05f, 0.45f));
                halo.rectTransform.offsetMin = new Vector2(-14f, -14f);
                halo.rectTransform.offsetMax = new Vector2(14f, 14f);
                halo.transform.SetAsFirstSibling();
                pin.gameObject.AddComponent<AlertPin>().Configure(dot, ring, 2.1f + 0.25f * (i % 3), i / (float)at.Length);
            }
        }

        /// <summary>Corner brackets, ticking readouts, the seismograph and the version tag.</summary>
        private static void BuildHud(Transform parent, MenuArt.Set art, MenuAudio.Cues cues)
        {
            RectTransform frame = UiFactory.MakeRect("ScreenBrackets", parent, Vector2.zero, Vector2.one);
            MenuKit.Brackets(frame, art.Bracket, 56f, new Color(0.85f, 0.78f, 0.76f, 0.55f), 22f);

            Readout(parent, "QuakeReadout", new Vector2(0.62f, 0.945f), new Vector2(0.975f, 0.975f), UiKeys.MenuHudQuake, TextAlignmentOptions.Right, 0.5f);
            Readout(parent, "ZoneReadout", new Vector2(0.62f, 0.918f), new Vector2(0.975f, 0.945f), UiKeys.MenuHudZone, TextAlignmentOptions.Right, 0f);
            Readout(parent, "CoordReadout", new Vector2(0.62f, 0.07f), new Vector2(0.975f, 0.1f), UiKeys.MenuHudCoords, TextAlignmentOptions.Right, 0.18f);
            Readout(parent, "FeedReadout", new Vector2(0.035f, 0.165f), new Vector2(0.35f, 0.192f), UiKeys.MenuHudFeed, TextAlignmentOptions.Left, 1f);

            RectTransform seis = UiFactory.MakeRect("Seismograph", parent, new Vector2(0.035f, 0.05f), new Vector2(0.33f, 0.16f));
            Image seisBg = MenuKit.Image("Backdrop", seis, Vector2.zero, Vector2.one, null, new Color(0.05f, 0.01f, 0.01f, 0.45f));
            for (int i = 1; i < 4; i++) MenuKit.Image("GridLine", seis, new Vector2(0f, i / 4f), new Vector2(1f, i / 4f), null, new Color(1f, 0.3f, 0.25f, 0.12f)).rectTransform.sizeDelta = new Vector2(0f, 1f);
            Seismograph trace = UiFactory.MakeRect("Trace", seis, Vector2.zero, Vector2.one).gameObject.AddComponent<Seismograph>();
            trace.raycastTarget = false;
            trace.Configure(cues.Thud, new Color(1f, 0.25f, 0.2f, 0.95f));
            seisBg.raycastTarget = false;

            TextMeshProUGUI version = MenuKit.Label("Version", parent, new Vector2(0.62f, 0.035f), new Vector2(0.975f, 0.065f), UiKeys.MenuVersion, MenuKit.Font.Mono, 16f, new Color(0.75f, 0.7f, 0.68f, 0.6f), TextAlignmentOptions.Right);
            version.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static void Readout(Transform parent, string name, Vector2 min, Vector2 max, string key, TextAlignmentOptions align, float tick)
        {
            TextMeshProUGUI label = MenuKit.Label(name, parent, min, max, tick > 0f ? null : key, MenuKit.Font.Mono, 16f, MenuKit.HudGrey, align, 2f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (tick > 0f) label.gameObject.AddComponent<HudReadout>().Configure(label, key, tick, 61f);
        }

        /// <summary>Galeri: a 6 x 3 grid of note cards, the counter, Geri.</summary>
        private static TipGalleryPanel BuildGallery(Transform parent, MenuArt.Set art, MenuAudio.Cues cues, GameContent content)
        {
            Image panel = MenuKit.Image("GalleryPanel", parent, new Vector2(0.15f, 0.07f), new Vector2(0.85f, 0.93f), art.Panel, MenuArt.Hex(0x1A0607), raycast: true);
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            MenuKit.Brackets(panel.transform, art.Bracket, 34f, new Color(0.8f, 0.76f, 0.72f, 1f), -6f);
            Transform p = panel.transform;
            // Two tabs instead of one title: Deprem Notları / Bilgi Kartları, a red marker under the active one.
            Button tipsTab = MenuKit.Text("TabTips", p, new Vector2(0.03f, 0.9f), new Vector2(0.29f, 0.98f), UiKeys.MenuGalleryTabTips, 36f, MenuKit.OffWhite, cues);
            Button infoTab = MenuKit.Text("TabInfo", p, new Vector2(0.3f, 0.9f), new Vector2(0.56f, 0.98f), UiKeys.MenuGalleryTabInfo, 36f, MenuKit.OffWhite, cues);
            Image tipsMarker = MenuKit.Image("Marker", tipsTab.transform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0.05f), null, MenuKit.AlertRed);
            Image infoMarker = MenuKit.Image("Marker", infoTab.transform, new Vector2(0.1f, 0f), new Vector2(0.9f, 0.05f), null, MenuKit.AlertRed);
            RectTransform tipsPage = UiFactory.MakeRect("TipsPage", p, Vector2.zero, Vector2.one);
            CanvasGroup tipsGroup = tipsPage.gameObject.AddComponent<CanvasGroup>();
            RectTransform infoPage = UiFactory.MakeRect("InfoPage", p, Vector2.zero, Vector2.one);
            CanvasGroup infoGroup = infoPage.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI counter = MenuKit.Label("Counter", p, new Vector2(0.55f, 0.9f), new Vector2(0.96f, 0.98f), null, MenuKit.Font.Mono, 22f, MenuKit.Amber, TextAlignmentOptions.Right);

            var ids = new List<string>();
            if (content != null)
            {
                foreach (Tip tip in content.Tips) if (tip != null) ids.Add(tip.Id);
            }

            const int columns = 6;
            int rows = Mathf.Max(1, Mathf.CeilToInt(ids.Count / (float)columns));
            var titles = new List<TMP_Text>();
            var bodies = new List<TMP_Text>();
            var cards = new List<Image>();
            for (int i = 0; i < ids.Count; i++)
            {
                int c = i % columns, r = i / columns;
                float x0 = 0.03f + c * (0.94f / columns), x1 = x0 + 0.94f / columns - 0.01f;
                float y1 = 0.87f - r * (0.74f / rows), y0 = y1 - 0.74f / rows + 0.012f;
                Image card = MenuKit.Image("Card_" + ids[i], tipsPage, new Vector2(x0, y0), new Vector2(x1, y1), art.Panel, new Color(0.1f, 0.05f, 0.05f, 0.85f));
                TextMeshProUGUI cardTitle = MenuKit.Label("Title", card.transform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.96f), null, MenuKit.Font.DisplayMedium, 20f, MenuKit.OffWhite, TextAlignmentOptions.TopLeft);
                cardTitle.enableAutoSizing = true;
                cardTitle.fontSizeMin = 16f;
                cardTitle.fontSizeMax = 20f;
                TextMeshProUGUI body = MenuKit.Label("Body", card.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.72f), null, MenuKit.Font.Body, 16f, new Color(0.86f, 0.82f, 0.8f, 1f), TextAlignmentOptions.TopLeft);
                body.overflowMode = TextOverflowModes.Ellipsis;
                titles.Add(cardTitle);
                bodies.Add(body);
                cards.Add(card);
            }

            // Bilgi Kartları: the 8 info cards in a 4 x 2 grid.
            var infoTitles = new List<TMP_Text>();
            var infoBodies = new List<TMP_Text>();
            var infoCards = new List<Image>();
            const int infoColumns = 4;
            for (int i = 0; i < InfoCardRules.CardCount; i++)
            {
                int c = i % infoColumns, r = i / infoColumns;
                float x0 = 0.03f + c * (0.94f / infoColumns), x1 = x0 + 0.94f / infoColumns - 0.012f;
                float y1 = 0.87f - r * 0.37f, y0 = y1 - 0.37f + 0.015f;
                Image card = MenuKit.Image("Info_" + (i + 1), infoPage, new Vector2(x0, y0), new Vector2(x1, y1), art.Panel, new Color(0.1f, 0.05f, 0.05f, 0.85f));
                TextMeshProUGUI cardTitle = MenuKit.Label("Title", card.transform, new Vector2(0.06f, 0.8f), new Vector2(0.94f, 0.96f), null, MenuKit.Font.DisplayMedium, 22f, MenuKit.OffWhite, TextAlignmentOptions.TopLeft);
                cardTitle.enableAutoSizing = true;
                cardTitle.fontSizeMin = 16f;
                cardTitle.fontSizeMax = 22f;
                TextMeshProUGUI body = MenuKit.Label("Body", card.transform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.79f), null, MenuKit.Font.Body, 16f, new Color(0.86f, 0.82f, 0.8f, 1f), TextAlignmentOptions.TopLeft);
                body.overflowMode = TextOverflowModes.Ellipsis;
                infoTitles.Add(cardTitle);
                infoBodies.Add(body);
                infoCards.Add(card);
            }

            Button back = MenuKit.Secondary("Back", p, new Vector2(0.38f, 0.025f), new Vector2(0.62f, 0.095f), UiKeys.SettingsBack, 30f, art, cues);
            TipGalleryPanel gallery = panel.gameObject.AddComponent<TipGalleryPanel>();
            gallery.Configure(group, counter, ids.ToArray(), titles.ToArray(), bodies.ToArray(), cards.ToArray(), back, cues.Click);
            gallery.ConfigureTabs(tipsGroup, infoGroup, tipsTab, infoTab, tipsMarker.gameObject, infoMarker.gameObject, infoTitles.ToArray(), infoBodies.ToArray(), infoCards.ToArray());
            return gallery;
        }

        /// <summary>Settings / confirm panels in the menu look: dark red panel, Oswald headers and labels, red rows.</summary>
        private static void RestylePanel(GameObject panel, MenuArt.Set art)
        {
            Image bg = panel.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = art.Panel;
                bg.type = Image.Type.Sliced;
                bg.color = MenuArt.Hex(0x1A0607);
            }

            MenuKit.Brackets(panel.transform, art.Bracket, 30f, new Color(0.8f, 0.76f, 0.72f, 1f), -6f);
            UITheme theme = UiFactory.Theme;
            foreach (TextMeshProUGUI label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (theme == null) break;
                bool header = label.name == "Title";
                label.font = header ? theme.DisplayBold : theme.DisplayMedium;
                if (header)
                {
                    label.fontStyle = FontStyles.UpperCase;
                    label.characterSpacing = 6f;
                }
            }

            foreach (Button b in panel.GetComponentsInChildren<Button>(true))
            {
                if (b.targetGraphic is Image img) img.color = MenuKit.RowRed;
            }

            foreach (Slider s in panel.GetComponentsInChildren<Slider>(true))
            {
                if (s.fillRect != null && s.fillRect.TryGetComponent(out Image fill)) fill.color = MenuKit.BrightRed;
            }

            foreach (Toggle t in panel.GetComponentsInChildren<Toggle>(true))
            {
                if (t.graphic is Image check) check.color = MenuKit.BrightRed;
            }
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
    }
}
