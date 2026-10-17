using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Volume sliders (Ana Ses / Müzik / Efekt) and the Efektleri Azalt toggle, shared by the main menu and the
    /// pause menu. Reads and writes UserSettings (PlayerPrefs), which applies them at once.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [Tooltip("Panel root shown / hidden by Open / Close.")]
        [SerializeField] private CanvasGroup group;

        [SerializeField] private Slider master;
        [SerializeField] private Slider music;
        [SerializeField] private Slider sfx;
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
            if (master != null) master.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Master = v; });
            if (music != null) music.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Music = v; });
            if (sfx != null) sfx.onValueChanged.AddListener(v => { if (!syncing) UserSettings.Sfx = v; });
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

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
