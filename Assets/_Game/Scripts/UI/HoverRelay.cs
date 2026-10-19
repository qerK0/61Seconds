using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SixtyOne.UI
{
    /// <summary>Forwards pointer enter/exit to callbacks (tooltips).</summary>
    public sealed class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Entered;
        public Action Exited;

        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => Exited?.Invoke();
    }
}
