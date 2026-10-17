using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// ESC pause (Input System): a dim full-screen overlay with Devam Et / Ayarlar / Ana Menü / Oyundan Çık.
    /// Ana Menü and Oyundan Çık ask "Emin misin?" first. Pausing goes through PauseState (timeScale 0, audio paused,
    /// world input blocked). Esc steps back out of the confirmation / settings before it resumes.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [Tooltip("Dim overlay + panel root (blocks clicks while shown).")]
        [SerializeField] private CanvasGroup overlay;

        [Tooltip("The four main buttons.")]
        [SerializeField] private CanvasGroup mainButtons;

        [SerializeField] private Button resume;
        [SerializeField] private Button settings;
        [SerializeField] private Button mainMenu;
        [SerializeField] private Button quit;

        [Tooltip("Shared settings panel.")]
        [SerializeField] private SettingsPanel settingsPanel;

        [Tooltip("'Emin misin? İlerleme kaybolacak.' with Evet / Hayır.")]
        [SerializeField] private CanvasGroup confirm;

        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;

        [Tooltip("UI click sound (plays while paused).")]
        [SerializeField] private AudioCue clickCue;

        private Action pendingConfirm;

        private void Awake()
        {
            if (resume != null) resume.onClick.AddListener(() => { Click(); SetPaused(false); });
            if (settings != null) settings.onClick.AddListener(() => { Click(); OpenSettings(); });
            if (mainMenu != null) mainMenu.onClick.AddListener(() => { Click(); AskConfirm(SceneFlow.LoadMainMenu); });
            if (quit != null) quit.onClick.AddListener(() => { Click(); AskConfirm(SceneFlow.Quit); });
            if (confirmYes != null) confirmYes.onClick.AddListener(() => { Click(); Action a = pendingConfirm; pendingConfirm = null; a?.Invoke(); });
            if (confirmNo != null) confirmNo.onClick.AddListener(() => { Click(); CloseConfirm(); });
            if (settingsPanel != null) settingsPanel.Closed += () => Show(mainButtons, true);
            Show(overlay, false);
            Show(confirm, false);
        }

        private void OnDestroy()
        {
            // Leaving the scene while paused must never leave the game frozen.
            if (PauseState.IsPaused) PauseState.ForceClear();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;
            if (!PauseState.IsPaused)
            {
                SetPaused(true);
                return;
            }

            if (confirm != null && confirm.alpha > 0.5f) CloseConfirm();
            else if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
            else SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            PauseState.Set(paused);
            Show(overlay, paused);
            Show(mainButtons, true);
            Show(confirm, false);
            pendingConfirm = null;
            if (!paused && settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
        }

        private void OpenSettings()
        {
            if (settingsPanel == null) return;
            Show(mainButtons, false);
            settingsPanel.Open();
        }

        private void AskConfirm(Action onYes)
        {
            pendingConfirm = onYes;
            Show(mainButtons, false);
            Show(confirm, true);
        }

        private void CloseConfirm()
        {
            pendingConfirm = null;
            Show(confirm, false);
            Show(mainButtons, true);
        }

        private void Click() => AudioService.PlayUiCue(clickCue);

        private static void Show(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
