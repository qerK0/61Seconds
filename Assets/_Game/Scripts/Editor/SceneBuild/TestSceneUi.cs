using SixtyOne.Nodes;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Placement;
using SixtyOne.Prep;
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
            PrepController prep, RoomSwitcher rooms, string[] prepRooms, out LetterboxBars letterbox)
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
            BuildCountdown(root, prep, nodePlayer);
            BuildActionProgress(root, prep);
            BuildRoomTabs(root, prep, rooms, prepRooms);
            BuildLoopCounter(root, gameManager);
            BuildBag(root, gameManager, content, art);
            BuildToast(root);
            BuildAfadPopup(root);
            BuildJournal(root, gameManager, content);
            BuildDeathCard(root, gameManager, content, art);
            BuildRewindFx(root, camera);
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
            TextMeshProUGUI reason = UiFactory.MakeLabel("Reason", button.transform, new Vector2(0.68f, 0f), new Vector2(0.99f, 1f), 16f, TextAlignmentOptions.Right, new Color(1f, 0.7f, 0.4f, 1f));
            reason.enableAutoSizing = true;
            reason.fontSizeMin = 10f;
            reason.fontSizeMax = 16f;

            ChoiceButton choiceButton = button.gameObject.AddComponent<ChoiceButton>();
            SerializedWire.Set(choiceButton, "button", button);
            SerializedWire.Set(choiceButton, "label", label);
            SerializedWire.Set(choiceButton, "lockIcon", lockIcon.gameObject);
            SerializedWire.Set(choiceButton, "reasonText", reason);
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

        private static void BuildCountdown(Transform root, PrepController prep, NodePlayer nodePlayer)
        {
            RectTransform rect = UiFactory.MakeRect("Countdown", root, new Vector2(0.4f, 0.85f), new Vector2(0.6f, 0.95f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            TextMeshProUGUI label = UiFactory.MakeLabel("Number", rect, Vector2.zero, Vector2.one, 90f, TextAlignmentOptions.Center, Color.white, bold: true);

            CountdownDisplay display = rect.gameObject.AddComponent<CountdownDisplay>();
            SerializedWire.Set(display, "prep", prep);
            SerializedWire.Set(display, "nodePlayer", nodePlayer);
            SerializedWire.Set(display, "group", group);
            SerializedWire.Set(display, "label", label);
        }

        private static void BuildActionProgress(Transform root, PrepController prep)
        {
            Image bg = UiFactory.MakeImage("ActionProgress", root, new Vector2(0.33f, 0.42f), new Vector2(0.67f, 0.47f), new Color(0f, 0f, 0f, 0.7f));
            CanvasGroup group = UiFactory.MakeGroup(bg.gameObject);
            Image fill = UiFactory.MakeImage("Fill", bg.transform, Vector2.zero, Vector2.one, new Color(0.35f, 0.85f, 0.45f, 0.9f));
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            TextMeshProUGUI label = UiFactory.MakeLabel("Label", bg.transform, Vector2.zero, Vector2.one, 24f, TextAlignmentOptions.Center, Color.white, bold: true);

            ActionProgressBar bar = bg.gameObject.AddComponent<ActionProgressBar>();
            SerializedWire.Set(bar, "prep", prep);
            SerializedWire.Set(bar, "group", group);
            SerializedWire.Set(bar, "fill", fill.rectTransform);
            SerializedWire.Set(bar, "label", label);
        }

        private static void BuildRoomTabs(Transform root, PrepController prep, RoomSwitcher rooms, string[] roomIds)
        {
            RectTransform rect = UiFactory.MakeRect("RoomTabs", root, new Vector2(0.005f, 0.45f), new Vector2(0.13f, 0.78f));
            CanvasGroup group = UiFactory.MakeGroup(rect.gameObject);
            var buttons = new Object[roomIds.Length];
            float step = 1f / Mathf.Max(1, roomIds.Length);
            for (int i = 0; i < roomIds.Length; i++)
            {
                float top = 1f - i * step;
                Button button = UiFactory.MakeButton("Tab_" + roomIds[i], rect, new Vector2(0f, top - step + 0.03f), new Vector2(1f, top), ButtonColor, 24f, out TextMeshProUGUI label);
                LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
                SerializedWire.SetTextKey(localized, "key", "room." + roomIds[i]);
                buttons[i] = button;
            }

            RoomTabs tabs = rect.gameObject.AddComponent<RoomTabs>();
            SerializedWire.Set(tabs, "prep", prep);
            SerializedWire.Set(tabs, "group", group);
            SerializedWire.Set(tabs, "rooms", rooms);
            SerializedWire.SetStringArray(tabs, "roomIds", roomIds);
            SerializedWire.SetArray(tabs, "buttons", buttons);
        }

        private static void BuildLoopCounter(Transform root, GameManager gameManager)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel("LoopCounter", root, new Vector2(0.01f, 0.91f), new Vector2(0.26f, 0.99f), 44f, TextAlignmentOptions.Left, Accent, bold: true);
            LoopCounterUI counter = label.gameObject.AddComponent<LoopCounterUI>();
            SerializedWire.Set(counter, "gameManager", gameManager);
            SerializedWire.Set(counter, "label", label);
        }

        private static void BuildBag(Transform root, GameManager gameManager, GameContent content, PlaceholderSprites art)
        {
            Image bg = UiFactory.MakeImage("Bag", root, new Vector2(0.865f, 0.42f), new Vector2(0.995f, 0.90f), new Color(0f, 0f, 0f, 0.6f));
            TextMeshProUGUI title = UiFactory.MakeLabel("Title", bg.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 1f), 26f, TextAlignmentOptions.Center, Accent, bold: true);
            LocalizedText localized = title.gameObject.AddComponent<LocalizedText>();
            SerializedWire.SetTextKey(localized, "key", UiKeys.BagTitle);

            var icons = new Object[Inventory.DefaultCapacity];
            var labels = new Object[Inventory.DefaultCapacity];
            var backgrounds = new Object[Inventory.DefaultCapacity];
            for (int i = 0; i < Inventory.DefaultCapacity; i++)
            {
                float top = 0.86f - i * 0.215f;
                Image slot = UiFactory.MakeImage("Slot" + i, bg.transform, new Vector2(0.05f, top - 0.2f), new Vector2(0.95f, top), new Color(1f, 1f, 1f, 0.12f));
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
            Image bg = UiFactory.MakeImage("AfadAlert", root, new Vector2(0.2f, 0.55f), new Vector2(0.8f, 0.84f), new Color(0.85f, 0.05f, 0.05f, 0.95f), raycast: true);
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
