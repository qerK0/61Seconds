using System;
using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Tutorial
{
    /// <summary>
    /// The bottom-centre tutorial card (speech-bubble paper look): an "ÖĞRETİCİ" tab, "Adım n/6", the hint, an
    /// optional "devam" line for click-through hints and the Geç (skip step) button. Pops in on every new hint.
    /// </summary>
    public sealed class TutorialHintPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text step;

        [Tooltip("'Sol tık / Space: devam' line, shown only while a hint waits for a click.")]
        [SerializeField] private TMP_Text continueLine;

        [Tooltip("Geç: skips the current step.")]
        [SerializeField] private Button skip;

        [Tooltip("Click sound of the Geç button.")]
        [SerializeField] private AudioCue clickCue;

        private Coroutine pop;

        /// <summary>Raised when Geç is pressed.</summary>
        public event Action SkipPressed;

        /// <summary>The Geç button (continue clicks on it must not also count as "continue").</summary>
        public RectTransform SkipRect => skip != null ? (RectTransform)skip.transform : null;

        private void Awake()
        {
            if (skip != null)
            {
                skip.onClick.AddListener(() =>
                {
                    AudioService.PlayUiCue(clickCue);
                    SkipPressed?.Invoke();
                });
            }

            UIVisibility.Set(group, false);
        }

        /// <summary>Shows a hint for step n of total; waitsForContinue adds the "devam" line.</summary>
        public void Show(string text, int stepNumber, int totalSteps, bool waitsForContinue)
        {
            if (body != null) body.text = text;
            if (step != null) step.text = TextDB.Format(UiKeys.TutorialStep, stepNumber, totalSteps);
            if (continueLine != null)
            {
                continueLine.gameObject.SetActive(waitsForContinue);
                if (waitsForContinue) continueLine.text = TextDB.Format(UiKeys.TutorialContinue, TutorialBindings.ContinueKeys());
            }

            bool wasHidden = group != null && group.alpha < 0.5f;
            UIVisibility.Set(group, true);
            if (card == null) return;
            if (pop != null) StopCoroutine(pop);
            pop = StartCoroutine(Pop(wasHidden ? 0.85f : 0.96f));
        }

        public void Hide()
        {
            UIVisibility.Set(group, false);
        }

        private IEnumerator Pop(float from)
        {
            const float seconds = 0.18f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                card.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, Easing.Evaluate(Ease.OutBack, t / seconds));
                yield return null;
            }

            card.localScale = Vector3.one;
            pop = null;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(CanvasGroup root, RectTransform cardRect, TMP_Text text, TMP_Text stepLabel, TMP_Text continueLabel, Button skipButton, AudioCue click)
        {
            group = root;
            card = cardRect;
            body = text;
            step = stepLabel;
            continueLine = continueLabel;
            skip = skipButton;
            clickCue = click;
        }
    }
}
