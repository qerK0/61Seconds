using SixtyOne.Nodes;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Puts an item's UI icon in a slot image the same way everywhere (bag, loot window, pockets): the UI sprite
    /// (centre pivot, square), aspect kept, no sprite mesh, and a clean transform on every refresh so a recycled
    /// slot never keeps an old position, size, scale or rotation.
    /// </summary>
    public static class SlotIcon
    {
        /// <summary>Inset from the slot edges (fraction of the slot).</summary>
        public const float Inset = 0.12f;

        /// <summary>Icon stretched over the slot with the fixed inset. No item = hidden.</summary>
        public static void Apply(Image image, Item item)
        {
            Apply(image, item, new Vector2(Inset, Inset), new Vector2(1f - Inset, 1f - Inset));
        }

        /// <summary>Icon stretched over [anchorMin, anchorMax] of its parent (a slot area above a name label).</summary>
        public static void Apply(Image image, Item item, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (image == null) return;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            SetSprite(image, item);
        }

        /// <summary>Sprite + look only, for icons with an authored rect (the backpack pockets); scale and rotation reset.</summary>
        public static void SetSprite(Image image, Item item)
        {
            if (image == null) return;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            image.preserveAspect = true;
            image.useSpriteMesh = false;
            image.type = Image.Type.Simple;
            Sprite sprite = item != null ? item.UiIcon : null;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
