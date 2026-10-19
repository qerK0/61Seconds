using SixtyOne.Nodes;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// The two full-screen cards of the game canvas, in the end screen's look (dark, Oswald titles, IBM Plex Mono
    /// body, ash, grain, scanlines, vignette): the "BİLGİLENDİRME" intro briefing (IntroBriefing, visible from the
    /// first frame of every new run) and the floor-transition "Deprem Bilgisi" card (InfoCardScreen, hidden until a
    /// floor change or a macro rewind). UiFactory.Theme must be set.
    /// </summary>
    public static class TestSceneIntroCards
    {
        private static readonly Color Body = new Color(0.93f, 0.92f, 0.9f, 1f);
        private static readonly Color Dim = new Color(0.62f, 0.62f, 0.6f, 1f);

        public static InfoCardScreen BuildInfoCards(Transform canvas, NodePlayer nodePlayer)
        {
            MenuArt.Set art = MenuArt.Generate();
            MenuAudio.Cues cues = MenuAudio.Load();

            RectTransform rootRect = UiFactory.MakeRect("InfoCards", canvas, Vector2.zero, Vector2.one);
            CanvasGroup root = rootRect.gameObject.AddComponent<CanvasGroup>();
            UiFactory.MakeImage("Black", rootRect, Vector2.zero, Vector2.one, new Color(0.035f, 0.03f, 0.03f, 1f), raycast: true);
            RectTransform ambience = UiFactory.MakeRect("Ambience", rootRect, Vector2.zero, Vector2.one);
            MenuKit.Particles("Ash", ambience, art.Dot, 30, new Vector2(3f, 7f), new Color(0.85f, 0.83f, 0.8f, 0.3f));

            // Top-left: the destination (3. KAT / 2. KAT / SOKAK) over a thin red rule.
            TextMeshProUGUI floor = MenuKit.Label("Floor", rootRect, new Vector2(0.04f, 0.86f), new Vector2(0.5f, 0.95f), null, MenuKit.Font.DisplayBold, 64f, MenuKit.OffWhite, TextAlignmentOptions.TopLeft, 4f);
            floor.textWrappingMode = TextWrappingModes.NoWrap;
            Image rule = UiFactory.MakeImage("Rule", rootRect, new Vector2(0.04f, 0.852f), new Vector2(0.16f, 0.855f), MenuKit.AlertRed);
            rule.raycastTarget = false;

            RectTransform column = UiFactory.MakeRect("Column", rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
            column.sizeDelta = new Vector2(900f, 0f);
            TextMeshProUGUI counter = MenuKit.Label("Counter", column, new Vector2(0f, 0.66f), new Vector2(1f, 0.7f), null, MenuKit.Font.MonoBold, 18f, MenuKit.Amber, TextAlignmentOptions.BottomLeft, 6f);
            counter.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI title = MenuKit.Label("Title", column, new Vector2(0f, 0.57f), new Vector2(1f, 0.66f), null, MenuKit.Font.DisplayBold, 56f, MenuKit.OffWhite, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI body = MenuKit.Label("Body", column, new Vector2(0f, 0.33f), new Vector2(1f, 0.56f), null, MenuKit.Font.Mono, 26f, Body, TextAlignmentOptions.TopLeft);
            body.lineSpacing = 10f;
            TextMeshProUGUI game = MenuKit.Label("Game", column, new Vector2(0f, 0.24f), new Vector2(1f, 0.32f), null, MenuKit.Font.Mono, 20f, Dim, TextAlignmentOptions.TopLeft);

            // The thin progress bar: a dim track and a red fill whose x scale grows (pivot left).
            Image track = UiFactory.MakeImage("ProgressTrack", column, new Vector2(0f, 0.17f), new Vector2(1f, 0.173f), new Color(1f, 1f, 1f, 0.12f));
            track.raycastTarget = false;
            Image fill = UiFactory.MakeImage("ProgressFill", track.rectTransform, Vector2.zero, Vector2.one, MenuKit.AlertRed);
            fill.raycastTarget = false;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.localScale = new Vector3(0f, 1f, 1f);

            MenuKit.Overlays(ambience, art, 0.04f, grain: true);

            InfoCardScreen screen = rootRect.gameObject.AddComponent<InfoCardScreen>();
            SerializedWire.Set(screen, "root", root);
            SerializedWire.Set(screen, "nodePlayer", nodePlayer);
            SerializedWire.Set(screen, "floorLabel", floor);
            SerializedWire.Set(screen, "counter", counter);
            SerializedWire.Set(screen, "title", title);
            SerializedWire.Set(screen, "body", body);
            SerializedWire.Set(screen, "gameLine", game);
            SerializedWire.Set(screen, "progress", fill.rectTransform);
            SerializedWire.Set(screen, "showCue", cues.Thud);
            return screen;
        }

        public static IntroBriefing BuildIntro(Transform canvas)
        {
            MenuArt.Set art = MenuArt.Generate();
            MenuAudio.Cues cues = MenuAudio.Load();

            RectTransform rootRect = UiFactory.MakeRect("IntroBriefing", canvas, Vector2.zero, Vector2.one);
            CanvasGroup root = rootRect.gameObject.AddComponent<CanvasGroup>();
            UiFactory.MakeImage("Black", rootRect, Vector2.zero, Vector2.one, Color.black, raycast: true);
            Image bg = MenuKit.Image("Background", rootRect, Vector2.zero, Vector2.one, art.EndBackground, new Color(0.28f, 0.27f, 0.27f, 1f));
            bg.raycastTarget = false;
            RectTransform ambience = UiFactory.MakeRect("Ambience", rootRect, Vector2.zero, Vector2.one);
            MenuKit.Particles("Ash", ambience, art.Dot, 40, new Vector2(4f, 9f), new Color(0.85f, 0.83f, 0.8f, 0.35f));
            MenuKit.Particles("Smoke", ambience, art.Dot, 12, new Vector2(120f, 260f), new Color(0.6f, 0.6f, 0.62f, 0.05f));

            RectTransform column = UiFactory.MakeRect("Column", rootRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
            column.sizeDelta = new Vector2(900f, 0f);
            TextMeshProUGUI title = MenuKit.Label("Title", column, new Vector2(0f, 0.74f), new Vector2(1f, 0.84f), UiKeys.IntroTitle, MenuKit.Font.DisplayBold, 72f, MenuKit.OffWhite, TextAlignmentOptions.BottomLeft, 4f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            Image rule = UiFactory.MakeImage("Rule", column, new Vector2(0f, 0.728f), new Vector2(0.14f, 0.731f), MenuKit.AlertRed);
            rule.raycastTarget = false;
            TextMeshProUGUI body = MenuKit.Label("Body", column, new Vector2(0f, 0.4f), new Vector2(1f, 0.7f), UiKeys.IntroBody, MenuKit.Font.Mono, 26f, Body, TextAlignmentOptions.TopLeft);
            body.lineSpacing = 10f;
            body.paragraphSpacing = 18f;
            TextMeshProUGUI controls = MenuKit.Label("Controls", column, new Vector2(-0.05f, 0.31f), new Vector2(1.05f, 0.36f), UiKeys.IntroControls, MenuKit.Font.Mono, 20f, MenuKit.Amber, TextAlignmentOptions.Center);
            controls.textWrappingMode = TextWrappingModes.NoWrap;
            Button start = MenuKit.Primary("Start", column, new Vector2(0.32f, 0.17f), new Vector2(0.68f, 0.25f), UiKeys.IntroStart, 32f, art, cues);
            TextMeshProUGUI source = MenuKit.Label("Source", column, new Vector2(-0.2f, 0.06f), new Vector2(1.2f, 0.1f), UiKeys.IntroSource, MenuKit.Font.Mono, 18f, new Color(0.7f, 0.7f, 0.68f, 0.8f), TextAlignmentOptions.Center);
            source.textWrappingMode = TextWrappingModes.NoWrap;

            MenuKit.Overlays(ambience, art, 0.05f, grain: true);

            IntroBriefing intro = rootRect.gameObject.AddComponent<IntroBriefing>();
            SerializedWire.Set(intro, "root", root);
            SerializedWire.Set(intro, "start", start);
            SerializedWire.Set(intro, "pressCue", cues.Press);
            return intro;
        }
    }
}
