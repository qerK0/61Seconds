using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// "BİLGİLENDİRME": the full-screen briefing at the start of every new run (scene load: the main menu start or
    /// Yeniden Dene; rewinds never reload the scene, so they never show it). Holds the first node (RunStartGate) and
    /// every timer (ScreenHold) until ANLADIM: a click, Enter or Space, accepted only after the guard so a stray click
    /// from the menu cannot skip it. Unscaled time.
    /// </summary>
    public sealed class IntroBriefing : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button start;

        [Tooltip("Seconds before any input is accepted.")]
        [SerializeField] private float guardSeconds = 1f;

        [Tooltip("Fade in / out seconds.")]
        [SerializeField] private float fadeSeconds = 0.3f;

        [Tooltip("Played when the briefing is dismissed.")]
        [SerializeField] private AudioCue pressCue;

        private float shownAt;
        private bool closing;
        private bool holding;

        private void Awake()
        {
            RunStartGate.Hold();
            ScreenHold.Begin();
            holding = true;
            shownAt = Time.unscaledTime;
            if (start != null) start.onClick.AddListener(Continue);
            if (root != null)
            {
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }
        }

        private void Start()
        {
            if (start != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(start.gameObject);
        }

        private void OnDestroy()
        {
            if (holding) ScreenHold.End();
            holding = false;
        }

        private void Update()
        {
            if (closing || !Ready) return;
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            // The button handles its own click; Enter / Space / a click anywhere also continue.
            if ((k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame)) Continue();
        }

        private bool Ready => Time.unscaledTime - shownAt >= guardSeconds;

        private void Continue()
        {
            if (closing || !Ready) return;
            closing = true;
            AudioService.PlayUiCue(pressCue);
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            if (root != null)
            {
                root.interactable = false;
                for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
                {
                    root.alpha = 1f - t / fadeSeconds;
                    yield return null;
                }

                root.alpha = 0f;
                root.blocksRaycasts = false;
            }

            if (holding) ScreenHold.End();
            holding = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            RunStartGate.Open();
            gameObject.SetActive(false);
        }
    }
}
