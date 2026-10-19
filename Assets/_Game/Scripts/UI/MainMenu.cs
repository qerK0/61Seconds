using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// "İSTANBUL AFET BÖLGESİ" main menu: Oyunu Başlat (0.5 s fade to black with a rumble, then the game; the first
    /// time it asks "Önce öğreticiyi oynamak ister misin?"), Öğretici (the tutorial scene), Ayarlar
    /// (shared SettingsPanel), Galeri (collected Deprem Notları) and Çıkış (confirm, then quit). Plays the calm prep
    /// music ducked over a low rumble bed. Keyboard / gamepad: the first button is selected after the intro and
    /// whenever a panel closes; Esc closes the open panel. Button sounds and hover looks live on MenuButtonFx.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private Button start;

        [Tooltip("Öğretici: opens the tutorial scene.")]
        [SerializeField] private Button tutorial;

        [SerializeField] private Button settings;
        [SerializeField] private Button gallery;
        [SerializeField] private Button quit;

        [Tooltip("Buttons hidden while a panel is open.")]
        [SerializeField] private CanvasGroup buttons;

        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private TipGalleryPanel galleryPanel;

        [Header("Quit confirmation")]
        [SerializeField] private CanvasGroup confirm;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;

        [Header("First Oyunu Başlat: 'Önce öğreticiyi oynamak ister misin?'")]
        [SerializeField] private CanvasGroup tutorialAsk;
        [SerializeField] private Button askYes;
        [SerializeField] private Button askNo;

        [SerializeField] private ScreenFader fader;
        [SerializeField] private MenuIntro intro;

        [Header("Audio")]
        [Tooltip("Menu music (the calm prep track).")]
        [SerializeField] private AudioCue music;

        [Tooltip("Music volume share under the rumble (ducked).")]
        [Range(0f, 1f)] [SerializeField] private float musicDuck = 0.6f;

        [Tooltip("Low ambient rumble loop under the music.")]
        [SerializeField] private AudioCue rumbleLoop;

        [Tooltip("The low rumble while fading into the game.")]
        [SerializeField] private AudioCue startRumble;

        [SerializeField] private AudioCue clickCue;

        [Tooltip("Seconds of the fade to black before the game loads.")]
        [SerializeField] private float startFadeSeconds = 0.5f;

        private bool leaving;

        private void Awake()
        {
            UserSettings.Apply();
            if (start != null) start.onClick.AddListener(StartGame);
            if (tutorial != null) tutorial.onClick.AddListener(StartTutorial);
            if (askYes != null) askYes.onClick.AddListener(() => Leave(SceneFlow.StartTutorial));
            if (askNo != null) askNo.onClick.AddListener(() => Leave(SceneFlow.StartGame));
            if (settings != null) settings.onClick.AddListener(OpenSettings);
            if (gallery != null) gallery.onClick.AddListener(OpenGallery);
            if (quit != null) quit.onClick.AddListener(OpenConfirm);
            if (confirmYes != null) confirmYes.onClick.AddListener(() => { AudioService.PlayUiCue(clickCue); SceneFlow.Quit(); });
            if (confirmNo != null) confirmNo.onClick.AddListener(CloseConfirm);
            if (settingsPanel != null) settingsPanel.Closed += PanelClosed;
            if (galleryPanel != null) galleryPanel.Closed += PanelClosed;
            if (intro != null) intro.Finished += SelectFirst;
            SetGroup(confirm, false);
            SetGroup(tutorialAsk, false);
        }

        private void Start()
        {
            if (AudioService.Instance != null)
            {
                if (music != null) AudioService.Instance.PlayMusic(music);
                OnSettingsChanged();
                if (rumbleLoop != null) AudioService.Instance.Play(rumbleLoop);
            }

            UserSettings.Changed += OnSettingsChanged;
            if (intro == null) SelectFirst();
        }

        private void OnDestroy()
        {
            UserSettings.Changed -= OnSettingsChanged;
        }

        private void Update()
        {
            Keyboard k = Keyboard.current;
            Gamepad g = Gamepad.current;
            bool back = (k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
            if (back)
            {
                if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
                else if (galleryPanel != null && galleryPanel.IsOpen) galleryPanel.Close();
                else if (confirm != null && confirm.alpha > 0.5f) CloseConfirm();
                else if (tutorialAsk != null && tutorialAsk.alpha > 0.5f) CloseAsk();
            }

            // Keyboard / gamepad: arrows with nothing selected (after a mouse click on empty space) pick a button again.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && NavigationPressed(k, g)) SelectFirst();
        }

        private static bool NavigationPressed(Keyboard k, Gamepad g)
        {
            return (k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame))
                || (g != null && (g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame));
        }

        private void OnSettingsChanged()
        {
            // The music layer is not a MusicService here: rescale it live (ducked under the rumble bed).
            if (AudioService.Instance != null && music != null) AudioService.Instance.SetMusicVolume(music.Volume * UserSettings.Music * musicDuck);
        }

        /// <summary>Oyunu Başlat: the very first time asks whether to play the tutorial first (once, ever).</summary>
        private void StartGame()
        {
            if (leaving) return;
            if (!TutorialProgress.Asked && tutorialAsk != null)
            {
                TutorialProgress.Asked = true;
                AudioService.PlayUiCue(clickCue);
                SetButtons(false);
                SetGroup(tutorialAsk, true);
                if (askYes != null) Select(askYes.gameObject);
                return;
            }

            Leave(SceneFlow.StartGame);
        }

        private void StartTutorial()
        {
            TutorialProgress.Asked = true;
            Leave(SceneFlow.StartTutorial);
        }

        /// <summary>0.5 s fade to black with the low rumble, then the scene.</summary>
        private void Leave(Action load)
        {
            if (leaving) return;
            leaving = true;
            AudioService.PlayUiCue(startRumble);
            SetButtons(false);
            SetGroup(tutorialAsk, false);
            if (fader != null) fader.FadeOut(startFadeSeconds, load);
            else load();
        }

        private void CloseAsk()
        {
            AudioService.PlayUiCue(clickCue);
            SetGroup(tutorialAsk, false);
            PanelClosed();
        }

        private void OpenSettings()
        {
            if (settingsPanel == null) return;
            SetButtons(false);
            settingsPanel.Open();
            Select(FirstSelectable(settingsPanel.gameObject));
        }

        private void OpenGallery()
        {
            if (galleryPanel == null) return;
            SetButtons(false);
            galleryPanel.Open();
            if (galleryPanel.BackButton != null) Select(galleryPanel.BackButton.gameObject);
        }

        private void OpenConfirm()
        {
            SetButtons(false);
            SetGroup(confirm, true);
            if (confirmNo != null) Select(confirmNo.gameObject);
        }

        private void CloseConfirm()
        {
            AudioService.PlayUiCue(clickCue);
            SetGroup(confirm, false);
            PanelClosed();
        }

        private void PanelClosed()
        {
            SetButtons(true);
            SelectFirst();
        }

        private void SelectFirst()
        {
            if (start != null) Select(start.gameObject);
        }

        private static void Select(GameObject go)
        {
            if (go != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(go);
        }

        private static GameObject FirstSelectable(GameObject root)
        {
            Selectable s = root.GetComponentInChildren<Selectable>();
            return s != null ? s.gameObject : null;
        }

        private void SetButtons(bool visible) => SetGroup(buttons, visible);

        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
