using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Main menu: Başla (loads the game scene), Ayarlar (shared SettingsPanel) and Çıkış. Plays the calm prep music
    /// and a click cue on every button. Applies the saved settings on start.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button start;
        [SerializeField] private Button settings;
        [SerializeField] private Button quit;

        [Tooltip("Buttons hidden while the settings panel is open.")]
        [SerializeField] private CanvasGroup buttons;

        [SerializeField] private SettingsPanel settingsPanel;

        [Tooltip("Menu music (the calm prep track).")]
        [SerializeField] private AudioCue music;

        [Tooltip("Button click.")]
        [SerializeField] private AudioCue clickCue;

        private void Awake()
        {
            UserSettings.Apply();
            if (start != null) start.onClick.AddListener(() => { Click(); SceneFlow.StartGame(); });
            if (settings != null) settings.onClick.AddListener(OpenSettings);
            if (quit != null) quit.onClick.AddListener(() => { Click(); SceneFlow.Quit(); });
            if (settingsPanel != null) settingsPanel.Closed += () => SetButtons(true);
        }

        private void Start()
        {
            if (AudioService.Instance != null && music != null) AudioService.Instance.PlayMusic(music);
            UserSettings.Changed += OnSettingsChanged;
        }

        private void OnDestroy()
        {
            UserSettings.Changed -= OnSettingsChanged;
        }

        private void OnSettingsChanged()
        {
            // The music layer is not a MusicService here: rescale it live.
            if (AudioService.Instance != null && music != null) AudioService.Instance.SetMusicVolume(music.Volume * UserSettings.Music);
        }

        private void OpenSettings()
        {
            Click();
            if (settingsPanel == null) return;
            SetButtons(false);
            settingsPanel.Open();
        }

        private void SetButtons(bool visible)
        {
            if (buttons == null) return;
            buttons.alpha = visible ? 1f : 0f;
            buttons.interactable = visible;
            buttons.blocksRaycasts = visible;
        }

        private void Click() => AudioService.PlayUiCue(clickCue);
    }
}
