using System.Collections;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Flashing AFAD early-warning popup: hides after ~4s, on the first click or tap anywhere, and on any rewind or death. While hidden it blocks no clicks.</summary>
    public sealed class AfadAlertPopup : MonoBehaviour
    {
        [Tooltip("Popup group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Flashing background.")]
        [SerializeField] private Image background;

        [Tooltip("Title label.")]
        [SerializeField] private TMP_Text title;

        [Tooltip("Body label ({0} = magnitude).")]
        [SerializeField] private TMP_Text body;

        [Tooltip("Tap anywhere on the popup to dismiss.")]
        [SerializeField] private Button dismissButton;

        [Tooltip("Seconds before it hides itself.")]
        [SerializeField] private float showSeconds = 4f;

        [Tooltip("A click anywhere after this many seconds hides it (the click that caused it does not).")]
        [SerializeField] private float clickGraceSeconds = 0.25f;

        private bool visible;
        private float shownAt;

        [Tooltip("Flash colors.")]
        [SerializeField] private Color flashA = new Color(0.85f, 0.05f, 0.05f, 0.95f);

        [Tooltip("Flash colors.")]
        [SerializeField] private Color flashB = new Color(0.25f, 0f, 0f, 0.95f);

        private void Awake()
        {
            UIVisibility.Set(group, false);
            if (dismissButton != null) dismissButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            GameEvents.AfadAlert += OnAlert;
            GameEvents.Alert += OnKeyedAlert;
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.AfadAlert -= OnAlert;
            GameEvents.Alert -= OnKeyedAlert;
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
        }

        private void OnAlert(float magnitude)
        {
            OnKeyedAlert(UiKeys.AfadTitle, UiKeys.AfadBody, magnitude.ToString("0.0"));
        }

        /// <summary>Shows any alert by text keys; arg fills {0} in the body.</summary>
        private void OnKeyedAlert(string titleKey, string bodyKey, string arg)
        {
            StopAllCoroutines();
            StartCoroutine(Show(titleKey, bodyKey, arg));
        }

        private IEnumerator Show(string titleKey, string bodyKey, string arg)
        {
            if (title != null) title.text = string.IsNullOrEmpty(titleKey) ? string.Empty : TextDB.GetDisplay(titleKey);
            if (body != null)
            {
                body.text = string.IsNullOrEmpty(bodyKey) ? string.Empty
                    : arg != null ? TextDB.Format(bodyKey, arg) : TextDB.GetDisplay(bodyKey);
            }

            UIVisibility.Set(group, true);
            visible = true;
            shownAt = Time.unscaledTime;

            float time = 0f;
            while (time < showSeconds)
            {
                time += Time.unscaledDeltaTime;
                if (background != null) background.color = Mathf.Repeat(time, 0.5f) < 0.25f ? flashA : flashB;
                yield return null;
            }

            yield return Tween.Fade(group, 0f, 0.3f);
            Hide();
        }

        public void Hide()
        {
            StopAllCoroutines();
            visible = false;
            UIVisibility.Set(group, false);
        }

        private void OnDied(DeathInfo info) => Hide();

        private void OnRewound(string nodeId) => Hide();

        private void Update()
        {
            // The first click or tap anywhere (world or UI) dismisses it.
            if (!visible || Time.unscaledTime - shownAt < clickGraceSeconds) return;
            UnityEngine.InputSystem.Pointer pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame) Hide();
        }
    }
}
