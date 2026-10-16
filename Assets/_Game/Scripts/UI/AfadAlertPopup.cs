using System.Collections;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Flashing AFAD early-warning popup. Tap to dismiss; hides itself after a few seconds.</summary>
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
        [SerializeField] private float showSeconds = 3.5f;

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
        }

        private void OnDisable()
        {
            GameEvents.AfadAlert -= OnAlert;
            GameEvents.Alert -= OnKeyedAlert;
            GameEvents.Died -= OnDied;
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
            UIVisibility.Set(group, false);
        }

        private void OnDied(DeathInfo info) => Hide();
    }
}
