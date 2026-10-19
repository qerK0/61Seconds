using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Volume sliders (Ana Ses / Müzik / Efekt), the Parlaklık slider and the Efektleri Azalt toggle, shared by the
    /// main menu and the pause menu. Reads and writes UserSettings (PlayerPrefs), which applies them at once. A panel
    /// built before Parlaklık existed gets the row at runtime (a copy of the Efekt row), so old scenes need no rebuild.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        /// <summary>Row bottoms (panel-relative y) of Ana Ses, Müzik, Efekt and Parlaklık.</summary>
        public static readonly float[] RowY = { 0.70f, 0.58f, 0.46f, 0.34f };

        /// <summary>Bottom of the Efektleri Azalt toggle row.</summary>
        public const float ToggleY = 0.21f;

        [Tooltip("Panel root shown / hidden by Open / Close.")]
        [SerializeField] private CanvasGroup group;

        [SerializeField] private Slider master;
        [SerializeField] private Slider music;
        [SerializeField] private Slider sfx;

        [Tooltip("Parlaklık (0..1, 0.5 = default). Empty = copied from the Efekt row at runtime.")]
        [SerializeField] private Slider brightness;

        [SerializeField] private Toggle reduceEffects;

        [Tooltip("Closes the panel.")]
        [SerializeField] private Button back;

        [Tooltip("UI click sound (plays while paused).")]
        [SerializeField] private AudioCue clickCue;

        private bool syncing;

        /// <summary>Raised when the panel closes (the owner shows its own buttons again).</summary>
        public event Action Closed;

        public bool IsOpen => group != null && group.alpha > 0.5f;

        private void Awake()
        {
            if (brightness == null) brightness = AddBrightnessRow();
            if (master != null) master.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Master = v; });
            if (music != null) music.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Music = v; });
            if (sfx != null) sfx.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Sfx = v; });
            if (brightness != null) brightness.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Brightness = v; });
            if (reduceEffects != null)
            {
                reduceEffects.onValueChanged.AddListener(v =>
                {
                    if (syncing) return;
                    UserSettings.ReduceEffects = v;
                    AudioService.PlayUiCue(clickCue);
                });
            }

            if (back != null) back.onClick.AddListener(Close);
            SetVisible(false);
        }

        public void Open()
        {
            syncing = true;
            if (master != null) master.value = UserSettings.Master;
            if (music != null) music.value = UserSettings.Music;
            if (sfx != null) sfx.value = UserSettings.Sfx;
            if (brightness != null) brightness.value = UserSettings.Brightness;
            if (reduceEffects != null) reduceEffects.isOn = UserSettings.ReduceEffects;
            syncing = false;
            SetVisible(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            AudioService.PlayUiCue(clickCue);
            SetVisible(false);
            Closed?.Invoke();
        }

        /// <summary>Places a label + slider row with its bottom at y (panel-relative), the MenuUi slider-row layout.</summary>
        public static void PlaceRow(RectTransform label, RectTransform slider, float y)
        {
            if (label != null) SetAnchors(label, new Vector2(0.08f, y), new Vector2(0.38f, y + 0.11f));
            if (slider != null) SetAnchors(slider, new Vector2(0.4f, y + 0.035f), new Vector2(0.92f, y + 0.075f));
        }

        /// <summary>Places the toggle row (box + label) with its bottom at y.</summary>
        public static void PlaceToggle(RectTransform toggle, float y)
        {
            if (toggle != null) SetAnchors(toggle, new Vector2(0.08f, y), new Vector2(0.92f, y + 0.11f));
        }

        /// <summary>
        /// Copies the Efekt row (label + slider) as the Parlaklık row and moves every row to the four-slider layout.
        /// Null when the panel has no Efekt row to copy.
        /// </summary>
        private Slider AddBrightnessRow()
        {
            if (sfx == null) return null;
            Transform parent = sfx.transform.parent;
            Transform sfxLabel = parent.Find("SfxLabel");
            Slider copy = Instantiate(sfx, parent);
            copy.name = "BrightnessSlider";
            copy.onValueChanged.RemoveAllListeners();
            RectTransform label = null;
            if (sfxLabel != null)
            {
                label = (RectTransform)Instantiate(sfxLabel.gameObject, parent).transform;
                label.name = "BrightnessLabel";
                LocalizedText text = label.GetComponent<LocalizedText>();
                if (text != null) text.SetKey(UiKeys.SettingsBrightness);
            }

            PlaceRow(parent.Find("MasterLabel") as RectTransform, master != null ? (RectTransform)master.transform : null, RowY[0]);
            PlaceRow(parent.Find("MusicLabel") as RectTransform, music != null ? (RectTransform)music.transform : null, RowY[1]);
            PlaceRow(sfxLabel as RectTransform, (RectTransform)sfx.transform, RowY[2]);
            PlaceRow(label, (RectTransform)copy.transform, RowY[3]);
            if (reduceEffects != null) PlaceToggle((RectTransform)reduceEffects.transform, ToggleY);
            return copy;
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
