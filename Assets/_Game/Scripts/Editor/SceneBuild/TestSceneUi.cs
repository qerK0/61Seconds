using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Placement;
using SixtyOne.Building;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>Builds the full gameplay canvas for the test scene and wires every UI component.</summary>
    public static class TestSceneUi
    {
        private static readonly Color Panel = new Color(0f, 0f, 0f, 0.78f);
        private static readonly Color ButtonColor = new Color(0.18f, 0.22f, 0.32f, 1f);
        private static readonly Color Accent = new Color(1f, 0.82f, 0.2f, 1f);

        /// <summary>Builds the canvas and returns the ChoicePanel (the NodePlayer presenter).</summary>
        public static ChoicePanel Build(GameManager gameManager, NodePlayer nodePlayer, GameContent content, Camera camera, PlaceholderSprites art,
            SurvivalController survival, RecipeBook recipes, SixtyOne.Prep.PrepPhase prep, out LetterboxBars letterbox, out FusePuzzlePanel fusePanel)
        {
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

            ChoicePanel choicePanel = BuildChoicePanel(root, art);
            BuildTimerBar(root, nodePlayer);
            BuildBuildingTimer(root, survival);
            BuildPrepCountdown(root, prep, survival);
            BuildLoopCounter(root, gameManager);
            BuildBag(root, gameManager, content, art, survival, recipes);
            BuildStamina(root, survival);
            BuildAfadPopup(root);
            BuildJournal(root, gameManager, content);
            fusePanel = BuildFusePanel(root);
            // After the fuse panel so "wrong pattern" messages draw on top of it.
            BuildToast(root);
            BuildDeathCard(root, gameManager, content, art);
            BuildPoisonOverlay(root);
            BuildRewindFx(root, camera);
            BuildClickFeedback(root, art);
            return choicePanel;
        }

        private static ChoicePanel BuildChoicePanel(Transform root, PlaceholderSprites art)
        {
            Image bg = UiFactory.MakeImage("ChoicePanel", root, new Vector2(0.03f, 0.01f), new Vector2(0.85f, 0.40f), Panel, raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.02f, 0.83f), new Vector2(0.98f, 0.98f), 34f, TextAlignmentOptions.Left, Accent, bold: true);
            TextMeshProUGUI body = UiFactory.MakeLabel("Body", bg.transform, new Vector2(0.02f, 0.60f), new Vector2(0.98f, 0.83f), 26f, TextAlignmentOptions.TopLeft, Color.white);

            RectTransform container = UiFactory.MakeRect("Choices", bg.transform, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.58f));
            GridLayoutGroup grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(740f, 54f);
            grid.spacing = new Vector2(16f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;

            Button button = UiFactory.MakeButton("ChoiceButtonTemplate", container, Vector2.zero, Vector2.one, ButtonColor, 24f, out TextMeshProUGUI label);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.08f, 0f);
            labelRect.anchorMax = new Vector2(0.68f, 1f);
            label.alignment = TextAlignmentOptions.Left;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 24f;

            Image lockIcon = UiFactory.MakeImage("Lock", button.transform, new Vector2(0.01f, 0.15f), new Vector2(0.06f, 0.85f), Accent, sprite: art.Lock);
            TextMeshProUGUI reason = UiFactory.MakeLabel("Reason", button.transform, new Vector2(0.68f, 0f), new Vector2(0.99f, 0.6f), 16f, TextAlignmentOptions.Right, new Color(1f, 0.7f, 0.4f, 1f));
            TextMeshProUGUI cost = UiFactory.MakeLabel("Cost", button.transform, new Vector2(0.80f, 0.55f), new Vector2(0.99f, 1f), 16f, TextAlignmentOptions.Right, new Color(1f, 0.55f, 0.45f, 1f));
            reason.enableAutoSizing = true;
            reason.fontSizeMin = 10f;
            reason.fontSizeMax = 16f;

            ChoiceButton choiceButton = button.gameObject.AddComponent<ChoiceButton>();
            SerializedWire.Set(choiceButton, "button", button);
            SerializedWire.Set(choiceButton, "label", label);
            SerializedWire.Set(choiceButton, "lockIcon", lockIcon.gameObject);
            SerializedWire.Set(choiceButton, "reasonText", reason);
            SerializedWire.Set(choiceButton, "costText", cost);
            button.gameObject.SetActive(false);

            ChoicePanel panel = bg.gameObject.AddComponent<ChoicePanel>();
            SerializedWire.Set(panel, "group", group);
            SerializedWire.Set(panel, "titleText", title);
            SerializedWire.Set(panel, "bodyText", body);
            SerializedWire.Set(panel, "buttonContainer", container);
            SerializedWire.Set(panel, "buttonTemplate", choiceButton);
            return panel;
        }

        private static void BuildTimerBar(Transform root, NodePlayer nodePlayer)
        {
            Image bg = UiFactory.MakeImage("TimerBar", root, new Vector2(0.3f, 0.955f), new Vector2(0.7f, 0.985f), new Color(0f, 0f, 0f, 0.6f));
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

        private static void BuildBuildingTimer(Transform root, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("BuildingTimer", root, new Vector2(0.4f, 0.86f), new Vector2(0.6f, 0.95f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI label = UiFactory.MakeLabel("Time", rect, Vector2.zero, Vector2.one, 72f, TextAlignmentOptions.Center, Color.white, bold: true);
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(1f, 0.1f), new Vector2(1.5f, 0.9f), 34f, TextAlignmentOptions.Left, new Color(1f, 0.45f, 0.35f, 1f), bold: true);
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, -0.35f), new Vector2(1f, 0f), 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
            LocalizedText localized = caption.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.BuildingTimerLabel);

            BuildingTimerDisplay display = rect.gameObject.AddComponent<BuildingTimerDisplay>();
            SerializedWire.Set(display, "survival", survival);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "spentLabel", spent);
            SerializedWire.Set(display, "group", group);
        }

        /// <summary>Pre-quake prep countdown in the building timer's spot (the building timer is hidden until the quake).</summary>
        private static void BuildPrepCountdown(Transform root, SixtyOne.Prep.PrepPhase prep, SurvivalController survival)
        {
            RectTransform rect = UiFactory.MakeRect("PrepCountdown", root, new Vector2(0.4f, 0.86f), new Vector2(0.6f, 0.95f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI label = UiFactory.MakeLabel("Time", rect, Vector2.zero, Vector2.one, 80f, TextAlignmentOptions.Center, Accent, bold: true);
            TextMeshProUGUI spent = UiFactory.MakeLabel("Spent", rect, new Vector2(1f, 0.1f), new Vector2(1.5f, 0.9f), 34f, TextAlignmentOptions.Left, new Color(1f, 0.45f, 0.35f, 1f), bold: true);
            TextMeshProUGUI caption = UiFactory.MakeLabel("Caption", rect, new Vector2(0f, -0.35f), new Vector2(1f, 0f), 22f, TextAlignmentOptions.Center, Accent);
            LocalizedText localized = caption.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.PrepTimerLabel);

            PrepCountdownDisplay display = rect.gameObject.AddComponent<PrepCountdownDisplay>();
            SerializedWire.Set(display, "prep", prep);
            SerializedWire.Set(display, "group", group);
            SerializedWire.Set(display, "label", label);
            SerializedWire.Set(display, "spentLabel", spent);
            if (survival != null && survival.Config != null) SerializedWire.SetInt(display, "tickFrom", survival.Config.PrepTickFrom);
        }

        private static FusePuzzlePanel BuildFusePanel(Transform root)
        {
            Image bg = UiFactory.MakeImage("FusePuzzle", root, new Vector2(0.3f, 0.53f), new Vector2(0.7f, 0.81f), new Color(0.08f, 0.08f, 0.1f, 0.95f), raycast: true);
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.03f, 0.8f), new Vector2(0.97f, 0.97f), 30f, TextAlignmentOptions.Center, Accent, bold: true);
            LocalizedText titleText = title.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(titleText, "key", UiKeys.FuseTitle);

            var buttons = new Object[4];
            var labels = new Object[4];
            for (int i = 0; i < 4; i++)
            {
                float x = 0.06f + i * 0.23f;
                Button sw = UiFactory.MakeButton("Switch" + (i + 1), bg.transform, new Vector2(x, 0.32f), new Vector2(x + 0.19f, 0.74f), ButtonColor, 44f, out TextMeshProUGUI label);
                TextMeshProUGUI number = UiFactory.MakeLabel("Number", sw.transform, new Vector2(0f, -0.3f), new Vector2(1f, 0f), 20f, TextAlignmentOptions.Center, Color.white);
                number.text = (i + 1).ToString();
                buttons[i] = sw;
                labels[i] = label;
            }

            Button tryButton = UiFactory.MakeButton("Try", bg.transform, new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.18f), new Color(0.8f, 0.4f, 0.1f, 1f), 26f, out TextMeshProUGUI tryLabel);
            LocalizedText tryText = tryLabel.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(tryText, "key", UiKeys.FuseTry);

            FusePuzzlePanel panel = bg.gameObject.AddComponent<FusePuzzlePanel>();
            SerializedWire.Set(panel, "group", group);
            SerializedWire.SetArray(panel, "switchButtons", buttons);
            SerializedWire.SetArray(panel, "switchLabels", labels);
            SerializedWire.Set(panel, "tryButton", tryButton);
            return panel;
        }

        private static void BuildStamina(Transform root, SurvivalController survival)
        {
            Image bg = UiFactory.MakeImage("Stamina", root, new Vector2(0.865f, 0.375f), new Vector2(0.995f, 0.405f), new Color(0f, 0f, 0f, 0.6f));
            Image fill = UiFactory.MakeImage("Fill", bg.transform, Vector2.zero, Vector2.one, new Color(0.4f, 0.85f, 0.45f, 1f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", bg.transform, Vector2.zero, Vector2.one, 18f, TextAlignmentOptions.Center, Color.white, bold: true);
            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.StaminaLabel);

            StaminaBar bar = bg.gameObject.AddComponent<StaminaBar>();
            SerializedWire.Set(bar, "survival", survival);
            SerializedWire.Set(bar, "fill", fill.rectTransform);
            SerializedWire.Set(bar, "fillImage", fill);
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

        private static void BuildLoopCounter(Transform root, GameManager gameManager)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel("LoopCounter", root, new Vector2(0.01f, 0.91f), new Vector2(0.26f, 0.99f), 44f, TextAlignmentOptions.Left, Accent, bold: true);
            LoopCounterUI counter = label.gameObject.AddComponent<LoopCounterUI>();
            SerializedWire.Set(counter, "gameManager", gameManager);
            SerializedWire.Set(counter, "label", label);
        }

        private static void BuildBag(Transform root, GameManager gameManager, GameContent content, PlaceholderSprites art, SurvivalController survival, RecipeBook recipes)
        {
            Image bg = UiFactory.MakeImage("Bag", root, new Vector2(0.865f, 0.42f), new Vector2(0.995f, 0.90f), new Color(0f, 0f, 0f, 0.6f));
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 1f), 26f, TextAlignmentOptions.Center, Accent, bold: true);
            LocalizedText localized = title.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.BagTitle);

            var icons = new Object[Inventory.DefaultCapacity];
            var labels = new Object[Inventory.DefaultCapacity];
            var backgrounds = new Object[Inventory.DefaultCapacity];
            var buttons = new Object[Inventory.DefaultCapacity];
            for (int i = 0; i < Inventory.DefaultCapacity; i++)
            {
                float top = 0.86f - i * 0.215f;
                Image slot = UiFactory.MakeImage("Slot" + i, bg.transform, new Vector2(0.05f, top - 0.2f), new Vector2(0.95f, top), new Color(1f, 1f, 1f, 0.12f), raycast: true);
                Button slotButton = slot.gameObject.AddComponent<Button>();
                slotButton.targetGraphic = slot;
                buttons[i] = slotButton;
                Image icon = UiFactory.MakeImage("Icon", slot.transform, new Vector2(0.03f, 0.1f), new Vector2(0.33f, 0.9f), Color.white, sprite: art.Dot);
                icon.enabled = false;
                TextMeshProUGUI name = UiFactory.MakeLabel("Name", slot.transform, new Vector2(0.36f, 0f), new Vector2(0.98f, 1f), 20f, TextAlignmentOptions.Left, Color.white);
                name.enableAutoSizing = true;
                name.fontSizeMin = 10f;
                name.fontSizeMax = 20f;
                icons[i] = icon;
                backgrounds[i] = slot;
                labels[i] = name;
            }

            BagUI bag = bg.gameObject.AddComponent<BagUI>();
            SerializedWire.Set(bag, "gameManager", gameManager);
            SerializedWire.Set(bag, "content", content);
            SerializedWire.SetArray(bag, "slotIcons", icons);
            SerializedWire.SetArray(bag, "slotLabels", labels);
            SerializedWire.SetArray(bag, "slotBackgrounds", backgrounds);
            SerializedWire.SetArray(bag, "slotButtons", buttons);
            SerializedWire.Set(bag, "survival", survival);
            SerializedWire.Set(bag, "recipes", recipes);
        }

        private static void BuildToast(Transform root)
        {
            Image bg = UiFactory.MakeImage("Toast", root, new Vector2(0.25f, 0.44f), new Vector2(0.75f, 0.54f), new Color(0f, 0f, 0f, 0.75f));
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", bg.transform, new Vector2(0.03f, 0f), new Vector2(0.97f, 1f), 30f, TextAlignmentOptions.Center, Color.white, bold: true);
            MessageToast toast = bg.gameObject.AddComponent<MessageToast>();
            SerializedWire.Set(toast, "group", group);
            SerializedWire.Set(toast, "label", label);
        }

        private static void BuildAfadPopup(Transform root)
        {
            // Bottom strip (where choice buttons go; placement nodes have none) so it never covers the room or its anchors.
            Image bg = UiFactory.MakeImage("AfadAlert", root, new Vector2(0.14f, 0.03f), new Vector2(0.74f, 0.23f), new Color(0.85f, 0.05f, 0.05f, 0.95f), raycast: true);
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
            Button open = UiFactory.MakeButton("OpenButton", journalRoot, new Vector2(0.79f, 0.92f), new Vector2(0.995f, 0.99f), new Color(0.15f, 0.15f, 0.22f, 0.92f), 24f, out TextMeshProUGUI openLabel);

            Image panelBg = UiFactory.MakeImage("Panel", journalRoot, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.92f), new Color(0.05f, 0.05f, 0.08f, 0.97f), raycast: true);
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

            Image tipRoot = UiFactory.MakeImage("Tip", bg.transform, new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.52f), new Color(1f, 1f, 1f, 0.07f));
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
