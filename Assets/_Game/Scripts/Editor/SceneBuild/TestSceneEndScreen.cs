using SixtyOne.Nodes;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// The "SİMÜLASYON SONA ERDİ" overlay in the game canvas (EndScreen): ruins background (Art/Menu/end_bg.png),
    /// ash, grain, scanlines and a vignette; the centred text column (max 900 px); the stats block; Yeniden Dene and
    /// Menüye Dön; the AFAD footer; its own fader. Hidden until GameEvents.EpilogueDone. UiFactory.Theme must be set.
    /// </summary>
    public static class TestSceneEndScreen
    {
        private static readonly Color Grey = new Color(0.8f, 0.8f, 0.79f, 1f);
        private static readonly Color Body = new Color(0.93f, 0.92f, 0.9f, 1f);

        public static EndScreen Build(Transform canvas, GameContent content)
        {
            MenuArt.Set art = MenuArt.Generate();
            MenuAudio.Cues cues = MenuAudio.Load();

            RectTransform rootRect = UiFactory.MakeRect("EndScreen", canvas, Vector2.zero, Vector2.one);
            CanvasGroup root = rootRect.gameObject.AddComponent<CanvasGroup>();
            Image blocker = UiFactory.MakeImage("Black", rootRect, Vector2.zero, Vector2.one, Color.black, raycast: true);
            blocker.raycastTarget = true;
            RectTransform shake = UiFactory.MakeRect("Shake", rootRect, Vector2.zero, Vector2.one);
            shake.gameObject.AddComponent<UiShake>();
            Image bg = MenuKit.Image("Background", shake, Vector2.zero, Vector2.one, art.EndBackground, Color.white);
            bg.rectTransform.offsetMin = new Vector2(-30f, -30f);
            bg.rectTransform.offsetMax = new Vector2(30f, 30f);

            RectTransform ambience = UiFactory.MakeRect("Ambience", shake, Vector2.zero, Vector2.one);
            MenuKit.Particles("Ash", ambience, art.Dot, 46, new Vector2(4f, 9f), new Color(0.85f, 0.83f, 0.8f, 0.4f));
            MenuKit.Particles("Smoke", ambience, art.Dot, 14, new Vector2(120f, 260f), new Color(0.6f, 0.6f, 0.62f, 0.06f));

            RectTransform column = UiFactory.MakeRect("Column", shake, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f));
            column.sizeDelta = new Vector2(900f, 0f);
            TextMeshProUGUI title = MenuKit.Label("Title", column, new Vector2(0f, 0.62f), new Vector2(1f, 0.71f), null, MenuKit.Font.MonoBold, 72f, Grey, TextAlignmentOptions.Center, 6f);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI line = MenuKit.Label("Line", column, new Vector2(0f, 0.505f), new Vector2(1f, 0.61f), null, MenuKit.Font.Mono, 30f, Body, TextAlignmentOptions.Top);
            line.lineSpacing = 8f;

            RectTransform punchRoot = UiFactory.MakeRect("Punchline", column, new Vector2(0f, 0.405f), new Vector2(1f, 0.5f));
            TextMeshProUGUI punchRed = MenuKit.Label("GlitchRed", punchRoot, Vector2.zero, Vector2.one, null, MenuKit.Font.Mono, 30f, new Color(1f, 0.1f, 0.15f, 0.7f), TextAlignmentOptions.Top);
            TextMeshProUGUI punchCyan = MenuKit.Label("GlitchCyan", punchRoot, Vector2.zero, Vector2.one, null, MenuKit.Font.Mono, 30f, new Color(0.1f, 0.95f, 1f, 0.55f), TextAlignmentOptions.Top);
            TextMeshProUGUI punch = MenuKit.Label("Text", punchRoot, Vector2.zero, Vector2.one, null, MenuKit.Font.Mono, 30f, MenuKit.AlertRed, TextAlignmentOptions.Top);
            punch.lineSpacing = 8f;
            RectTransform band = UiFactory.MakeRect("Slice", punchRoot, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            band.sizeDelta = new Vector2(0f, 14f);
            band.gameObject.AddComponent<RectMask2D>();
            RectTransform sliceText = UiFactory.MakeRect("SliceText", band, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f));
            sliceText.sizeDelta = new Vector2(0f, 1080f * 0.095f);
            TextMeshProUGUI slice = MenuKit.Label("SliceLabel", sliceText, Vector2.zero, Vector2.one, null, MenuKit.Font.Mono, 30f, MenuKit.AlertRed, TextAlignmentOptions.Top);
            slice.lineSpacing = 8f;
            GlitchText glitch = punchRoot.gameObject.AddComponent<GlitchText>();
            glitch.Configure(punch, punchRed, punchCyan, band, slice.rectTransform, Vector2.zero, 0.3f, 12f);

            // Stats (left) and Yeniden Dene (right), then Menüye Dön under them.
            RectTransform statsRect = UiFactory.MakeRect("Stats", column, new Vector2(0.02f, 0.22f), new Vector2(0.62f, 0.38f));
            CanvasGroup stats = statsRect.gameObject.AddComponent<CanvasGroup>();
            string[] labels = { UiKeys.EndStatLoops, UiKeys.EndStatTips, UiKeys.EndStatTime, UiKeys.EndStatFamily };
            var values = new TMP_Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                float y1 = 1f - i * 0.25f, y0 = y1 - 0.25f;
                MenuKit.Label("Label" + i, statsRect, new Vector2(0f, y0), new Vector2(0.55f, y1), labels[i], MenuKit.Font.Mono, 20f, new Color(0.62f, 0.62f, 0.62f, 1f), TextAlignmentOptions.MidlineLeft).textWrappingMode = TextWrappingModes.NoWrap;
                TextMeshProUGUI value = MenuKit.Label("Value" + i, statsRect, new Vector2(0.56f, y0), new Vector2(1.3f, y1), null, MenuKit.Font.MonoBold, 20f, Body, TextAlignmentOptions.MidlineLeft);
                value.textWrappingMode = TextWrappingModes.NoWrap;
                values[i] = value;
            }

            RectTransform buttonsRect = UiFactory.MakeRect("Buttons", column, Vector2.zero, Vector2.one);
            CanvasGroup buttons = buttonsRect.gameObject.AddComponent<CanvasGroup>();
            Button retry = MenuKit.Text("Retry", buttonsRect, new Vector2(0.68f, 0.3f), new Vector2(0.98f, 0.36f), UiKeys.EndRetry, 30f, MenuKit.Amber, cues);
            Button menu = MenuKit.Primary("Menu", buttonsRect, new Vector2(0.33f, 0.1f), new Vector2(0.67f, 0.18f), UiKeys.EndMenu, 30f, art, cues,
                panelTint: new Color(0.3f, 0.28f, 0.27f, 1f), glowColor: new Color(1f, 0.55f, 0.12f, 0.55f), borderColor: new Color(1f, 0.6f, 0.2f, 1f));
            MenuKit.VerticalNavigation(retry, menu);

            TextMeshProUGUI afad = MenuKit.Label("Afad", column, new Vector2(-0.2f, 0.025f), new Vector2(1.2f, 0.06f), UiKeys.EndAfad, MenuKit.Font.Mono, 18f, new Color(0.7f, 0.7f, 0.68f, 0.8f), TextAlignmentOptions.Center);
            afad.textWrappingMode = TextWrappingModes.NoWrap;

            MenuKit.Overlays(ambience, art, 0.05f, grain: true);
            ScreenFader fader = MenuKit.Fader(rootRect, 0f);

            EndScreen end = rootRect.gameObject.AddComponent<EndScreen>();
            SerializedWire.Set(end, "root", root);
            SerializedWire.Set(end, "background", bg.rectTransform);
            SerializedWire.Set(end, "ambience", ambience.gameObject);
            SerializedWire.Set(end, "title", title);
            SerializedWire.Set(end, "line", line);
            SerializedWire.Set(end, "punchline", punch);
            SerializedWire.Set(end, "punchGlitch", glitch);
            SerializedWire.Set(end, "stats", stats);
            SerializedWire.SetArray(end, "statValues", values);
            SerializedWire.SetInt(end, "totalTips", content != null ? content.Tips.Count : 18);
            SerializedWire.Set(end, "buttons", buttons);
            SerializedWire.Set(end, "retry", retry);
            SerializedWire.Set(end, "menu", menu);
            SerializedWire.Set(end, "fader", fader);
            SerializedWire.Set(end, "outroMusic", cues.MusicOutro);
            SerializedWire.Set(end, "typeCue", cues.Type);
            SerializedWire.Set(end, "boomCue", cues.Boom);
            return end;
        }
    }
}
