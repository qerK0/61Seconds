using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Placement;
using SixtyOne.QTE;
using SixtyOne.Building;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builds the full gameplay canvas for the test scene and wires every UI component. The playfield is the
    /// whole screen: compact overlays only (timer top-centre, loop + notes top-left, a slim collapsible bag /
    /// key-item / stamina column on the right, the choice panel sliding in from the bottom), all styled by UITheme.
    /// </summary>
    public static class TestSceneUi
    {
        private static Color Panel = new Color(0.06f, 0.065f, 0.08f, 0.86f);
        private static Color ButtonColor = new Color(0.12f, 0.13f, 0.16f, 0.88f);
        private static Color Accent = new Color(1f, 0.76f, 0.29f, 1f);
        private static Color Danger = new Color(1f, 0.42f, 0.36f, 1f);
        private static Color Muted = new Color(0.78f, 0.78f, 0.8f, 1f);
        private static UITheme theme;

        /// <summary>Left edge (viewport x) of the right HUD column; RoomCameraFraming keeps rooms left of it.</summary>
        public const float ColumnLeft = 0.9f;

        /// <summary>Builds the canvas and returns the ChoicePanel (the NodePlayer presenter).</summary>
        public static ChoicePanel Build(GameManager gameManager, NodePlayer nodePlayer, GameContent content, Camera camera, PlaceholderSprites art,
            SurvivalController survival, RecipeBook recipes, SixtyOne.Prep.PrepPhase prep, int bagSlots, Item[] keyItems, UITheme uiTheme,
            out LetterboxBars letterbox, out QTEView qteView, out CanvasGroup transitionOverlay)
        {
            theme = uiTheme;
            UiFactory.Theme = uiTheme;
            UiFactory.Rounded = art.Rounded;
            if (theme != null)
            {
                Panel = theme.Panel;
                ButtonColor = theme.Button;
                Accent = theme.Accent;
                Danger = theme.Danger;
                Muted = theme.MutedText;
            }

            var canvasGo = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            // First child: everything else draws on top, so the bars only crop the world.
            letterbox = BuildLetterbox(root);

            BuildTimerBar(root, nodePlayer);
            BuildBuildingTimer(root, survival);
            BuildPrepCountdown(root, prep, survival);
            BuildLoopCounter(root, gameManager);
            BuildRightColumn(root, gameManager, content, art, survival, recipes, bagSlots, keyItems);
            ChoicePanel choicePanel = BuildChoicePanel(root, art);
            BuildAfadPopup(root);
            BuildJournal(root, gameManager, content);
            qteView = BuildQteView(root, art);
            // After the QTE panel so miss / timeout messages draw on top of it.
            BuildToast(root);
            BuildDeathCard(root, gameManager, content, art);
            BuildPoisonOverlay(root);
            transitionOverlay = BuildTransitionOverlay(root);
            BuildRewindFx(root, camera);
            BuildClickFeedback(root, art);
            EnforceReadableText(canvasGo);
            UiFactory.Theme = null;
            return choicePanel;
        }

        /// <summary>Nothing below 16 px at 1080p, auto-sized labels included; faux bold becomes the real Bold weight.</summary>
        private static void EnforceReadableText(GameObject canvas)
        {
            foreach (TextMeshProUGUI label in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.fontSize = Mathf.Max(16f, label.fontSize);
                if (label.enableAutoSizing) label.fontSizeMin = Mathf.Max(16f, label.fontSizeMin);
                if (theme != null && theme.Bold != null && (label.fontStyle & FontStyles.Bold) != 0)
                {
                    label.fontStyle &= ~FontStyles.Bold;
                    label.font = theme.Bold;
                }
            }
        }

        private static ChoicePanel BuildChoicePanel(Transform root, PlaceholderSprites art)
        {
            // Bottom overlay, 66% wide, slides (anchoredPosition.y) in from below the screen.
            Image bg = UiFactory.MakePanel("ChoicePanel", root, new Vector2(0.17f, 0f), new Vector2(0.83f, 0f), Panel, raycast: true);
            RectTransform panelRect = bg.rectTransform;
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(0f, 330f);
            panelRect.anchoredPosition = new Vector2(0f, -400f);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);

            // Accent hairline on top of the panel.
            Image line = UiFactory.MakeImage("AccentLine", bg.transform, new Vector2(0.03f, 1f), new Vector2(0.97f, 1f), new Color(Accent.r, Accent.g, Accent.b, 0.55f));
            line.rectTransform.sizeDelta = new Vector2(0f, 2f);
            line.rectTransform.anchoredPosition = new Vector2(0f, -1f);

            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), TextRole.Speaker, TextAlignmentOptions.MidlineLeft);
            Inset(title.rectTransform, 28f, 10f, 28f, 46f);
            TextMeshProUGUI body = UiFactory.MakeLabel("Body", bg.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), TextRole.Body, TextAlignmentOptions.TopLeft);
            Inset(body.rectTransform, 28f, 58f, 28f, 96f);
            body.enableAutoSizing = true;
            body.fontSizeMin = 18f;
            body.fontSizeMax = theme != null ? theme.BodySize : 25f;
            TypewriterText typewriter = body.gameObject.AddComponent<TypewriterText>();
            typewriter.Configure(body, theme != null ? theme.TypewriterCharsPerSecond : 90f);

            RectTransform container = UiFactory.MakeRect("Choices", bg.transform, Vector2.zero, Vector2.one);
            container.offsetMin = new Vector2(24f, 16f);
            container.offsetMax = new Vector2(-24f, -164f);
            GridLayoutGroup grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(604f, theme != null ? theme.ChoiceHeight : 56f);
            grid.spacing = new Vector2(theme != null ? theme.Gap : 10f, theme != null ? theme.Gap : 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;

            Button button = UiFactory.MakeButton("ChoiceButtonTemplate", container, Vector2.zero, Vector2.one, ButtonColor, 23f, out TextMeshProUGUI label);
            if (theme != null) theme.Apply(label, TextRole.Choice);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0.72f, 1f);
            labelRect.offsetMin = new Vector2(26f, 0f);
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16f;
            label.fontSizeMax = theme != null ? theme.ChoiceSize : 23f;

            Image accentBar = UiFactory.MakeImage("AccentBar", button.transform, new Vector2(0f, 0.2f), new Vector2(0f, 0.8f), Accent);
            accentBar.rectTransform.sizeDelta = new Vector2(theme != null ? theme.AccentBarWidth : 5f, 0f);
            accentBar.rectTransform.anchoredPosition = new Vector2(12f, 0f);

            // Locked: label on the top line (ChoiceButton), lock + reason on a full-width bottom line.
            Image lockIcon = UiFactory.MakeImage("Lock", button.transform, new Vector2(0f, 0.08f), new Vector2(0f, 0.44f), Muted, sprite: art.Lock);
            lockIcon.rectTransform.sizeDelta = new Vector2(16f, 0f);
            lockIcon.rectTransform.anchoredPosition = new Vector2(34f, 0f);
            TextMeshProUGUI reason = UiFactory.MakeLabel("Reason", button.transform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), TextRole.Small, TextAlignmentOptions.MidlineLeft);
            reason.color = new Color(1f, 0.72f, 0.45f, 1f);
            reason.rectTransform.offsetMin = new Vector2(48f, 0f);
            reason.rectTransform.offsetMax = new Vector2(-12f, 0f);
            reason.fontSize = 16f;
            reason.textWrappingMode = TextWrappingModes.NoWrap;
            reason.overflowMode = TextOverflowModes.Ellipsis;
            TextMeshProUGUI cost = UiFactory.MakeLabel("Cost", button.transform, new Vector2(0.72f, 0.45f), new Vector2(1f, 1f), TextRole.Small, TextAlignmentOptions.MidlineRight);
            cost.color = Danger;
            cost.rectTransform.offsetMax = new Vector2(-12f, 0f);

            ChoiceButton choiceButton = button.gameObject.AddComponent<ChoiceButton>();
            SerializedWire.Set(choiceButton, "button", button);
            SerializedWire.Set(choiceButton, "label", label);
            SerializedWire.Set(choiceButton, "lockIcon", lockIcon.gameObject);
            SerializedWire.Set(choiceButton, "reasonText", reason);
            SerializedWire.Set(choiceButton, "costText", cost);
            SerializedWire.Set(choiceButton, "accentBar", accentBar);
            SerializedWire.SetColor(choiceButton, "accentColor", Accent);
            button.gameObject.SetActive(false);

            ChoicePanel panel = bg.gameObject.AddComponent<ChoicePanel>();
            SerializedWire.Set(panel, "group", group);
            SerializedWire.Set(panel, "panel", panelRect);
            SerializedWire.Set(panel, "titleText", title);
            SerializedWire.Set(panel, "bodyText", body);
            SerializedWire.Set(panel, "typewriter", typewriter);
            SerializedWire.Set(panel, "buttonContainer", container);
            SerializedWire.Set(panel, "buttonTemplate", choiceButton);
            if (theme != null) SerializedWire.SetFloat(panel, "slideSeconds", theme.SlideSeconds);
            return panel;
        }

        /// <summary>Top-anchored strip inside a panel: left / top / right insets and a fixed height (px).</summary>
        private static void Inset(RectTransform rect, float left, float top, float right, float height)
        {
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void BuildTimerBar(Transform root, NodePlayer nodePlayer)
        {
            Image bg = UiFactory.MakeImage("TimerBar", root, new Vector2(0.42f, 0.885f), new Vector2(0.58f, 0.891f), new Color(0f, 0f, 0f, 0.5f));
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            Image fill = UiFactory.MakeImage("Fill", bg.transform, Vector2.zero, Vector2.one, Accent);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);

            TimerBar bar = bg.gameObject.AddComponent<TimerBar>();
            SerializedWire.Set(bar, "nodePlayer", nodePlayer);
            SerializedWire.Set(bar, "group", group);
            SerializedWire.Set(bar, "fill", fill.rectTransform);
            SerializedWire.Set(bar, "fillImage", fill);
        }

        private static LetterboxBars BuildLetterbox(Transform root)
        {
            RectTransform rect = UiFactory.MakeRect("Letterbox", root, Vector2.zero, Vector2.one);
            Image top = UiFactory.MakeImage("Top", rect, new Vector2(0f, 1f), Vector2.one, Color.black);
            Image bottom = UiFactory.MakeImage("Bottom", rect, Vector2.zero, new Vector2(1f, 0f), Color.black);
            LetterboxBars bars = rect.gameObject.AddComponent<LetterboxBars>();
            SerializedWire.Set(bars, "top", top.rectTransform);
            SerializedWire.Set(bars, "bottom", bottom.rectTransform);
            return bars;
        }

        /// <summary>Compact top-centre timer: small all-caps caption over Bold tabular digits.</summary>
        private static void BuildBuildingTimer(Transform root, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("BuildingTimer", root, new Vector2(0.42f, 0.895f), new Vector2(0.58f, 0.995f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, 0.72f), new Vector2(1f, 1f), TextRole.Label, TextAlignmentOptions.Center);
            SerializedWire.SetTextKey(caption.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.BuildingTimerLabel);
            TextMeshProUGUI label = UiFactory.MakeLabel("Time", rect, Vector2.zero, new Vector2(1f, 0.74f), TextRole.Timer, TextAlignmentOptions.Center);
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(1f, 0.1f), new Vector2(1.5f, 0.7f), 28f, TextAlignmentOptions.Left, Danger, bold: true);

            BuildingTimerDisplay display = rect.gameObject.AddComponent<BuildingTimerDisplay>();
            SerializedWire.Set(display, "survival", survival);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "spentLabel", spent);
            SerializedWire.Set(display, "group", group);
        }

        /// <summary>Pre-quake prep countdown in the building timer's spot (the building timer is hidden until the quake).</summary>
        private static void BuildPrepCountdown(Transform root, SixtyOne.Prep.PrepPhase prep, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("PrepCountdown", root, new Vector2(0.42f, 0.895f), new Vector2(0.58f, 0.995f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, 0.72f), new Vector2(1f, 1f), TextRole.Label, TextAlignmentOptions.Center);
            caption.color = Accent;
            SerializedWire.SetTextKey(caption.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.PrepTimerLabel);
            TextMeshProUGUI label = UiFactory.MakeLabel("Time", rect, Vector2.zero, new Vector2(1f, 0.74f), TextRole.Timer, TextAlignmentOptions.Center);
            label.color = Accent;
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(1f, 0.1f), new Vector2(1.5f, 0.7f), 28f, TextAlignmentOptions.Left, Danger, bold: true);

            PrepCountdownDisplay display = rect.gameObject.AddComponent<PrepCountdownDisplay>();
            SerializedWire.Set(display, "prep", prep);
            SerializedWire.Set(display, "group", group);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "spentLabel", spent);
            if (survival != null && survival.Config != null) SerializedWire.SetInt(display, "tickFrom", survival.Config.PrepTickFrom);
        }

        private static void BuildLoopCounter(Transform root, GameManager gameManager)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel("LoopCounter", root, new Vector2(0.012f, 0.93f), new Vector2(0.2f, 0.99f), 30f, TextAlignmentOptions.MidlineLeft, Accent, bold: true);
            LoopCounterUI counter = label.gameObject.AddComponent<LoopCounterUI>();
            SerializedWire.Set(counter, "gameManager", gameManager);
            SerializedWire.Set(counter, "label", label);
        }

        /// <summary>
        /// Slim right column (collapsible to a tab): key items, the bag slots with Drop / Tie rope, and a thin
        /// stamina bar at the bottom.
        /// </summary>
        private static void BuildRightColumn(Transform root, GameManager gameManager, GameContent content, PlaceholderSprites art, SurvivalController survival,
            RecipeBook recipes, int slots, Item[] keyItems)
        {
            RectTransform columnRoot = UiFactory.MakeRect("RightColumn", root, Vector2.zero, Vector2.one);
            Image column = UiFactory.MakePanel("Column", columnRoot, new Vector2(ColumnLeft, 0.13f), new Vector2(0.995f, 0.985f), Panel, raycast: true);
            Transform c = column.transform;

            BuildKeyItemHud(c, root, gameManager, keyItems, art);
            BuildBag(c, gameManager, content, art, survival, recipes, slots);
            BuildStamina(c, survival);

            Button toggle = UiFactory.MakeButton("Toggle", columnRoot, new Vector2(ColumnLeft - 0.022f, 0.925f), new Vector2(ColumnLeft - 0.004f, 0.985f), Panel, 22f, out TextMeshProUGUI toggleLabel);
            CollapsiblePanel collapsible = columnRoot.gameObject.AddComponent<CollapsiblePanel>();
            SerializedWire.Set(collapsible, "content", column.rectTransform);
            SerializedWire.Set(collapsible, "toggle", toggle);
            SerializedWire.Set(collapsible, "toggleLabel", toggleLabel);
        }

        private static void BuildStamina(Transform column, SurvivalController survival)
        {
            TextMeshProUGUI caption = UiFactory.MakeLabel("StaminaCaption", column, new Vector2(0.08f, 0.012f), new Vector2(0.92f, 0.045f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(caption.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.StaminaLabel);
            Image bg = UiFactory.MakeImage("Stamina", column, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.058f), new Color(1f, 1f, 1f, 0.12f));
            Image fill = UiFactory.MakeImage("Fill", bg.transform, Vector2.zero, Vector2.one, theme != null ? theme.Positive : new Color(0.4f, 0.85f, 0.45f, 1f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);

            StaminaBar bar = bg.gameObject.AddComponent<StaminaBar>();
            SerializedWire.Set(bar, "survival", survival);
            SerializedWire.Set(bar, "fill", fill.rectTransform);
            SerializedWire.Set(bar, "fillImage", fill);
        }

        private static void BuildBag(Transform column, GameManager gameManager, GameContent content, PlaceholderSprites art, SurvivalController survival, RecipeBook recipes, int slots)
        {
            RectTransform bagRect = UiFactory.MakeRect("Bag", column, new Vector2(0f, 0.07f), new Vector2(1f, 0.83f));
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bagRect, new Vector2(0.08f, 0.93f), new Vector2(0.7f, 1f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            title.color = Accent;
            SerializedWire.SetTextKey(title.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.BagTitle);
            TextMeshProUGUI count = UiFactory.MakeLabel("Count", bagRect, new Vector2(0.6f, 0.93f), new Vector2(0.92f, 1f), TextRole.Small, TextAlignmentOptions.MidlineRight);
            count.color = Muted;

            // Slots fill the area between the title and the Drop / Rope buttons (any slot count).
            slots = Mathf.Max(1, slots);
            const float slotsTop = 0.915f;
            const float slotsBottom = 0.16f;
            float pitch = (slotsTop - slotsBottom) / slots;
            var icons = new Object[slots];
            var labels = new Object[slots];
            var backgrounds = new Object[slots];
            var buttons = new Object[slots];
            for (int i = 0; i < slots; i++)
            {
                float top = slotsTop - i * pitch;
                Image slot = UiFactory.MakePanel("Slot" + i, bagRect, new Vector2(0.06f, top - pitch * 0.88f), new Vector2(0.94f, top), new Color(1f, 1f, 1f, 0.08f), raycast: true);
                Button slotButton = slot.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slot;
                buttons[i] = slotButton;
                Image icon = UiFactory.MakeImage("Icon", slot.transform, new Vector2(0.03f, 0.06f), new Vector2(0.3f, 0.94f), Color.white, sprite: art.Dot);
                icon.preserveAspect = true;
                icon.enabled = false;
                TextMeshProUGUI name = UiFactory.MakeLabel("Name", slot.transform, new Vector2(0.3f, 0f), new Vector2(0.97f, 1f), TextRole.Small, TextAlignmentOptions.MidlineLeft);
                name.enableAutoSizing = true;
                name.fontSizeMin = 16f;
                name.fontSizeMax = 18f;
                icons[i] = icon;
                backgrounds[i] = slot;
                labels[i] = name;
            }

            BagUI bag = bagRect.gameObject.AddComponent<BagUI>();
            SerializedWire.Set(bag, "gameManager", gameManager);
            SerializedWire.Set(bag, "content", content);
            SerializedWire.SetArray(bag, "slotIcons", icons);
            SerializedWire.SetArray(bag, "slotLabels", labels);
            SerializedWire.SetArray(bag, "slotBackgrounds", backgrounds);
            SerializedWire.SetArray(bag, "slotButtons", buttons);
            SerializedWire.Set(bag, "survival", survival);
            SerializedWire.Set(bag, "recipes", recipes);

            Button drop = UiFactory.MakeButton("Drop", bagRect, new Vector2(0.06f, 0.01f), new Vector2(0.94f, 0.07f), new Color(0.5f, 0.22f, 0.2f, 0.95f), 18f, out TextMeshProUGUI dropLabel);
            SerializedWire.SetTextKey(dropLabel.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.Drop);
            drop.gameObject.SetActive(false);
            Button rope = UiFactory.MakeButton("TieRope", bagRect, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.14f), new Color(0.22f, 0.42f, 0.28f, 0.95f), 18f, out TextMeshProUGUI ropeLabel);
            SerializedWire.SetTextKey(ropeLabel.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.RopeTie);
            rope.gameObject.SetActive(false);
            SerializedWire.Set(bag, "dropButton", drop);
            SerializedWire.Set(bag, "ropeButton", rope);
            SerializedWire.Set(bag, "countLabel", count);
        }

        /// <summary>Top of the right column: one icon per key item (no bag slot), dim until owned, with a hover tooltip.</summary>
        private static void BuildKeyItemHud(Transform column, Transform root, GameManager gameManager, Item[] keyItems, PlaceholderSprites art)
        {
            RectTransform rect = UiFactory.MakeRect("KeyItems", column, new Vector2(0f, 0.85f), new Vector2(1f, 1f));
            TextMeshProUGUI header = UiFactory.MakeLabel("Header", rect, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.97f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(header.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.KeyItemsTitle);
            TextMeshProUGUI tooltip = UiFactory.MakeLabel("Tooltip", root, new Vector2(0.6f, 0.86f), new Vector2(ColumnLeft - 0.03f, 0.91f), TextRole.Small, TextAlignmentOptions.MidlineRight);
            int count = keyItems != null ? keyItems.Length : 0;
            var slots = new System.Collections.Generic.List<System.Collections.Generic.IDictionary<string, Object>>();
            for (int i = 0; i < count; i++)
            {
                float x0 = 0.06f + i * (0.88f / Mathf.Max(1, count));
                float x1 = x0 + 0.88f / Mathf.Max(1, count) - 0.04f;
                Image frame = UiFactory.MakePanel("Frame_" + keyItems[i].Id, rect, new Vector2(x0, 0.04f), new Vector2(x1, 0.7f), new Color(1f, 1f, 1f, 0.12f));
                Image icon = UiFactory.MakeImage("Icon", frame.transform, new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.96f), Color.white, raycast: true, sprite: art.Dot);
                icon.preserveAspect = true;
                TextMeshProUGUI name = UiFactory.MakeLabel("Name", frame.transform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.4f), TextRole.Small, TextAlignmentOptions.Center);
                name.enableAutoSizing = true;
                name.fontSizeMin = 16f;
                name.fontSizeMax = 16f;
                SerializedWire.SetTextKey(name.gameObject.AddComponent<LocalizedText>(), "key", keyItems[i].NameKey.Key);
                slots.Add(new System.Collections.Generic.Dictionary<string, Object> { { "item", keyItems[i] }, { "icon", icon }, { "frame", frame } });
            }

            KeyItemCornerHUD hud = rect.gameObject.AddComponent<KeyItemCornerHUD>();
            SerializedWire.Set(hud, "gameManager", gameManager);
            SerializedWire.Set(hud, "tooltip", tooltip);
            SerializedWire.SetStructArray(hud, "slots", slots);
        }

        private static void BuildPoisonOverlay(Transform root)
        {
            Image overlay = UiFactory.MakeImage("PoisonOverlay", root, Vector2.zero, Vector2.one, new Color(0.02f, 0.05f, 0.02f, 0f));
            PoisonOverlay poison = overlay.gameObject.AddComponent<PoisonOverlay>();
            SerializedWire.Set(poison, "overlay", overlay);
        }

        private static void BuildClickFeedback(Transform root, PlaceholderSprites art)
        {
            RectTransform rect = UiFactory.MakeRect("ClickFeedback", root, Vector2.zero, Vector2.one);
            Image ring = UiFactory.MakeImage("Ring", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 1f, 1f, 0.85f), sprite: art.Ring);
            ring.rectTransform.sizeDelta = new Vector2(64f, 32f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ClickFeedback feedback = rect.gameObject.AddComponent<ClickFeedback>();
            SerializedWire.Set(feedback, "ring", ring);
        }

        /// <summary>Center panel for WASD QTEs: title, prompt, big key, pips, countdowns, on-screen W/A/S/D.</summary>
        private static QTEView BuildQteView(Transform root, PlaceholderSprites art)
        {
            Image bg = UiFactory.MakePanel("QTE", root, new Vector2(0.3f, 0.42f), new Vector2(0.7f, 0.83f), new Color(0.06f, 0.06f, 0.08f, 0.95f), raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.03f, 0.88f), new Vector2(0.97f, 0.98f), 30f, TextAlignmentOptions.Center, Accent, bold: true);
            TextMeshProUGUI prompt = UiFactory.MakeLabel("Prompt", bg.transform, new Vector2(0.03f, 0.79f), new Vector2(0.97f, 0.88f), 22f, TextAlignmentOptions.Center, Color.white);
            TextMeshProUGUI target = UiFactory.MakeLabel("TargetKey", bg.transform, new Vector2(0.38f, 0.52f), new Vector2(0.62f, 0.79f), 110f, TextAlignmentOptions.Center, Color.white, bold: true);

            const int maxPips = 8;
            var pips = new Object[maxPips];
            for (int i = 0; i < maxPips; i++)
            {
                float x = 0.14f + i * 0.0925f;
                pips[i] = UiFactory.MakeImage("Pip" + i, bg.transform, new Vector2(x, 0.465f), new Vector2(x + 0.07f, 0.505f), new Color(1f, 1f, 1f, 0.2f));
            }

            Image keyBack = UiFactory.MakeImage("KeyCountdownBack", bg.transform, new Vector2(0.1f, 0.42f), new Vector2(0.9f, 0.445f), new Color(1f, 1f, 1f, 0.1f));
            Image keyFill = UiFactory.MakeImage("Fill", keyBack.transform, Vector2.zero, Vector2.one, Accent, sprite: art.Dot);
            keyFill.type = Image.Type.Filled;
            keyFill.fillMethod = Image.FillMethod.Horizontal;
            Image totalBack = UiFactory.MakeImage("TotalCountdownBack", bg.transform, new Vector2(0.1f, 0.395f), new Vector2(0.9f, 0.408f), new Color(1f, 1f, 1f, 0.1f));
            Image totalFill = UiFactory.MakeImage("Fill", totalBack.transform, Vector2.zero, Vector2.one, new Color(1f, 0.35f, 0.25f, 1f), sprite: art.Dot);
            totalFill.type = Image.Type.Filled;
            totalFill.fillMethod = Image.FillMethod.Horizontal;

            // W on top, A S D below (keyboard layout); also clickable for touch.
            Vector2[] min = { new Vector2(0.41f, 0.22f), new Vector2(0.27f, 0.05f), new Vector2(0.41f, 0.05f), new Vector2(0.55f, 0.05f) };
            var keyButtons = new Object[4];
            for (int i = 0; i < 4; i++)
            {
                Button key = UiFactory.MakeButton("Key" + QTESequence.AllKeys[i], bg.transform, min[i], min[i] + new Vector2(0.18f, 0.15f), ButtonColor, 36f, out TextMeshProUGUI label);
                label.text = QTESequence.AllKeys[i].ToString();
                keyButtons[i] = key;
            }

            TextMeshProUGUI hint = UiFactory.MakeLabel("Hint", bg.transform, new Vector2(0.03f, 0.37f), new Vector2(0.97f, 0.395f), 16f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
            Image flash = UiFactory.MakeImage("Flash", bg.transform, Vector2.zero, Vector2.one, Color.clear);

            QTEView view = bg.gameObject.AddComponent<QTEView>();
            SerializedWire.Set(view, "group", group);
            SerializedWire.Set(view, "panel", bg.rectTransform);
            SerializedWire.Set(view, "title", title);
            SerializedWire.Set(view, "prompt", prompt);
            SerializedWire.Set(view, "targetKey", target);
            SerializedWire.Set(view, "hint", hint);
            SerializedWire.SetArray(view, "pips", pips);
            SerializedWire.Set(view, "keyCountdown", keyFill);
            SerializedWire.Set(view, "totalCountdown", totalFill);
            SerializedWire.SetArray(view, "keyButtons", keyButtons);
            SerializedWire.Set(view, "flash", flash);
            return view;
        }

        /// <summary>Full-screen fade used by RoomTransitionController (never blocks clicks; input is locked by InputGate).</summary>
        private static CanvasGroup BuildTransitionOverlay(Transform root)
        {
            Image overlay = UiFactory.MakeImage("RoomTransition", root, Vector2.zero, Vector2.one, new Color(0.02f, 0.02f, 0.03f, 1f));
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static void BuildToast(Transform root)
        {
            Image bg = UiFactory.MakePanel("Toast", root, new Vector2(0.27f, 0.46f), new Vector2(0.73f, 0.545f), new Color(0.05f, 0.05f, 0.07f, 0.88f));
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", bg.transform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 30f, TextAlignmentOptions.Center, Color.white, bold: true);
            MessageToast toast = bg.gameObject.AddComponent<MessageToast>();
            SerializedWire.Set(toast, "group", group);
            SerializedWire.Set(toast, "label", label);
        }

        private static void BuildAfadPopup(Transform root)
        {
            // Bottom strip (where choice buttons go; placement nodes have none) so it never covers the room or its anchors.
            Image bg = UiFactory.MakePanel("AfadAlert", root, new Vector2(0.24f, 0.62f), new Vector2(0.76f, 0.82f), new Color(0.85f, 0.05f, 0.05f, 0.95f), raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            Button dismiss = bg.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = bg;
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.95f), 46f, TextAlignmentOptions.Center, Color.white, bold: true);
            TextMeshProUGUI body = UiFactory.MakeLabel("Body", bg.transform, new Vector2(0.03f, 0.06f), new Vector2(0.97f, 0.58f), 28f, TextAlignmentOptions.Center, Color.white);

            AfadAlertPopup popup = bg.gameObject.AddComponent<AfadAlertPopup>();
            SerializedWire.Set(popup, "group", group);
            SerializedWire.Set(popup, "background", bg);
            SerializedWire.Set(popup, "title", title);
            SerializedWire.Set(popup, "body", body);
            SerializedWire.Set(popup, "dismissButton", dismiss);
        }

        private static void BuildJournal(Transform root, GameManager gameManager, GameContent content)
        {
            RectTransform journalRoot = UiFactory.MakeRect("TipJournal", root, Vector2.zero, Vector2.one);
            Button open = UiFactory.MakeButton("OpenButton", journalRoot, new Vector2(0.012f, 0.875f), new Vector2(0.12f, 0.925f), ButtonColor, 20f, out TextMeshProUGUI openLabel);

            Image panelBg = UiFactory.MakePanel("Panel", journalRoot, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), new Color(0.05f, 0.05f, 0.08f, 0.97f), raycast: true);
            CanvasGroup panel = UiFactory.MakeGroup(panelBg.gameObject);
            TextMeshProUGUI header = UiFactory.MakeLabel("Header", panelBg.transform, new Vector2(0.04f, 0.88f), new Vector2(0.96f, 0.98f), 34f, TextAlignmentOptions.Center, Accent, bold: true);
            TextMeshProUGUI body = UiFactory.MakeLabel("Body", panelBg.transform, new Vector2(0.04f, 0.13f), new Vector2(0.96f, 0.87f), 22f, TextAlignmentOptions.TopLeft, Color.white);
            body.enableAutoSizing = true;
            body.fontSizeMin = 11f;
            body.fontSizeMax = 22f;
            Button close = UiFactory.MakeButton("CloseButton", panelBg.transform, new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.1f), ButtonColor, 24f, out TextMeshProUGUI closeLabel);

            TipJournal journal = journalRoot.gameObject.AddComponent<TipJournal>();
            SerializedWire.Set(journal, "gameManager", gameManager);
            SerializedWire.Set(journal, "content", content);
            SerializedWire.Set(journal, "openButton", open);
            SerializedWire.Set(journal, "openLabel", openLabel);
            SerializedWire.Set(journal, "panel", panel);
            SerializedWire.Set(journal, "header", header);
            SerializedWire.Set(journal, "body", body);
            SerializedWire.Set(journal, "closeButton", close);
            SerializedWire.Set(journal, "closeLabel", closeLabel);
        }

        private static void BuildDeathCard(Transform root, GameManager gameManager, GameContent content, PlaceholderSprites art)
        {
            Image bg = UiFactory.MakeImage("DeathCard", root, Vector2.zero, Vector2.one, Color.black, raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI deathText = UiFactory.MakeLabel("DeathText", bg.transform, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.8f), 46f, TextAlignmentOptions.Center, Color.white, bold: true);

            Image tipRoot = UiFactory.MakePanel("Tip", bg.transform, new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.52f), new Color(1f, 1f, 1f, 0.07f));
            TextMeshProUGUI tipHeader = UiFactory.MakeLabel("Header", tipRoot.transform, new Vector2(0.03f, 0.74f), new Vector2(0.97f, 0.97f), 28f, TextAlignmentOptions.Center, Accent, bold: true);
            TextMeshProUGUI tipText = UiFactory.MakeLabel("Text", tipRoot.transform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.74f), 26f, TextAlignmentOptions.Center, Color.white);
            tipText.enableAutoSizing = true;
            tipText.fontSizeMin = 14f;
            tipText.fontSizeMax = 26f;

            // "Keşke yanıma alsaydım" block: icon + "Eksik: <item>".
            RectTransform missing = UiFactory.MakeRect("MissingItem", bg.transform, new Vector2(0.3f, 0.81f), new Vector2(0.7f, 0.93f));
            Image missingIcon = UiFactory.MakeImage("Icon", missing, new Vector2(0f, 0f), new Vector2(0.18f, 1f), Accent, sprite: art.Dot);
            TextMeshProUGUI missingLabel = UiFactory.MakeLabel("Label", missing, new Vector2(0.2f, 0f), new Vector2(1f, 1f), 34f, TextAlignmentOptions.Left, Accent, bold: true);
            missing.gameObject.SetActive(false);

            Button rewind = UiFactory.MakeButton("RewindButton", bg.transform, new Vector2(0.4f, 0.06f), new Vector2(0.6f, 0.16f), new Color(0.8f, 0.12f, 0.12f, 1f), 32f, out TextMeshProUGUI rewindLabel);

            DeathCard card = bg.gameObject.AddComponent<DeathCard>();
            SerializedWire.Set(card, "gameManager", gameManager);
            SerializedWire.Set(card, "content", content);
            SerializedWire.Set(card, "group", group);
            SerializedWire.Set(card, "deathText", deathText);
            SerializedWire.Set(card, "tipRoot", tipRoot.gameObject);
            SerializedWire.Set(card, "tipHeader", tipHeader);
            SerializedWire.Set(card, "tipText", tipText);
            SerializedWire.Set(card, "missingRoot", missing.gameObject);
            SerializedWire.Set(card, "missingIcon", missingIcon);
            SerializedWire.Set(card, "missingLabel", missingLabel);
            SerializedWire.Set(card, "rewindButton", rewind);
            SerializedWire.SetFloat(card, "showDelaySeconds", 0.5f);
            SerializedWire.Set(card, "rewindLabel", rewindLabel);
        }

        private static void BuildRewindFx(Transform root, Camera camera)
        {
            RectTransform rect = UiFactory.MakeRect("RewindFX", root, Vector2.zero, Vector2.one);
            RawImage raw = rect.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.color = Color.white;
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);

            RewindFX fx = rect.gameObject.AddComponent<RewindFX>();
            SerializedWire.Set(fx, "group", group);
            SerializedWire.Set(fx, "scanlines", raw);
            SerializedWire.Set(fx, "shakeTarget", camera.transform);
        }
    }
}
