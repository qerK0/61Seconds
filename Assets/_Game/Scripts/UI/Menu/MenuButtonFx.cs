using SixtyOne.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Look and feel of a menu button. Mouse hover selects the button, so mouse and keyboard / gamepad share one
    /// "hot" state. Primary (Oyunu Başlat, Menüye Dön): the glow pulses slowly, hot = a stronger glow, a diagonal
    /// shine sweep and 1.03 scale; a press = 0.97, a micro-shake, a click and a low rumble. Secondary rows: hot = a
    /// left accent bar slides in, the text brightens and a › appears. Text buttons (Yeniden Dene): the underline grows.
    /// Unscaled time.
    /// </summary>
    public sealed class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public enum Style { Primary, Secondary, Text }

        [SerializeField] private Style style;

        [Tooltip("Scaled on hot / press (the button root).")]
        [SerializeField] private RectTransform body;

        [SerializeField] private TMP_Text label;

        [Header("Primary")]
        [SerializeField] private Image glow;
        [Tooltip("The shine band (inside a RectMask2D the size of the panel).")]
        [SerializeField] private RectTransform shine;

        [Header("Secondary / text")]
        [Tooltip("Accent bar (secondary) or underline (text); scaled in X from its pivot.")]
        [SerializeField] private RectTransform accent;
        [SerializeField] private TMP_Text arrow;
        [SerializeField] private Color labelColor = new Color(0.82f, 0.74f, 0.72f, 1f);
        [SerializeField] private Color labelHot = Color.white;

        [Header("Audio")]
        [SerializeField] private AudioCue hoverCue;
        [SerializeField] private AudioCue pressCue;
        [Tooltip("Primary only: a low rumble on press.")]
        [SerializeField] private AudioCue rumbleCue;

        private float hot;
        private bool selected;
        private float shineT = 1f;
        private float press;

        public void Configure(Style look, RectTransform root, TMP_Text text, Image glowImage, RectTransform shineBand, RectTransform accentBar, TMP_Text arrowText,
            Color normal, Color highlighted, AudioCue hover, AudioCue pressed, AudioCue rumble)
        {
            style = look;
            body = root;
            label = text;
            glow = glowImage;
            shine = shineBand;
            accent = accentBar;
            arrow = arrowText;
            labelColor = normal;
            labelHot = highlighted;
            hoverCue = hover;
            pressCue = pressed;
            rumbleCue = rumble;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Selectable s = GetComponent<Selectable>();
            if (s != null && s.interactable && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (selected) return;
            selected = true;
            shineT = 0f;
            AudioService.PlayUiCue(hoverCue);
        }

        public void OnDeselect(BaseEventData eventData) => selected = false;

        public void OnPointerDown(PointerEventData eventData) => Press();

        public void OnSubmit(BaseEventData eventData) => Press();

        private void Press()
        {
            press = 1f;
            AudioService.PlayUiCue(pressCue);
            if (style != Style.Primary) return;
            AudioService.PlayUiCue(rumbleCue);
            UiShake.Kick(0.5f, 0.2f);
        }

        private void OnDisable()
        {
            selected = false;
            hot = 0f;
            Apply();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hot = Mathf.MoveTowards(hot, selected ? 1f : 0f, dt * 7f);
            press = Mathf.MoveTowards(press, 0f, dt * 6f);
            shineT = Mathf.Min(1f, shineT + dt / 0.55f);
            Apply();
        }

        private void Apply()
        {
            float ease = hot * hot * (3f - 2f * hot);
            if (body != null)
            {
                float s = style == Style.Primary ? 1f + 0.03f * ease - 0.06f * press : 1f - 0.02f * press;
                body.localScale = new Vector3(s, s, 1f);
            }

            if (label != null) label.color = Color.Lerp(labelColor, labelHot, ease);
            if (glow != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f);
                Color c = glow.color;
                c.a = Mathf.Lerp(0.35f + 0.25f * pulse, 1f, ease);
                glow.color = c;
            }

            if (shine != null)
            {
                float w = ((RectTransform)shine.parent).rect.width;
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.7f, w * 0.7f, shineT), 0f);
                shine.gameObject.SetActive(shineT < 1f);
            }

            if (accent != null) accent.localScale = new Vector3(Mathf.Max(0.0001f, ease), 1f, 1f);
            if (arrow != null)
            {
                arrow.alpha = ease;
                arrow.rectTransform.anchoredPosition = new Vector2(-12f * (1f - ease), 0f);
            }
        }
    }
}
