using SixtyOne.Building;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builds the dialogue layer: the full-screen click catcher with the DialoguePresenter, the top narration card,
    /// the one speech bubble (paper card, outline, shadow, name tab, line, ▼, LED cost chip, speech tail, thought
    /// dots) and its row / pill templates. Bubbles stay below the panic meter and left of the bag column.
    /// </summary>
    public static class TestSceneDialogueUi
    {
        private static readonly Color Paper = new Color(0.969f, 0.953f, 0.918f, 1f);      // #F7F3EA
        private static readonly Color Ink = new Color(0.106f, 0.114f, 0.133f, 1f);        // #1B1D22
        private static readonly Color Night = new Color(0.055f, 0.067f, 0.086f, 0.9f);    // #0E1116 @ 90%

        public static DialoguePresenter Build(Transform root, PlaceholderSprites art, UITheme theme, SurvivalController survival)
        {
            TMP_FontAsset semiBold = theme != null ? theme.SemiBold : null;
            TMP_FontAsset bold = theme != null ? theme.Bold : null;

            // Full-screen layer: transparent catcher (raycast only while a line waits for "click anywhere").
            Image layer = UiFactory.MakeImage("Dialogue", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f), raycast: false);
            RectTransform layerRect = layer.rectTransform;

            NarrationCard card = BuildNarration(layerRect, semiBold);

            // ---- the bubble block ----
            RectTransform block = Rect("SpeechBubble", layerRect);
            CanvasGroup group = block.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            RectTransform back = Rect("BubbleBack", block);
            Image shadow = Stretch(UiFactory.MakeImage("Shadow", back, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.28f)), 4f, -5f, 0f);
            UiFactory.MakeRound(shadow);
            Image outline = Stretch(UiFactory.MakeImage("Outline", back, Vector2.zero, Vector2.one, Ink), 0f, 0f, 2f);
            UiFactory.MakeRound(outline);

            Image tailOutline = UiFactory.MakeImage("TailOutline", block, Vector2.zero, Vector2.zero, Ink);
            Image tailFill = UiFactory.MakeImage("TailFill", block, Vector2.zero, Vector2.zero, Paper);

            Image fill = UiFactory.MakeImage("Bubble", block, Vector2.zero, Vector2.zero, Paper);
            UiFactory.MakeRound(fill);
            RectTransform bubble = fill.rectTransform;
            bubble.pivot = Vector2.zero;

            TextMeshProUGUI body = UiFactory.MakeLabel("Line", bubble, new Vector2(0f, 1f), new Vector2(0f, 1f), 20f, TextAlignmentOptions.TopLeft, Ink);
            if (semiBold != null) body.font = semiBold;
            body.rectTransform.pivot = new Vector2(0f, 1f);
            body.rectTransform.anchoredPosition = new Vector2(18f, -14f);
            body.maxVisibleLines = 4;
            body.overflowMode = TextOverflowModes.Ellipsis;
            body.raycastTarget = false;
            TypewriterText typewriter = body.gameObject.AddComponent<TypewriterText>();
            typewriter.Configure(body, theme != null ? theme.TypewriterCharsPerSecond : 90f);

            // Name tab on the top-left edge: paper tab, ink outline, name in the speaker's colour.
            Image tabOutline = UiFactory.MakeImage("NameTab", bubble, new Vector2(0f, 1f), new Vector2(0f, 1f), Ink);
            UiFactory.MakeRound(tabOutline);
            RectTransform tab = tabOutline.rectTransform;
            tab.pivot = new Vector2(0f, 0.5f);
            tab.anchoredPosition = new Vector2(14f, 2f);
            tab.sizeDelta = new Vector2(150f, 30f);
            Image tabFill = Stretch(UiFactory.MakeImage("Fill", tab, Vector2.zero, Vector2.one, Paper), 0f, 0f, -2f);
            UiFactory.MakeRound(tabFill);
            TextMeshProUGUI name = UiFactory.MakeLabel("Name", tab, Vector2.zero, Vector2.one, 16f, TextAlignmentOptions.Center, Ink);
            if (bold != null) name.font = bold;
            name.characterSpacing = 4f;
            ContentSizeFitter tabFit = tab.gameObject.AddComponent<ContentSizeFitter>();
            tabFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            HorizontalLayoutGroup tabLayout = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.padding = new RectOffset(12, 12, 2, 2);
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            LayoutElement ignoreFill = tabFill.gameObject.AddComponent<LayoutElement>();
            ignoreFill.ignoreLayout = true;

            TextMeshProUGUI mark = UiFactory.MakeLabel("Continue", bubble, Vector2.right, Vector2.right, 16f, TextAlignmentOptions.Center, Ink);
            mark.text = "▼";
            mark.rectTransform.sizeDelta = new Vector2(20f, 20f);

            // LED cost chip hanging off the bottom-right corner.
            Image chip = UiFactory.MakeImage("CostChip", bubble, Vector2.right, Vector2.right, Night);
            UiFactory.MakeRound(chip);
            chip.rectTransform.pivot = new Vector2(1f, 1f);
            chip.rectTransform.anchoredPosition = new Vector2(10f, 8f);
            chip.rectTransform.sizeDelta = new Vector2(78f, 28f);
            TextMeshProUGUI chipLabel = UiFactory.MakeLabel("Cost", chip.transform, Vector2.zero, Vector2.one, 16f, TextAlignmentOptions.Center, Color.green);
            if (theme != null && theme.Led != null) chipLabel.font = theme.Led;

            // Thought dots (Mehmet), big to small, ink-outlined.
            var dots = new Image[3];
            float[] sizes = { 14f, 10f, 7f };
            for (int i = 0; i < dots.Length; i++)
            {
                Image dot = UiFactory.MakeImage("ThoughtDot" + i, block, Vector2.zero, Vector2.zero, Paper, sprite: art.Dot);
                dot.rectTransform.sizeDelta = Vector2.one * sizes[i];
                Outline o = dot.gameObject.AddComponent<Outline>();
                o.effectColor = Ink;
                o.effectDistance = new Vector2(1.5f, -1.5f);
                dots[i] = dot;
            }

            RectTransform rows = Rect("Rows", block);
            BubbleChoiceRow rowTemplate = BuildRow(rows, art, semiBold, bold, false);
            BubbleChoiceRow pillTemplate = BuildRow(rows, art, semiBold, bold, true);

            SpeechBubble view = block.gameObject.AddComponent<SpeechBubble>();
            SerializedWire.Set(view, "block", block);
            SerializedWire.Set(view, "group", group);
            SerializedWire.Set(view, "bubble", bubble);
            SerializedWire.Set(view, "bubbleBack", back);
            SerializedWire.Set(view, "fill", fill);
            SerializedWire.Set(view, "nameTab", tab);
            SerializedWire.Set(view, "nameLabel", name);
            SerializedWire.Set(view, "body", body);
            SerializedWire.Set(view, "typewriter", typewriter);
            SerializedWire.Set(view, "continueMark", mark.rectTransform);
            SerializedWire.Set(view, "costChip", chip.rectTransform);
            SerializedWire.Set(view, "costLabel", chipLabel);
            SerializedWire.Set(view, "tailOutline", tailOutline);
            SerializedWire.Set(view, "tailFill", tailFill);
            SerializedWire.SetArray(view, "thoughtDots", dots);
            SerializedWire.Set(view, "rows", rows);
            SerializedWire.Set(view, "rowTemplate", rowTemplate);
            SerializedWire.Set(view, "pillTemplate", pillTemplate);
            SerializedWire.Set(view, "topLimit", root.Find("PanicMeter") as RectTransform);
            SerializedWire.Set(view, "rightLimit", root.Find("RightColumn/Column") as RectTransform);

            DialoguePresenter presenter = layer.gameObject.AddComponent<DialoguePresenter>();
            SerializedWire.Set(presenter, "bubble", view);
            SerializedWire.Set(presenter, "narration", card);
            SerializedWire.Set(presenter, "catcher", layer);
            SerializedWire.Set(presenter, "survival", survival);
            return presenter;
        }

        /// <summary>Top-centre card under the timer / panic meter: dark, white SemiBold 20, max 720 wide.</summary>
        private static NarrationCard BuildNarration(RectTransform layer, TMP_FontAsset semiBold)
        {
            Image card = UiFactory.MakeImage("NarrationCard", layer, new Vector2(0.5f, 0.835f), new Vector2(0.5f, 0.835f), Night);
            UiFactory.MakeRound(card);
            RectTransform rect = card.rectTransform;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(720f, 60f);
            CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            TextMeshProUGUI label = UiFactory.MakeLabel("Text", rect, Vector2.zero, Vector2.one, 20f, TextAlignmentOptions.Center, Color.white);
            if (semiBold != null) label.font = semiBold;
            label.rectTransform.offsetMin = new Vector2(18f, 10f);
            label.rectTransform.offsetMax = new Vector2(-18f, -10f);
            label.raycastTarget = false;

            NarrationCard view = card.gameObject.AddComponent<NarrationCard>();
            SerializedWire.Set(view, "group", group);
            SerializedWire.Set(view, "card", rect);
            SerializedWire.Set(view, "label", label);
            return view;
        }

        /// <summary>A choice row (badge, label, cost chip, lock, reason, hover fill/bar) or the small dark pill.</summary>
        private static BubbleChoiceRow BuildRow(RectTransform parent, PlaceholderSprites art, TMP_FontAsset semiBold, TMP_FontAsset bold, bool pill)
        {
            Image bg = UiFactory.MakeImage(pill ? "PillTemplate" : "RowTemplate", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), pill ? Night : new Color(1f, 1f, 1f, 0.9f), raycast: true);
            UiFactory.MakeRound(bg);
            RectTransform rect = bg.rectTransform;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(360f, 46f);
            Outline outline = bg.gameObject.AddComponent<Outline>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            CanvasGroup group = bg.gameObject.AddComponent<CanvasGroup>();
            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;

            Image hoverFill = UiFactory.MakeImage("HoverFill", rect, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.15f));
            UiFactory.MakeRound(hoverFill);
            hoverFill.enabled = false;
            Image hoverBar = UiFactory.MakeImage("HoverBar", rect, new Vector2(0f, 0.15f), new Vector2(0f, 0.85f), Ink);
            hoverBar.rectTransform.sizeDelta = new Vector2(3f, 0f);
            hoverBar.rectTransform.anchoredPosition = new Vector2(3f, 0f);
            hoverBar.enabled = false;

            TextMeshProUGUI badge = null;
            if (!pill)
            {
                Image circle = UiFactory.MakeImage("Badge", rect, new Vector2(0f, 1f), new Vector2(0f, 1f), Ink, sprite: art.Dot);
                circle.rectTransform.sizeDelta = new Vector2(28f, 28f);
                circle.rectTransform.anchoredPosition = new Vector2(24f, -23f);
                badge = UiFactory.MakeLabel("Number", circle.transform, Vector2.zero, Vector2.one, 16f, TextAlignmentOptions.Center, Color.white);
                if (bold != null) badge.font = bold;
            }

            TextMeshProUGUI label = UiFactory.MakeLabel("Label", rect, new Vector2(0f, 1f), new Vector2(1f, 1f), pill ? 16f : 18f, pill ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft, pill ? Color.white : Ink);
            if (semiBold != null) label.font = semiBold;
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.offsetMin = new Vector2(pill ? 14f : 48f, pill ? -34f : -46f);
            label.rectTransform.offsetMax = new Vector2(pill ? -14f : -92f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            TextMeshProUGUI cost = UiFactory.MakeLabel("Cost", rect, new Vector2(1f, 1f), new Vector2(1f, 1f), 16f, TextAlignmentOptions.MidlineRight, new Color(0.7f, 0.16f, 0.12f, 1f));
            if (bold != null) cost.font = bold;
            cost.rectTransform.pivot = new Vector2(1f, 1f);
            cost.rectTransform.sizeDelta = new Vector2(pill ? 0f : 84f, 46f);
            cost.rectTransform.anchoredPosition = new Vector2(-12f, 0f);
            if (pill) cost.gameObject.SetActive(false);

            Image lockIcon = UiFactory.MakeImage("Lock", rect, new Vector2(1f, 1f), new Vector2(1f, 1f), Ink, sprite: art.Lock);
            lockIcon.rectTransform.sizeDelta = new Vector2(16f, 20f);
            lockIcon.rectTransform.anchoredPosition = new Vector2(-24f, -23f);
            lockIcon.gameObject.SetActive(false);

            TextMeshProUGUI reason = UiFactory.MakeLabel("Reason", rect, new Vector2(0f, 0f), new Vector2(1f, 0f), 16f, TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.2f, 0.12f, 1f));
            reason.rectTransform.pivot = new Vector2(0.5f, 0f);
            reason.rectTransform.offsetMin = new Vector2(48f, 4f);
            reason.rectTransform.offsetMax = new Vector2(-12f, 26f);
            reason.textWrappingMode = TextWrappingModes.NoWrap;
            reason.overflowMode = TextOverflowModes.Ellipsis;
            reason.gameObject.SetActive(false);

            BubbleChoiceRow row = bg.gameObject.AddComponent<BubbleChoiceRow>();
            SerializedWire.Set(row, "button", button);
            SerializedWire.Set(row, "group", group);
            if (badge != null) SerializedWire.Set(row, "badge", badge);
            SerializedWire.Set(row, "label", label);
            SerializedWire.Set(row, "cost", cost);
            SerializedWire.Set(row, "lockIcon", lockIcon.gameObject);
            SerializedWire.Set(row, "reason", reason);
            SerializedWire.Set(row, "hoverFill", hoverFill);
            SerializedWire.Set(row, "hoverBar", hoverBar);
            if (pill) SerializedWire.SetVector2(row, "heights", new Vector2(34f, 34f));
            bg.gameObject.SetActive(false);
            return row;
        }

        /// <summary>A point-sized rect at the parent's bottom-left (pivot bottom-left), positioned by code.</summary>
        private static RectTransform Rect(string name, Transform parent)
        {
            RectTransform rect = UiFactory.MakeRect(name, parent, Vector2.zero, Vector2.zero);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        /// <summary>Stretches an image over its parent, shifted by (dx, dy) and grown by grow px on every side.</summary>
        private static Image Stretch(Image image, float dx, float dy, float grow)
        {
            RectTransform r = image.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(dx - grow, dy - grow);
            r.offsetMax = new Vector2(dx + grow, dy + grow);
            return image;
        }
    }
}
