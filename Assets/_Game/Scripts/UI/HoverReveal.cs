using UnityEngine;
using UnityEngine.EventSystems;

namespace SixtyOne.UI
{
    /// <summary>Shows a CanvasGroup only while the pointer is over this element (bag slot names under the backpack).</summary>
    public sealed class HoverReveal : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Shown on hover, hidden otherwise.")]
        [SerializeField] private CanvasGroup target;

        private void OnEnable() => Set(false);

        private void OnDisable() => Set(false);

        public void OnPointerEnter(PointerEventData eventData) => Set(true);

        public void OnPointerExit(PointerEventData eventData) => Set(false);

        private void Set(bool visible)
        {
            if (target != null) target.alpha = visible ? 1f : 0f;
        }
    }
}
