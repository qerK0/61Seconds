using SixtyOne.Audio;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Shared builders for the main menu and the ESC pause menu: serif menu buttons, the settings panel (volume
    /// sliders + Efektleri Azalt) and the "Emin misin?" confirmation. UiFactory.Theme / Rounded must be set.
    /// </summary>
    public static class MenuUi
    {
        public static readonly Color Ink = new Color(0.949f, 0.929f, 0.894f, 1f);
        public static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.06f, 0.94f);
        public static readonly Color ButtonColor = new Color(0.13f, 0.12f, 0.12f, 0.9f);

        /// <summary>A label bound to a text key (LocalizedText).</summary>
        public static TextMeshProUGUI KeyLabel(string name, Transform parent, Vector2 min, Vector2 max, string key, float size, TextAlignmentOptions align, bool serif, bool strong)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel(name, parent, min, max, size, align, Ink);
            UITheme theme = UiFactory.Theme;
            if (theme != null) label.font = serif ? (strong ? theme.SerifSemiBold : theme.SerifRegular) : (strong ? theme.SemiBold : theme.Regular);
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", key);
            return label;
        }

        /// <summary>A menu button with a serif, text-keyed label.</summary>
        public static Button Button(string name, Transform parent, Vector2 min, Vector2 max, string key, float size = 34f)
        {
            Button button = UiFactory.MakeButton(name, parent, min, max, ButtonColor, size, out TextMeshProUGUI label);
            if (UiFactory.Theme != null) label.font = UiFactory.Theme.SerifSemiBold;
            label.color = Ink;
            SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", key);
            return button;
        }

        /// <summary>Settings panel centred in the parent: title, three volume sliders, Parlaklık, the Reduce Effects toggle, Geri.</summary>
        public static SettingsPanel Settings(Transform parent, AudioCue click)
        {
            Image panel = UiFactory.MakePanel("SettingsPanel", parent, new Vector2(0.33f, 0.24f), new Vector2(0.67f, 0.76f), PanelColor, raycast: true);
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            Transform t = panel.transform;
            KeyLabel("Title", t, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.97f), UiKeys.SettingsTitle, 44f, TextAlignmentOptions.Center, true, true);

            Slider master = SliderRow(t, "Master", UiKeys.SettingsMaster, SettingsPanel.RowY[0]);
            Slider music = SliderRow(t, "Music", UiKeys.SettingsMusic, SettingsPanel.RowY[1]);
            Slider sfx = SliderRow(t, "Sfx", UiKeys.SettingsSfx, SettingsPanel.RowY[2]);
            Slider brightness = SliderRow(t, "Brightness", UiKeys.SettingsBrightness, SettingsPanel.RowY[3]);
            brightness.value = 0.5f;
            Toggle reduce = ToggleRow(t, UiKeys.SettingsReduce, SettingsPanel.ToggleY);
            Button back = Button("Back", t, new Vector2(0.32f, 0.04f), new Vector2(0.68f, 0.15f), UiKeys.SettingsBack, 30f);

            SettingsPanel settings = panel.gameObject.AddComponent<SettingsPanel>();
            SerializedWire.Set(settings, "group", group);
            SerializedWire.Set(settings, "master", master);
            SerializedWire.Set(settings, "music", music);
            SerializedWire.Set(settings, "sfx", sfx);
            SerializedWire.Set(settings, "brightness", brightness);
            SerializedWire.Set(settings, "reduceEffects", reduce);
            SerializedWire.Set(settings, "back", back);
            SerializedWire.Set(settings, "clickCue", click);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return settings;
        }

        /// <summary>"Emin misin? İlerleme kaybolacak." with Evet / Hayır, hidden by default.</summary>
        public static CanvasGroup Confirm(Transform parent, out Button yes, out Button no)
        {
            Image panel = UiFactory.MakePanel("Confirm", parent, new Vector2(0.34f, 0.38f), new Vector2(0.66f, 0.62f), PanelColor, raycast: true);
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            KeyLabel("Question", panel.transform, new Vector2(0.06f, 0.5f), new Vector2(0.94f, 0.92f), UiKeys.PauseConfirm, 32f, TextAlignmentOptions.Center, true, false);
            yes = Button("Yes", panel.transform, new Vector2(0.12f, 0.1f), new Vector2(0.46f, 0.38f), UiKeys.PauseYes, 30f);
            no = Button("No", panel.transform, new Vector2(0.54f, 0.1f), new Vector2(0.88f, 0.38f), UiKeys.PauseNo, 30f);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static Slider SliderRow(Transform parent, string name, string key, float y)
        {
            KeyLabel(name + "Label", parent, new Vector2(0.08f, y), new Vector2(0.38f, y + 0.11f), key, 28f, TextAlignmentOptions.MidlineLeft, true, false);
            RectTransform root = UiFactory.MakeRect(name + "Slider", parent, new Vector2(0.4f, y + 0.035f), new Vector2(0.92f, y + 0.075f));
            Image bg = UiFactory.MakeImage("Background", root, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.14f));
            UiFactory.MakeRound(bg);
            RectTransform fillArea = UiFactory.MakeRect("Fill Area", root, Vector2.zero, Vector2.one);
            Image fill = UiFactory.MakeImage("Fill", fillArea, Vector2.zero, Vector2.one, UiFactory.Theme != null ? UiFactory.Theme.Accent : Color.white);
            UiFactory.MakeRound(fill);
            RectTransform handleArea = UiFactory.MakeRect("Handle Slide Area", root, Vector2.zero, Vector2.one);
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            Image handle = UiFactory.MakeImage("Handle", handleArea, new Vector2(0f, -0.6f), new Vector2(0f, 1.6f), Ink, raycast: true);
            UiFactory.MakeRound(handle);
            handle.rectTransform.sizeDelta = new Vector2(24f, 0f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            bg.raycastTarget = true;
            return slider;
        }

        private static Toggle ToggleRow(Transform parent, string key, float y)
        {
            RectTransform root = UiFactory.MakeRect("ReduceEffects", parent, new Vector2(0.08f, y), new Vector2(0.92f, y + 0.11f));
            Image box = UiFactory.MakeImage("Box", root, new Vector2(0f, 0.2f), new Vector2(0f, 0.8f), new Color(1f, 1f, 1f, 0.14f), raycast: true);
            UiFactory.MakeRound(box);
            box.rectTransform.sizeDelta = new Vector2(38f, 0f);
            box.rectTransform.anchoredPosition = new Vector2(19f, 0f);
            Image check = UiFactory.MakeImage("Check", box.transform, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), UiFactory.Theme != null ? UiFactory.Theme.Accent : Color.white);
            UiFactory.MakeRound(check);
            TextMeshProUGUI label = KeyLabel("Label", root, new Vector2(0f, 0f), new Vector2(1f, 1f), key, 28f, TextAlignmentOptions.MidlineLeft, true, false);
            label.rectTransform.offsetMin = new Vector2(56f, 0f);

            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = check;
            toggle.isOn = false;
            return toggle;
        }
    }
}
