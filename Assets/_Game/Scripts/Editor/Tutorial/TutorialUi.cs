using SixtyOne.Audio;
using SixtyOne.Text;
using SixtyOne.Tutorial;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// The tutorial's UI on the game canvas, in the speech-bubble look (paper card, ink outline, soft shadow, name tab,
    /// Inter SemiBold): the bottom-centre hint card with Geç, the pulsing outline + arrow, and the "Eğitim tamamlandı"
    /// panel in the pause-menu look (serif title, Oyuna başla / Ana menü). UiFactory.Theme / Rounded must be set.
    /// </summary>
    public static class TutorialUi
    {
        private static readonly Color Paper = new Color(0.969f, 0.953f, 0.918f, 1f);      // #F7F3EA, the bubble paper
        private static readonly Color Ink = new Color(0.106f, 0.114f, 0.133f, 1f);        // #1B1D22, the bubble ink
        private static readonly Color Pencil = new Color(0.35f, 0.35f, 0.38f, 1f);

        public sealed class Parts
        {
            public TutorialHintPanel Hint;
            public TutorialHighlight Highlight;
            public CanvasGroup Complete;
            public Button Play;
            public Button Menu;
        }

        public static Parts Build(Transform canvas, Camera camera, AudioCue click)
        {
            UITheme theme = UiFactory.Theme;
            Color accent = theme != null ? theme.Accent : new Color(1f, 0.76f, 0.29f, 1f);
            var parts = new Parts
            {
                Highlight = BuildHighlight(canvas, camera, accent, theme),
                Hint = BuildHint(canvas, click, accent, theme)
            };
            parts.Complete = BuildComplete(canvas, out parts.Play, out parts.Menu);
            return parts;
        }

        private static TutorialHintPanel BuildHint(Transform canvas, AudioCue click, Color accent, UITheme theme)
        {
            RectTransform root = UiFactory.MakeRect("TutorialHint", canvas, new Vector2(0.27f, 0.035f), new Vector2(0.73f, 0.205f));
            CanvasGroup group = UiFactory.MakeGroup(root.gameObject);
            RectTransform card = UiFactory.MakeRect("Card", root, Vector2.zero, Vector2.one);

            Image shadow = UiFactory.MakeImage("Shadow", card, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.28f));
            UiFactory.MakeRound(shadow);
            shadow.rectTransform.offsetMin = new Vector2(4f, -5f);
            shadow.rectTransform.offsetMax = new Vector2(4f, -5f);
            Image outline = UiFactory.MakeImage("Outline", card, Vector2.zero, Vector2.one, Ink);
            UiFactory.MakeRound(outline);
            outline.rectTransform.offsetMin = new Vector2(-2f, -2f);
            outline.rectTransform.offsetMax = new Vector2(2f, 2f);
            Image fill = UiFactory.MakeImage("Paper", card, Vector2.zero, Vector2.one, Paper, raycast: true);
            UiFactory.MakeRound(fill);

            // "ÖĞRETİCİ" name tab on the top-left edge (the bubble's speaker tab, in the accent colour).
            Image tabOutline = UiFactory.MakeImage("Tab", card, new Vector2(0f, 1f), new Vector2(0f, 1f), Ink);
            UiFactory.MakeRound(tabOutline);
            tabOutline.rectTransform.pivot = new Vector2(0f, 0.5f);
            tabOutline.rectTransform.anchoredPosition = new Vector2(18f, 2f);
            tabOutline.rectTransform.sizeDelta = new Vector2(170f, 34f);
            Image tabFill = UiFactory.MakeImage("Fill", tabOutline.transform, Vector2.zero, Vector2.one, Paper);
            UiFactory.MakeRound(tabFill);
            tabFill.rectTransform.offsetMin = new Vector2(2f, 2f);
            tabFill.rectTransform.offsetMax = new Vector2(-2f, -2f);
            TextMeshProUGUI tab = Label("Name", tabOutline.transform, Vector2.zero, Vector2.one, 17f, TextAlignmentOptions.Center, Color.Lerp(accent, Ink, 0.35f), theme != null ? theme.Bold : null);
            tab.characterSpacing = 4f;
            Keyed(tab, UiKeys.TutorialTag);

            TextMeshProUGUI step = Label("Step", card, new Vector2(0.6f, 0.76f), new Vector2(0.8f, 0.96f), 16f, TextAlignmentOptions.TopRight, Pencil, theme != null ? theme.SemiBold : null);
            TextMeshProUGUI body = Label("Body", card, new Vector2(0.035f, 0.26f), new Vector2(0.8f, 0.8f), 24f, TextAlignmentOptions.MidlineLeft, Ink, theme != null ? theme.SemiBold : null);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.enableAutoSizing = true;
            body.fontSizeMin = 18f;
            body.fontSizeMax = 24f;
            TextMeshProUGUI next = Label("Continue", card, new Vector2(0.035f, 0.05f), new Vector2(0.8f, 0.26f), 17f, TextAlignmentOptions.MidlineLeft, Pencil, theme != null ? theme.Regular : null);
            next.fontStyle = FontStyles.Italic;

            Button skip = UiFactory.MakeButton("Skip", card, new Vector2(0.82f, 0.28f), new Vector2(0.97f, 0.72f), Ink, 22f, out TextMeshProUGUI skipLabel);
            skipLabel.color = Paper;
            if (theme != null) skipLabel.font = theme.Bold;
            Keyed(skipLabel, UiKeys.TutorialSkipStep);

            TutorialHintPanel panel = root.gameObject.AddComponent<TutorialHintPanel>();
            panel.Configure(group, card, body, step, next, skip, click);
            return panel;
        }

        private static TutorialHighlight BuildHighlight(Transform canvas, Camera camera, Color accent, UITheme theme)
        {
            RectTransform layer = UiFactory.MakeRect("TutorialHighlight", canvas, Vector2.zero, Vector2.one);

            RectTransform frame = UiFactory.MakeRect("Frame", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            frame.sizeDelta = new Vector2(120f, 120f);
            CanvasGroup frameGroup = frame.gameObject.AddComponent<CanvasGroup>();
            frameGroup.blocksRaycasts = false;
            frameGroup.interactable = false;
            const float t = 4f;
            Bar(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, t), accent);
            Bar(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, t), accent);
            Bar(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(t, 0f), accent);
            Bar(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(t, 0f), accent);

            RectTransform arrow = UiFactory.MakeRect("Arrow", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            arrow.sizeDelta = new Vector2(56f, 48f);
            CanvasGroup arrowGroup = arrow.gameObject.AddComponent<CanvasGroup>();
            arrowGroup.blocksRaycasts = false;
            arrowGroup.interactable = false;
            TextMeshProUGUI glyph = Label("Glyph", arrow, Vector2.zero, Vector2.one, 44f, TextAlignmentOptions.Center, accent, theme != null ? theme.Bold : null);
            glyph.text = "▼";
            glyph.outlineColor = Ink;
            glyph.outlineWidth = 0.2f;

            TutorialHighlight highlight = layer.gameObject.AddComponent<TutorialHighlight>();
            highlight.Configure((RectTransform)canvas, frame, frameGroup, arrow, arrowGroup, camera);
            return highlight;
        }

        private static CanvasGroup BuildComplete(Transform canvas, out Button play, out Button menu)
        {
            Image dim = UiFactory.MakeImage("TutorialComplete", canvas, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.75f), raycast: true);
            CanvasGroup group = dim.gameObject.AddComponent<CanvasGroup>();
            Image panel = UiFactory.MakePanel("Panel", dim.transform, new Vector2(0.34f, 0.3f), new Vector2(0.66f, 0.7f), MenuUi.PanelColor, raycast: true);
            MenuUi.KeyLabel("Title", panel.transform, new Vector2(0.06f, 0.7f), new Vector2(0.94f, 0.92f), UiKeys.TutorialDoneTitle, 46f, TextAlignmentOptions.Center, true, true);
            TextMeshProUGUI body = MenuUi.KeyLabel("Body", panel.transform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.68f), UiKeys.TutorialDoneBody, 26f, TextAlignmentOptions.Center, false, false);
            body.textWrappingMode = TextWrappingModes.Normal;
            play = MenuUi.Button("Play", panel.transform, new Vector2(0.1f, 0.1f), new Vector2(0.48f, 0.32f), UiKeys.TutorialDonePlay, 30f);
            menu = MenuUi.Button("Menu", panel.transform, new Vector2(0.52f, 0.1f), new Vector2(0.9f, 0.32f), UiKeys.TutorialDoneMenu, 30f);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Bar(RectTransform frame, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
        {
            Image bar = UiFactory.MakeImage(name, frame, min, max, color);
            bar.raycastTarget = false;
            bar.rectTransform.sizeDelta = size;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 min, Vector2 max, float size, TextAlignmentOptions align, Color color, TMP_FontAsset font)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel(name, parent, min, max, size, align, color);
            if (font != null) label.font = font;
            label.raycastTarget = false;
            return label;
        }

        private static void Keyed(TextMeshProUGUI label, string key)
        {
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", key);
        }
    }
}
