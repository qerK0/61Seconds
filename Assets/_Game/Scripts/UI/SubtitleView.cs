using System;
using SixtyOne.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SixtyOne.UI
{
    /// <summary>
    /// Cinematic subtitles: a full-width bottom band (transparent → 85% black gradient) with an optional speaker
    /// name over a typewritten line (EB Garamond). Dialogue lines show a bobbing ▼ after the last word once complete;
    /// a click on the band, Space or Enter first completes the line, then advances. Narration (no speaker, no
    /// advance) fades out after a reading delay. Visual only: DialogueController pays the line costs. Fades in/out
    /// over 0.2s and hides during room transitions. Reduce Effects removes the bob.
    /// </summary>
    public sealed class SubtitleView : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Band root (gradient image + text).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Speaker name (empty for narration).")]
        [SerializeField] private TMP_Text speaker;

        [Tooltip("The line text.")]
        [SerializeField] private TMP_Text line;

        [Tooltip("Typewriter on the line (voice blips listen to it).")]
        [SerializeField] private TypewriterText typewriter;

        [Tooltip("The ▼ continue mark, placed after the last character.")]
        [SerializeField] private RectTransform continueMark;

        [Tooltip("Fade in/out seconds (unscaled).")]
        [SerializeField] private float fadeSeconds = 0.2f;

        [Tooltip("Bob height (px) and frequency (Hz) of the ▼ mark.")]
        [SerializeField] private Vector2 bob = new Vector2(4f, 1.2f);

        [Tooltip("Narration stays this long (plus reading time) before fading out.")]
        [SerializeField] private float narrationSeconds = 3f;

        private Action onAdvance;
        private bool wanted;
        private bool inTransition;
        private float hideAt = -1f;
        private Vector2 markBase;
        private string shownText;
        private string shownSpeaker;

        /// <summary>Band height in canvas pixels (the ChoicePanel opens above it while visible).</summary>
        public float Height => ((RectTransform)transform).rect.height;

        /// <summary>True while the band is (being) shown.</summary>
        public bool IsShown => wanted && !inTransition;

        public TypewriterText Typewriter => typewriter;

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }

            if (continueMark != null) continueMark.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            NavigationEvents.OnTransitionStart += OnTransitionStart;
            NavigationEvents.OnTransitionComplete += OnTransitionComplete;
        }

        private void OnDisable()
        {
            NavigationEvents.OnTransitionStart -= OnTransitionStart;
            NavigationEvents.OnTransitionComplete -= OnTransitionComplete;
        }

        /// <summary>A dialogue line: the name (or none), the typewritten line, ▼ when complete; advance on click / Space / Enter.</summary>
        public void ShowLine(string speakerName, string text, Action advance)
        {
            onAdvance = advance;
            hideAt = -1f;
            // Choice refreshes re-present the same line: keep it (no re-typing).
            if (wanted && text == shownText && speakerName == shownSpeaker) return;
            Show(speakerName, text);
        }

        /// <summary>Narration / node body with no speaker: no ▼, fades out after a reading delay.</summary>
        public void ShowNarration(string text)
        {
            onAdvance = null;
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            if (wanted && text == shownText && shownSpeaker == null) return;
            Show(null, text);
            hideAt = Time.unscaledTime + narrationSeconds + text.Length / 18f;
        }

        public void Hide()
        {
            wanted = false;
            onAdvance = null;
            hideAt = -1f;
            shownText = null;
            shownSpeaker = null;
            if (typewriter != null) typewriter.Complete();
        }

        public void OnPointerClick(PointerEventData eventData) => Advance();

        private void Show(string speakerName, string text)
        {
            wanted = true;
            shownText = text;
            shownSpeaker = speakerName;
            if (speaker != null)
            {
                speaker.text = speakerName ?? string.Empty;
                speaker.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
            }

            if (typewriter != null) typewriter.Play(text, true);
            else if (line != null) line.text = text ?? string.Empty;
        }

        private void Advance()
        {
            if (!wanted || PauseState.IsPaused) return;
            if (typewriter != null && typewriter.IsRevealing)
            {
                typewriter.Complete();
                return;
            }

            if (onAdvance != null)
            {
                Action next = onAdvance;
                onAdvance = null;
                next();
            }
            else
            {
                Hide();
            }
        }

        private void Update()
        {
            if (wanted && !inTransition && !PauseState.IsPaused && !InputGate.IsBlocked)
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                {
                    Advance();
                }
            }

            if (hideAt > 0f && Time.unscaledTime >= hideAt && (typewriter == null || !typewriter.IsRevealing))
            {
                Hide();
            }

            if (group != null)
            {
                float target = wanted && !inTransition ? 1f : 0f;
                group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
                group.blocksRaycasts = target > 0f;
            }

            UpdateMark();
        }

        /// <summary>▼ right after the last visible character once the line is complete (dialogue only).</summary>
        private void UpdateMark()
        {
            if (continueMark == null) return;
            bool show = wanted && onAdvance != null && line != null && (typewriter == null || !typewriter.IsRevealing);
            if (continueMark.gameObject.activeSelf != show) continueMark.gameObject.SetActive(show);
            if (!show) return;

            TMP_TextInfo info = line.textInfo;
            int last = Mathf.Min(info.characterCount, line.maxVisibleCharacters) - 1;
            while (last >= 0 && !info.characterInfo[last].isVisible) last--;
            if (last >= 0)
            {
                TMP_CharacterInfo c = info.characterInfo[last];
                Vector3 local = new Vector3(c.topRight.x + 10f, (c.baseLine + c.ascender) * 0.5f, 0f);
                Vector3 world = line.rectTransform.TransformPoint(local);
                markBase = continueMark.parent.InverseTransformPoint(world);
            }

            float y = LedClock.ReduceEffects ? 0f : Mathf.Sin(Time.unscaledTime * bob.y * Mathf.PI * 2f) * bob.x;
            continueMark.localPosition = new Vector3(markBase.x, markBase.y + y, 0f);
        }

        private void OnTransitionStart(string from, string to) => inTransition = true;

        private void OnTransitionComplete(string room) => inTransition = false;
    }
}
