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
        public const float ColumnLeft = 0.835f;

        /// <summary>Builds the canvas and returns the DialoguePresenter (speech bubbles + narration card).</summary>
        public static DialoguePresenter Build(GameManager gameManager, NodePlayer nodePlayer, GameContent content, Camera camera, PlaceholderSprites art,
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
            BuildPanicMeter(root, survival);
            BuildPrepCountdown(root, prep, survival);
            BuildLoopCounter(root, gameManager);
            BuildRightColumn(root, gameManager, content, art, survival, recipes, bagSlots, keyItems, nodePlayer);
            BuildLootWindow(root, gameManager, content, art);
            TestSceneCraftNotebook.Build(root, gameManager, content, survival, recipes, art, theme);
            BuildSearchRing(root, camera, art);
            // Speech bubbles + the top narration card (after the HUD so it reads the panic meter / bag column rects).
            DialoguePresenter presenter = TestSceneDialogueUi.Build(root, art, theme, survival);
            BuildAfadPopup(root);
            BuildJournal(root, gameManager, content);
            qteView = BuildQteView(root, art);
            // After the QTE panel so miss / timeout messages draw on top of it.
            BuildToast(root);
            BuildDeathCard(root, gameManager, content, art);
            BuildPoisonOverlay(root, survival);
            transitionOverlay = BuildTransitionOverlay(root);
            BuildRewindFx(root, camera);
            BuildClickFeedback(root, art);
            // The end screen ("Simülasyon sona erdi") covers the HUD once the epilogue was read.
            TestSceneEnding.Build(root, theme);
            TestSceneEndScreen.Build(root, content);
            // The floor-transition info card, then the intro briefing (up from the first frame of a new run).
            TestSceneIntroCards.BuildInfoCards(root, nodePlayer);
            TestSceneIntroCards.BuildIntro(root);
            // Last: the ESC pause overlay draws over everything.
            BuildPauseMenu(root);
            EnforceReadableText(canvasGo);
            UiFactory.Theme = null;
            return presenter;
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
            LedClock clock = BuildLedFace(rect, out TextMeshProUGUI label);
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(0.86f, 0.1f), new Vector2(1.4f, 0.7f), 28f, TextAlignmentOptions.Left, Danger, bold: true);

            BuildingTimerDisplay display = rect.gameObject.AddComponent<BuildingTimerDisplay>();
            SerializedWire.Set(display, "survival", survival);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "clock", clock);
            SerializedWire.Set(display, "spentLabel", spent);
            SerializedWire.Set(display, "group", group);
        }

        /// <summary>"Panik" meter under the timer (and the node timer bar): caption left, bar right, threshold tick.</summary>
        private static void BuildPanicMeter(Transform root, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("PanicMeter", root, new Vector2(0.42f, 0.848f), new Vector2(0.58f, 0.88f));
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, 0f), new Vector2(0.3f, 1f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(caption.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.PanicLabel);
            Image bg = UiFactory.MakeImage("Bar", rect, new Vector2(0.32f, 0.3f), new Vector2(1f, 0.7f), new Color(1f, 1f, 1f, 0.12f));
            Image fill = UiFactory.MakeImage("Fill", bg.transform, Vector2.zero, new Vector2(0f, 1f), Danger);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            Image tick = UiFactory.MakeImage("Threshold", bg.transform, new Vector2(0.7f, -0.25f), new Vector2(0.7f, 1.25f), new Color(1f, 1f, 1f, 0.7f));
            tick.rectTransform.sizeDelta = new Vector2(2f, 0f);

            PanicMeter meter = bg.gameObject.AddComponent<PanicMeter>();
            SerializedWire.Set(meter, "survival", survival);
            SerializedWire.Set(meter, "fill", fill.rectTransform);
            SerializedWire.Set(meter, "fillImage", fill);
            SerializedWire.Set(meter, "thresholdTick", tick.rectTransform);
        }

        /// <summary>Pre-quake prep countdown in the building timer's spot (the building timer is hidden until the quake).</summary>
        private static void BuildPrepCountdown(Transform root, SixtyOne.Prep.PrepPhase prep, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("PrepCountdown", root, new Vector2(0.42f, 0.895f), new Vector2(0.58f, 0.995f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, 0.72f), new Vector2(1f, 1f), TextRole.Label, TextAlignmentOptions.Center);
            caption.color = Accent;
            SerializedWire.SetTextKey(caption.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.PrepTimerLabel);
            LedClock clock = BuildLedFace(rect, out TextMeshProUGUI label);
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(0.86f, 0.1f), new Vector2(1.4f, 0.7f), 28f, TextAlignmentOptions.Left, Danger, bold: true);

            PrepCountdownDisplay display = rect.gameObject.AddComponent<PrepCountdownDisplay>();
            SerializedWire.Set(display, "clock", clock);
            SerializedWire.Set(display, "prep", prep);
            SerializedWire.Set(display, "group", group);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "spentLabel", spent);
            if (survival != null && survival.Config != null) SerializedWire.SetInt(display, "tickFrom", survival.Config.PrepTickFrom);
        }

        /// <summary>
        /// Digital alarm-clock face under a timer caption: a small near-black, green-tinted rounded panel with a thin
        /// dark border, an unlit "88:88" ghost and the lit LED digits (glow set up at runtime by LedClock).
        /// </summary>
        private static LedClock BuildLedFace(RectTransform rect, out TextMeshProUGUI digits)
        {
            Image face = UiFactory.MakePanel("LedFace", rect, new Vector2(0.14f, 0f), new Vector2(0.86f, 0.72f), new Color(0.03f, 0.05f, 0.035f, 0.94f));
            Outline border = face.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0f, 0f, 0f, 0.85f);
            border.effectDistance = new Vector2(2f, -2f);

            TMP_FontAsset led = theme != null ? theme.Led : null;
            TextMeshProUGUI ghost = UiFactory.MakeLabel("Ghost", face.transform, Vector2.zero, Vector2.one, 58f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.12f));
            digits = UiFactory.MakeLabel("Time", face.transform, Vector2.zero, Vector2.one, 58f, TextAlignmentOptions.Center, Color.white);
            foreach (TextMeshProUGUI l in new[] { ghost, digits })
            {
                if (led != null) l.font = led;
                l.textWrappingMode = TextWrappingModes.NoWrap;
                l.overflowMode = TextOverflowModes.Overflow;
                l.characterSpacing = 4f;
            }

            ghost.text = "88:88";
            digits.text = "00:00";
            LedClock clock = face.gameObject.AddComponent<LedClock>();
            SerializedWire.Set(clock, "digits", digits);
            SerializedWire.Set(clock, "ghost", ghost);
            return clock;
        }

        /// <summary>ESC pause: dim overlay, centred serif panel (Devam Et / Ayarlar / Ana Menü / Oyundan Çık), settings, confirm.</summary>
        private static void BuildPauseMenu(Transform root)
        {
            var click = UnityEditor.AssetDatabase.LoadAssetAtPath<SixtyOne.Audio.AudioCue>("Assets/_Game/Data/Audio/cue_choice_click.asset");
            Image dim = UiFactory.MakeImage("PauseMenu", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.8f), raycast: true);
            CanvasGroup overlay = dim.gameObject.AddComponent<CanvasGroup>();

            Image panel = UiFactory.MakePanel("Panel", dim.transform, new Vector2(0.38f, 0.26f), new Vector2(0.62f, 0.74f), MenuUi.PanelColor, raycast: true);
            CanvasGroup buttons = panel.gameObject.AddComponent<CanvasGroup>();
            MenuUi.KeyLabel("Title", panel.transform, new Vector2(0.08f, 0.8f), new Vector2(0.92f, 0.95f), UiKeys.PauseTitle, 44f, TextAlignmentOptions.Center, true, true);
            Button resume = MenuUi.Button("Resume", panel.transform, new Vector2(0.12f, 0.61f), new Vector2(0.88f, 0.75f), UiKeys.PauseResume);
            Button settings = MenuUi.Button("Settings", panel.transform, new Vector2(0.12f, 0.43f), new Vector2(0.88f, 0.57f), UiKeys.PauseSettings);
            Button mainMenu = MenuUi.Button("MainMenu", panel.transform, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.39f), UiKeys.PauseMainMenu);
            Button quit = MenuUi.Button("Quit", panel.transform, new Vector2(0.12f, 0.07f), new Vector2(0.88f, 0.21f), UiKeys.PauseQuit);

            SettingsPanel settingsPanel = MenuUi.Settings(dim.transform, click);
            CanvasGroup confirm = MenuUi.Confirm(dim.transform, out Button yes, out Button no);

            PauseMenu menu = dim.gameObject.AddComponent<PauseMenu>();
            SerializedWire.Set(menu, "overlay", overlay);
            SerializedWire.Set(menu, "mainButtons", buttons);
            SerializedWire.Set(menu, "resume", resume);
            SerializedWire.Set(menu, "settings", settings);
            SerializedWire.Set(menu, "mainMenu", mainMenu);
            SerializedWire.Set(menu, "quit", quit);
            SerializedWire.Set(menu, "settingsPanel", settingsPanel);
            SerializedWire.Set(menu, "confirm", confirm);
            SerializedWire.Set(menu, "confirmYes", yes);
            SerializedWire.Set(menu, "confirmNo", no);
            SerializedWire.Set(menu, "clickCue", click);
            overlay.alpha = 0f;
            overlay.interactable = false;
            overlay.blocksRaycasts = false;
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
            RecipeBook recipes, int slots, Item[] keyItems, NodePlayer nodePlayer)
        {
            RectTransform columnRoot = UiFactory.MakeRect("RightColumn", root, Vector2.zero, Vector2.one);
            Image column = UiFactory.MakePanel("Column", columnRoot, new Vector2(ColumnLeft, 0.13f), new Vector2(0.995f, 0.985f), Panel, raycast: true);
            Transform c = column.transform;

            // Flashlight and whistle live in the backpack's side pockets; the corner HUD only shows any other key items.
            var otherKeyItems = new System.Collections.Generic.List<Item>();
            if (keyItems != null)
            {
                foreach (Item item in keyItems)
                {
                    if (item != null && item.Id != "flashlight" && item.Id != "whistle") otherKeyItems.Add(item);
                }
            }

            if (otherKeyItems.Count > 0) BuildKeyItemHud(c, root, gameManager, otherKeyItems.ToArray(), art);
            BuildBag(c, gameManager, content, art, survival, recipes, slots, keyItems, nodePlayer);
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

        private static void BuildBag(Transform column, GameManager gameManager, GameContent content, PlaceholderSprites art, SurvivalController survival, RecipeBook recipes, int slots,
            Item[] keyItems, NodePlayer nodePlayer)
        {
            RectTransform bagRect = UiFactory.MakeRect("Bag", column, new Vector2(0f, 0.07f), new Vector2(1f, 0.985f));
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bagRect, new Vector2(0.06f, 0.96f), new Vector2(0.7f, 1f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            title.color = Accent;
            SerializedWire.SetTextKey(title.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.BagTitle);
            TextMeshProUGUI count = UiFactory.MakeLabel("Count", bagRect, new Vector2(0.6f, 0.96f), new Vector2(0.94f, 1f), TextRole.Small, TextAlignmentOptions.MidlineRight);
            count.color = Muted;

            // The red pixel-art backpack (Art/uı/examples.png) at an integer x3 scale: the 8 bag slots sit on its drawn
            // 2x4 main compartment, the two side pockets hold the key items (left = flashlight, right = whistle).
            // Item icons stay smooth at ~80% of a cell; names show under the backpack on hover.
            UiSkinArt.Sprites skin = UiSkinArt.Import();
            const int scale = UiSkinArt.Scale;
            Vector2Int packSize = UiSkinArt.BackpackSize;
            Image frame = UiFactory.MakeImage("Backpack", bagRect, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.95f), Color.white);
            frame.rectTransform.pivot = new Vector2(0.5f, 1f);
            frame.rectTransform.sizeDelta = new Vector2(packSize.x * scale, packSize.y * scale);
            if (skin.Backpack != null)
            {
                frame.sprite = skin.Backpack;
                frame.preserveAspect = false;
            }
            else
            {
                frame.color = new Color(0.7f, 0.35f, 0.27f, 1f);
            }

            RectTransform names = UiFactory.MakeRect("SlotNames", bagRect, new Vector2(0f, 0.95f), new Vector2(1f, 0.95f));
            names.pivot = new Vector2(0.5f, 1f);
            names.anchoredPosition = new Vector2(0f, -packSize.y * scale - 4f);
            names.sizeDelta = new Vector2(0f, 48f);

            float cell = UiSkinArt.CellSize * scale;
            slots = Mathf.Clamp(slots, 1, UiSkinArt.CellColumns * UiSkinArt.CellRows);
            var icons = new Object[slots];
            var labels = new Object[slots];
            var backgrounds = new Object[slots];
            var buttons = new Object[slots];
            for (int i = 0; i < slots; i++)
            {
                Vector2Int origin = UiSkinArt.CellOrigin(i % UiSkinArt.CellColumns, i / UiSkinArt.CellColumns);
                Image slot = UiFactory.MakeImage("Slot" + i, frame.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), skin.Cell != null ? Color.white : new Color(0.25f, 0.32f, 0.45f, 1f), raycast: true);
                slot.rectTransform.pivot = new Vector2(0f, 1f);
                slot.rectTransform.sizeDelta = new Vector2(cell, cell);
                slot.rectTransform.anchoredPosition = new Vector2(origin.x * scale, -origin.y * scale);
                if (skin.Cell != null)
                {
                    slot.sprite = skin.Cell;
                    slot.preserveAspect = false;
                }

                Button slotButton = slot.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slot;
                // Hover = second highlight (the combine target while a slot is selected); BagUI tints the selected one.
                ColorBlock colors = slotButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.62f, 1f, 0.95f, 1f);
                colors.pressedColor = new Color(1f, 0.85f, 0.45f, 1f);
                colors.selectedColor = Color.white;
                colors.fadeDuration = 0.05f;
                slotButton.colors = colors;
                buttons[i] = slotButton;

                Image icon = UiFactory.MakeImage("Icon", slot.transform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Color.white, sprite: null);
                icon.preserveAspect = true;
                icon.enabled = false;

                // Name under the backpack, revealed while the slot is hovered (BagUI still writes it).
                TextMeshProUGUI name = UiFactory.MakeLabel("Name" + i, names, Vector2.zero, Vector2.one, TextRole.Small, TextAlignmentOptions.Top);
                name.enableAutoSizing = true;
                name.fontSizeMin = 16f;
                name.fontSizeMax = 18f;
                CanvasGroup nameGroup = name.gameObject.AddComponent<CanvasGroup>();
                nameGroup.alpha = 0f;
                nameGroup.blocksRaycasts = false;
                SerializedWire.Set(slot.gameObject.AddComponent<HoverReveal>(), "target", nameGroup);

                icons[i] = icon;
                backgrounds[i] = slot;
                labels[i] = name;
            }

            BuildPocket(frame.transform, "PocketLeft", UiSkinArt.LeftPocket, UiSkinArt.LeftPocketIconCentre, keyItems, "flashlight", nodePlayer, names);
            BuildPocket(frame.transform, "PocketRight", UiSkinArt.RightPocket, UiSkinArt.RightPocketIconCentre, keyItems, "whistle", nodePlayer, names);

            BagUI bag = bagRect.gameObject.AddComponent<BagUI>();
            SerializedWire.Set(bag, "gameManager", gameManager);
            SerializedWire.Set(bag, "content", content);
            SerializedWire.SetArray(bag, "slotIcons", icons);
            SerializedWire.SetArray(bag, "slotLabels", labels);
            SerializedWire.SetArray(bag, "slotBackgrounds", backgrounds);
            SerializedWire.SetArray(bag, "slotButtons", buttons);
            SerializedWire.Set(bag, "survival", survival);
            SerializedWire.Set(bag, "recipes", recipes);
            SerializedWire.SetColor(bag, "selectedColor", new Color(1f, 0.84f, 0.3f, 1f));

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

        /// <summary>
        /// A backpack side pocket for one key item: an invisible click area over the pocket art and the item icon
        /// (~80% of a bag cell) at the pocket's centre; the name shows under the backpack on hover.
        /// </summary>
        private static void BuildPocket(Transform frame, string objectName, RectInt area, Vector2 iconCentre, Item[] keyItems, string itemId, NodePlayer nodePlayer,
            RectTransform names)
        {
            Item item = null;
            if (keyItems != null)
            {
                foreach (Item k in keyItems)
                {
                    if (k != null && k.Id == itemId) item = k;
                }
            }

            if (item == null) return;
            const int scale = UiSkinArt.Scale;
            Image hit = UiFactory.MakeImage(objectName, frame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Color(1f, 1f, 1f, 0f), raycast: true);
            hit.rectTransform.pivot = new Vector2(0f, 1f);
            hit.rectTransform.anchoredPosition = new Vector2(area.x * scale, -area.y * scale);
            hit.rectTransform.sizeDelta = new Vector2(area.width * scale, area.height * scale);
            Button button = hit.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            float iconSize = UiSkinArt.CellSize * scale * 0.8f;
            Image icon = UiFactory.MakeImage("Icon", frame, new Vector2(0f, 1f), new Vector2(0f, 1f), Color.white, sprite: item.UiIcon);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(iconCentre.x * scale, -iconCentre.y * scale);
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.name = objectName + "Icon";

            TextMeshProUGUI name = UiFactory.MakeLabel("PocketName_" + itemId, names, Vector2.zero, Vector2.one, TextRole.Small, TextAlignmentOptions.Top);
            SerializedWire.SetTextKey(name.gameObject.AddComponent<LocalizedText>(), "key", item.NameKey.Key);
            CanvasGroup nameGroup = name.gameObject.AddComponent<CanvasGroup>();
            nameGroup.alpha = 0f;
            nameGroup.blocksRaycasts = false;
            SerializedWire.Set(hit.gameObject.AddComponent<HoverReveal>(), "target", nameGroup);

            KeyItemPocket pocket = hit.gameObject.AddComponent<KeyItemPocket>();
            SerializedWire.Set(pocket, "item", item);
            SerializedWire.Set(pocket, "icon", icon);
            SerializedWire.Set(pocket, "button", button);
            SerializedWire.Set(pocket, "nodePlayer", nodePlayer);
            SerializedWire.Set(pocket, "clickCue", UnityEditor.AssetDatabase.LoadAssetAtPath<SixtyOne.Audio.AudioCue>("Assets/_Game/Data/Audio/cue_choice_click.asset"));
        }

        /// <summary>Top of the right column: one icon per key item (no bag slot), dim until owned, with a hover tooltip.</summary>
        private static void BuildKeyItemHud(Transform column, Transform root, GameManager gameManager, Item[] keyItems, PlaceholderSprites art)
        {
            RectTransform rect = UiFactory.MakeRect("KeyItems", column, new Vector2(0f, 0.85f), new Vector2(1f, 1f));
            TextMeshProUGUI header = UiFactory.MakeLabel("Header", rect, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.97f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(header.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.KeyItemsTitle);
            TextMeshProUGUI tooltip = UiFactory.MakeLabel("Tooltip", root, new Vector2(0.6f, 0.86f), new Vector2(ColumnLeft - 0.03f, 0.91f), TextRole.Small, TextAlignmentOptions.MidlineRight);
            int count = keyItems != null ? keyItems.Length : 0;
            UiSkinArt.Sprites keySkin = UiSkinArt.Import();
            var slots = new System.Collections.Generic.List<System.Collections.Generic.IDictionary<string, Object>>();
            for (int i = 0; i < count; i++)
            {
                float x0 = 0.06f + i * (0.88f / Mathf.Max(1, count));
                float x1 = x0 + 0.88f / Mathf.Max(1, count) - 0.04f;
                Image frame = UiFactory.MakePanel("Frame_" + keyItems[i].Id, rect, new Vector2(x0, 0.04f), new Vector2(x1, 0.7f), new Color(1f, 1f, 1f, 0.12f));
                if (keySkin.KeyPanel != null)
                {
                    // Small green-framed pixel panel from the inventory sheet (x3, crisp).
                    frame.sprite = keySkin.KeyPanel;
                    frame.type = Image.Type.Sliced;
                    frame.pixelsPerUnitMultiplier = 1f / UiSkinArt.Scale;
                }

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
            if (keySkin.KeyPanel != null)
            {
                // The pixel frame multiplies the tint: white when owned, dimmed when missing.
                SerializedWire.SetColor(hud, "ownedFrameColor", Color.white);
                SerializedWire.SetColor(hud, "missingFrameColor", new Color(0.5f, 0.5f, 0.55f, 0.85f));
            }
        }

        private static void BuildPoisonOverlay(Transform root, SurvivalController survival)
        {
            Image overlay = UiFactory.MakeImage("PoisonOverlay", root, Vector2.zero, Vector2.one, new Color(0.02f, 0.05f, 0.02f, 0f));
            PoisonOverlay poison = overlay.gameObject.AddComponent<PoisonOverlay>();
            SerializedWire.Set(poison, "overlay", overlay);

            // "Maske takılı": the worn wet-cloth icon left of the panic meter, drawn over the gas darkening.
            Image chip = UiFactory.MakePanel("MaskIndicator", root, new Vector2(0.325f, 0.842f), new Vector2(0.415f, 0.886f), Panel);
            CanvasGroup group = UiFactory.MakeGroup(chip.gameObject);
            UiFactory.MakeImage("Icon", chip.transform, new Vector2(0.03f, 0.08f), new Vector2(0.3f, 0.92f), Color.white, sprite: ItemIconArt.MaskIcon());
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", chip.transform, new Vector2(0.32f, 0f), new Vector2(0.98f, 1f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.MaskWorn);
            MaskIndicator indicator = chip.gameObject.AddComponent<MaskIndicator>();
            SerializedWire.Set(indicator, "survival", survival);
            SerializedWire.Set(indicator, "group", group);
        }

        /// <summary>
        /// Container window just left of the bag column: title (container name or "Ödünç al"), 2x4 slots with the bag's
        /// cell art, "Boş" when empty, "Hepsini al" and "Kapat".
        /// </summary>
        private static void BuildLootWindow(Transform root, GameManager gameManager, GameContent content, PlaceholderSprites art)
        {
            Image panel = UiFactory.MakePanel("LootWindow", root, new Vector2(ColumnLeft - 0.165f, 0.42f), new Vector2(ColumnLeft - 0.008f, 0.93f), Panel, raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(panel.gameObject);
            Transform p = panel.transform;
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", p, new Vector2(0.06f, 0.9f), new Vector2(0.94f, 0.98f), TextRole.Label, TextAlignmentOptions.MidlineLeft);
            title.color = Accent;
            TextMeshProUGUI subtitle = UiFactory.MakeLabel("Subtitle", p, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.9f), TextRole.Small, TextAlignmentOptions.MidlineLeft);
            subtitle.color = Muted;

            UiSkinArt.Sprites skin = UiSkinArt.Import();
            const int columns = 2;
            const int rows = 4;
            var buttons = new Object[columns * rows];
            var icons = new Object[columns * rows];
            var labels = new Object[columns * rows];
            for (int i = 0; i < columns * rows; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float x0 = 0.06f + col * 0.45f;
                float y1 = 0.82f - row * 0.155f;
                Image slot = UiFactory.MakeImage("Slot" + i, p, new Vector2(x0, y1 - 0.145f), new Vector2(x0 + 0.42f, y1), skin.Cell != null ? Color.white : new Color(0.25f, 0.32f, 0.45f, 1f),
                    raycast: true, sprite: skin.Cell);
                if (skin.Cell != null)
                {
                    slot.type = Image.Type.Sliced;
                    slot.pixelsPerUnitMultiplier = 1f / UiSkinArt.Scale;
                }

                Button button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = slot;
                Image icon = UiFactory.MakeImage("Icon", slot.transform, new Vector2(0.08f, 0.3f), new Vector2(0.92f, 0.95f), Color.white, sprite: null);
                icon.preserveAspect = true;
                icon.enabled = false;
                TextMeshProUGUI name = UiFactory.MakeLabel("Name", slot.transform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.3f), TextRole.Small, TextAlignmentOptions.Center);
                name.enableAutoSizing = true;
                name.fontSizeMin = 16f;
                name.fontSizeMax = 18f;
                buttons[i] = button;
                icons[i] = icon;
                labels[i] = name;
            }

            TextMeshProUGUI empty = UiFactory.MakeLabel("Empty", p, new Vector2(0.06f, 0.45f), new Vector2(0.94f, 0.55f), TextRole.Body, TextAlignmentOptions.Center);
            empty.color = Muted;
            SerializedWire.SetTextKey(empty.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.LootEmpty);

            Button takeAll = UiFactory.MakeButton("TakeAll", p, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.18f), new Color(0.22f, 0.42f, 0.28f, 0.95f), 18f, out TextMeshProUGUI takeLabel);
            SerializedWire.SetTextKey(takeLabel.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.LootTakeAll);
            Button close = UiFactory.MakeButton("Close", p, new Vector2(0.06f, 0.015f), new Vector2(0.94f, 0.09f), ButtonColor, 18f, out TextMeshProUGUI closeLabel);
            SerializedWire.SetTextKey(closeLabel.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.LootClose);

            LootWindow window = panel.gameObject.AddComponent<LootWindow>();
            SerializedWire.Set(window, "gameManager", gameManager);
            SerializedWire.Set(window, "content", content);
            SerializedWire.Set(window, "group", group);
            SerializedWire.Set(window, "title", title);
            SerializedWire.Set(window, "subtitle", subtitle);
            SerializedWire.Set(window, "emptyLabel", empty);
            SerializedWire.SetArray(window, "slotButtons", buttons);
            SerializedWire.SetArray(window, "slotIcons", icons);
            SerializedWire.SetArray(window, "slotLabels", labels);
            SerializedWire.Set(window, "takeAllButton", takeAll);
            SerializedWire.Set(window, "closeButton", close);
        }

        /// <summary>Radial ring + "Aranıyor…" over the container being searched (never blocks clicks).</summary>
        private static void BuildSearchRing(Transform root, Camera camera, PlaceholderSprites art)
        {
            RectTransform holder = UiFactory.MakeRect("LootSearch", root, Vector2.zero, Vector2.one);
            RectTransform ringRoot = UiFactory.MakeRect("Ring", holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            ringRoot.sizeDelta = new Vector2(72f, 72f);
            Image back = UiFactory.MakeImage("Back", ringRoot, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.45f), sprite: art.Ring);
            back.raycastTarget = false;
            Image fill = UiFactory.MakeImage("Fill", ringRoot, Vector2.zero, Vector2.one, Accent, sprite: art.Ring);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", ringRoot, new Vector2(-1f, -0.7f), new Vector2(2f, -0.05f), TextRole.Small, TextAlignmentOptions.Center);
            label.raycastTarget = false;
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.LootSearching);

            LootSearchRing ring = holder.gameObject.AddComponent<LootSearchRing>();
            SerializedWire.Set(ring, "worldCamera", camera);
            SerializedWire.Set(ring, "root", ringRoot);
            SerializedWire.Set(ring, "fill", fill);
            SerializedWire.Set(ring, "label", label);
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
