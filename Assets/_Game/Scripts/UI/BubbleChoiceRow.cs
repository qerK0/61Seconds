using System;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// One choice row under a speech bubble: number badge (keys 1-4), label, cost chip (−3 sn / +ağır / lock),
    /// hover fill + left bar in the speaker colour, 0.97 press scale. ShowLocked rows are dimmed with their reason;
    /// ShowLockedFatal and trap rows look normal on purpose. Also used as the small "Kısa kes" pill.
    /// </summary>
    public sealed class BubbleChoiceRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("The clickable button.")]
        [SerializeField] private Button button;

        [Tooltip("Fades the row in (stagger) and dims locked rows.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Number badge label (1-4). Optional (pills have none).")]
        [SerializeField] private TMP_Text badge;

        [Tooltip("Choice label.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Cost chip label. Optional.")]
        [SerializeField] private TMP_Text cost;

        [Tooltip("Lock icon in the chip spot. Optional.")]
        [SerializeField] private GameObject lockIcon;

        [Tooltip("Locked reason under the label. Optional.")]
        [SerializeField] private TMP_Text reason;

        [Tooltip("Accent fill shown on hover.")]
        [SerializeField] private Image hoverFill;

        [Tooltip("3 px left bar shown on hover.")]
        [SerializeField] private Image hoverBar;

        [Tooltip("Row height (px) without / with a locked reason line.")]
        [SerializeField] private Vector2 heights = new Vector2(46f, 66f);

        private float appearAt;
        private bool locked;

        /// <summary>Height this row needs (px).</summary>
        public float Height { get; private set; }

        /// <summary>The choice index it plays (-1 for a pill).</summary>
        public int ChoiceIndex { get; private set; } = -1;

        public bool Clickable => button != null && button.interactable;

        /// <summary>Fills the row. number = badge (1-based); delay = stagger before it fades in.</summary>
        public void Bind(ChoiceView view, int number, Color accent, float delay, Action onClick)
        {
            ChoiceIndex = view.Index;
            // Fatal locks are clickable and look normal (no spoilers); plain locks are dimmed with a reason.
            locked = view.Locked && !view.Fatal;
            if (label != null) label.text = view.Choice.LabelKey.Display;
            if (badge != null) badge.text = number.ToString();
            SetCost(view);
            if (reason != null)
            {
                reason.gameObject.SetActive(locked);
                TextKey key = view.Choice.LockedReasonKey;
                if (locked) reason.text = view.ReasonOverride != null ? TextDB.GetDisplay(view.ReasonOverride) : key.IsEmpty ? TextDB.GetDisplay(UiKeys.Locked) : key.Display;
            }

            Height = locked ? heights.y : heights.x;
            Wire(view.Clickable ? onClick : null, accent, delay);
        }

        /// <summary>A small pill (during-dialogue choice such as "Kısa kes").</summary>
        public void BindPill(ChoiceView view, Color accent, Action onClick)
        {
            ChoiceIndex = view.Index;
            locked = false;
            if (label != null) label.text = view.Choice.LabelKey.Display;
            SetCost(view);
            Height = heights.x;
            Wire(view.Clickable ? onClick : null, accent, 0f);
        }

        private void SetCost(ChoiceView view)
        {
            float seconds = view.Choice.TimeCost;
            string text = seconds > 0f ? TextDB.Format(UiKeys.TimeSpent, Mathf.CeilToInt(seconds)) : view.Choice.Heavy ? TextDB.GetDisplay(UiKeys.ChoiceHeavy) : string.Empty;
            if (seconds > 0f && view.Choice.Heavy) text += " " + TextDB.GetDisplay(UiKeys.ChoiceHeavy);
            if (cost != null)
            {
                cost.text = text;
                cost.gameObject.SetActive(!locked && text.Length > 0);
            }

            if (lockIcon != null) lockIcon.SetActive(locked);
        }

        private void Wire(Action onClick, Color accent, float delay)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = onClick != null;
            if (onClick != null) button.onClick.AddListener(() => onClick());
            if (hoverFill != null)
            {
                hoverFill.color = new Color(accent.r, accent.g, accent.b, 0.15f);
                hoverFill.enabled = false;
            }

            if (hoverBar != null)
            {
                hoverBar.color = accent;
                hoverBar.enabled = false;
            }

            appearAt = Time.unscaledTime + delay;
            if (group != null) group.alpha = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (group == null) return;
            float target = Time.unscaledTime >= appearAt ? (locked ? 0.55f : 1f) : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.12f);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetHover(Clickable);

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHover(false);
            transform.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Clickable) transform.localScale = Vector3.one * 0.97f;
        }

        public void OnPointerUp(PointerEventData eventData) => transform.localScale = Vector3.one;

        private void SetHover(bool on)
        {
            if (hoverFill != null) hoverFill.enabled = on;
            if (hoverBar != null) hoverBar.enabled = on;
        }

        /// <summary>Plays the row as if clicked (keyboard 1-4).</summary>
        public void Press()
        {
            if (Clickable) button.onClick.Invoke();
        }
    }
}
