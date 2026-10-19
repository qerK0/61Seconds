using System;
using System.Collections;
using SixtyOne.QTE;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// WASD QTE panel: big current key, one pip per key (wire stages for the breaker), a per-key countdown bar,
    /// on-screen W/A/S/D buttons for touch, green flash on a hit, red flash + panel shake on a miss.
    /// </summary>
    public sealed class QTEView : MonoBehaviour, IQTEView
    {
        [Tooltip("Whole panel (hidden when no QTE runs).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Panel shaken on a miss.")]
        [SerializeField] private RectTransform panel;

        [Tooltip("Title (breaker / dog).")]
        [SerializeField] private TMP_Text title;

        [Tooltip("Node prompt.")]
        [SerializeField] private TMP_Text prompt;

        [Tooltip("The key to press now.")]
        [SerializeField] private TMP_Text targetKey;

        [Tooltip("Hint under the key ('W A S D ile bas').")]
        [SerializeField] private TMP_Text hint;

        [Tooltip("Progress pips (enough for the longest sequence).")]
        [SerializeField] private Image[] pips = new Image[0];

        [Tooltip("Per-key countdown (Filled image).")]
        [SerializeField] private Image keyCountdown;

        [Tooltip("Whole-sequence countdown (Filled image). Optional.")]
        [SerializeField] private Image totalCountdown;

        [Tooltip("On-screen W, A, S, D buttons (in that order).")]
        [SerializeField] private Button[] keyButtons = new Button[4];

        [Tooltip("Full-panel flash image (hit/miss).")]
        [SerializeField] private Image flash;

        [SerializeField] private Color pipPending = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color pipDone = new Color(0.35f, 0.9f, 0.45f, 1f);
        [SerializeField] private Color pipCurrent = new Color(1f, 0.82f, 0.2f, 1f);
        [SerializeField] private Color hitColor = new Color(0.35f, 0.9f, 0.45f, 0.35f);
        [SerializeField] private Color missColor = new Color(1f, 0.2f, 0.2f, 0.5f);

        private Action<QTEKey> onPress;
        private Vector2 panelOrigin;
        private Coroutine flashing;

        private void Awake()
        {
            if (panel != null) panelOrigin = panel.anchoredPosition;
            for (int i = 0; i < keyButtons.Length && i < QTESequence.AllKeys.Length; i++)
            {
                QTEKey key = QTESequence.AllKeys[i];
                if (keyButtons[i] != null) keyButtons[i].onClick.AddListener(() => onPress?.Invoke(key));
            }

            Hide();
        }

        public void Show(QTEContext context, string promptText, Action<QTEKey> press)
        {
            onPress = press;
            if (title != null) title.text = TextDB.GetDisplay(context == QTEContext.DogEvasion ? UiKeys.QteDogTitle : UiKeys.QteBreakerTitle);
            if (prompt != null) prompt.text = promptText;
            if (hint != null) hint.text = TextDB.GetDisplay(UiKeys.QteHint);
            if (flash != null) flash.color = Color.clear;
            UIVisibility.Set(group, true);
        }

        public void Refresh(QTERun run)
        {
            if (run == null) return;
            if (targetKey != null) targetKey.text = run.Target.ToString();
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] == null) continue;
                bool used = i < run.Keys.Count;
                pips[i].gameObject.SetActive(used);
                if (used) pips[i].color = i < run.Index ? pipDone : i == run.Index ? pipCurrent : pipPending;
            }

            if (keyCountdown != null) keyCountdown.fillAmount = Mathf.Clamp01(run.KeyTimeFraction);
            if (totalCountdown != null) totalCountdown.fillAmount = run.TotalTimeout > 0f ? Mathf.Clamp01(run.TotalTimeLeft / run.TotalTimeout) : 1f;
        }

        public void FlashHit(int keysDone) => Flash(hitColor, 0f);

        public void FlashMiss(QTEFailReason reason) => Flash(missColor, 14f);

        public void Hide()
        {
            onPress = null;
            if (flashing != null) StopCoroutine(flashing);
            flashing = null;
            if (panel != null) panel.anchoredPosition = panelOrigin;
            UIVisibility.Set(group, false);
        }

        private void Flash(Color color, float shakePixels)
        {
            if (!isActiveAndEnabled) return;
            if (flashing != null) StopCoroutine(flashing);
            flashing = StartCoroutine(FlashRoutine(color, shakePixels));
        }

        private IEnumerator FlashRoutine(Color color, float shakePixels)
        {
            const float duration = 0.25f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                if (flash != null) flash.color = new Color(color.r, color.g, color.b, color.a * k);
                if (panel != null && shakePixels > 0f)
                {
                    panel.anchoredPosition = panelOrigin + new Vector2(Mathf.Sin(t * 90f) * shakePixels * k, 0f);
                }

                yield return null;
            }

            if (flash != null) flash.color = Color.clear;
            if (panel != null) panel.anchoredPosition = panelOrigin;
            flashing = null;
        }
    }
}
