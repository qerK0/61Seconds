using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>Small helpers to build uGUI + TMP hierarchies from code (themed when Theme is set).</summary>
    public static class UiFactory
    {
        private const int UiLayer = 5;

        /// <summary>Applied to every label/button made while set (TestSceneUi sets it for the build).</summary>
        public static UITheme Theme { get; set; }

        /// <summary>9-sliced rounded sprite for panels and buttons. Optional.</summary>
        public static Sprite Rounded { get; set; }

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

        /// <summary>Rounded (9-sliced) panel background.</summary>
        public static Image MakePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, bool raycast = false)
        {
            Image image = MakeImage(name, parent, anchorMin, anchorMax, color, raycast);
            MakeRound(image);
            return image;
        }

        public static void MakeRound(Image image)
        {
            if (image == null || Rounded == null) return;
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
        }

        public static TextMeshProUGUI MakeLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, Color color, bool bold = false)
        {
            RectTransform rect = MakeRect(name, parent, anchorMin, anchorMax);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = Mathf.Max(16f, fontSize);
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.richText = true;
            if (Theme != null && Theme.Regular != null)
            {
                // Real weights instead of faux bold.
                label.font = bold ? Theme.Bold : Theme.Regular;
            }
            else if (bold)
            {
                label.fontStyle = FontStyles.Bold;
            }

            label.text = string.Empty;
            return label;
        }

        /// <summary>A label styled by a theme role (font, size, colour, spacing).</summary>
        public static TextMeshProUGUI MakeLabel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, TextRole role, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = MakeLabel(name, parent, anchorMin, anchorMax, 18f, alignment, Color.white);
            if (Theme != null) Theme.Apply(label, role);
            return label;
        }

        public static Button MakeButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color, float fontSize, out TextMeshProUGUI label)
        {
            Image background = MakeImage(name, parent, anchorMin, anchorMax, color, raycast: true);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            label = MakeLabel("Label", background.transform, Vector2.zero, Vector2.one, fontSize, TextAlignmentOptions.Center, Color.white, bold: true);
            if (Theme != null)
            {
                MakeRound(background);
                label.font = Theme.SemiBold;
                Style(button, background, color);
            }

            return button;
        }

        /// <summary>Hover / pressed / disabled tints derived from the base colour (ColorTint over a white graphic).</summary>
        public static void Style(Button button, Image background, Color baseColor)
        {
            background.color = Color.white;
            ColorBlock colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
            colors.selectedColor = baseColor;
            colors.pressedColor = Color.Lerp(baseColor, Theme != null ? Theme.Accent : Color.white, 0.35f);
            colors.disabledColor = new Color(baseColor.r * 0.6f, baseColor.g * 0.6f, baseColor.b * 0.6f, baseColor.a * 0.7f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
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
