using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>Small helpers to build uGUI + TMP hierarchies from code.</summary>
    public static class UiFactory
    {
        private const int UiLayer = 5;

        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image MakeImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, bool raycast = false, Sprite sprite = null)
        {
            RectTransform rect = MakeRect(name, parent, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }

            return image;
        }

        public static TextMeshProUGUI MakeLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, Color color, bool bold = false)
        {
            RectTransform rect = MakeRect(name, parent, anchorMin, anchorMax);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.richText = true;
            if (bold) label.fontStyle = FontStyles.Bold;
            label.text = string.Empty;
            return label;
        }

        public static Button MakeButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, float fontSize, out TextMeshProUGUI label)
        {
            Image background = MakeImage(name, parent, anchorMin, anchorMax, color, raycast: true);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = MakeLabel("Label", background.transform, Vector2.zero, Vector2.one, fontSize, TextAlignmentOptions.Center, Color.white, bold: true);
            return button;
        }

        /// <summary>Adds a CanvasGroup, hidden and non-blocking by default.</summary>
        public static CanvasGroup MakeGroup(GameObject go, bool visible = false)
        {
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            return group;
        }
    }
}
