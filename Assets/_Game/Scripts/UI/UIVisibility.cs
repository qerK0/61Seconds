using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Show/hide helpers for CanvasGroups (hidden groups never block world clicks).</summary>
    public static class UIVisibility
    {
        public static void Set(CanvasGroup group, bool visible, bool blocksWhenVisible = true)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible && blocksWhenVisible;
        }
    }
}
