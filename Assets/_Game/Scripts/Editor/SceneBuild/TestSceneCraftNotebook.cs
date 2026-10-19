using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builds the "Craft Defteri" (CraftNotebook): an off-white lined page (#F3EEDF, faint blue lines, red margin)
    /// tilted −1.5° with a soft shadow, a strip of punched holes under ~10 dark spiral rings along the top, a
    /// hand-underlined Caveat title, one handwritten line per recipe (+ the bedsheet rope), two pasted-in slots and a
    /// hand-drawn "Birleştir" button; a small notebook button sits next to the bag header.
    /// </summary>
    public static class TestSceneCraftNotebook
    {
        /// <summary>Paper rustle played when the page opens (set by the scene builder).</summary>
        public static AudioCue RustleCue { get; set; }

        private static readonly Color Paper = new Color32(0xF3, 0xEE, 0xDF, 0xFF);
        private static readonly Color Line = new Color(0.45f, 0.6f, 0.85f, 0.35f);
        private static readonly Color Margin = new Color(0.85f, 0.25f, 0.25f, 0.55f);
        private static readonly Color Ink = new Color(0.16f, 0.2f, 0.32f, 1f);
        private static readonly Color Pencil = new Color(0.35f, 0.35f, 0.38f, 0.85f);
        private static readonly Color Ring = new Color(0.27f, 0.28f, 0.3f, 1f);

        public static void Build(Transform root, GameManager gameManager, GameContent content, SurvivalController survival, RecipeBook recipeBook, PlaceholderSprites art, UITheme theme)
        {
            TMP_FontAsset hand = theme != null ? theme.Hand : null;
            TMP_FontAsset handBold = theme != null ? theme.HandBold : null;

            RectTransform holder = UiFactory.MakeRect("CraftNotebook", root, new Vector2(TestSceneUi.ColumnLeft - 0.255f, 0.16f), new Vector2(TestSceneUi.ColumnLeft - 0.012f, 0.86f));
            CanvasGroup group = UiFactory.MakeGroup(holder.gameObject);
            RectTransform page = UiFactory.MakeRect("Page", holder, Vector2.zero, Vector2.one);
            page.localRotation = Quaternion.Euler(0f, 0f, -1.5f);

            Image shadow = UiFactory.MakeImage("Shadow", page, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f), sprite: art.Rounded);
            shadow.type = Image.Type.Sliced;
            shadow.rectTransform.offsetMin = new Vector2(8f, -10f);
            shadow.rectTransform.offsetMax = new Vector2(8f, -10f);
            Image sheet = UiFactory.MakeImage("Sheet", page, Vector2.zero, Vector2.one, Paper, raycast: true);

            // Ruled lines and the red margin.
            for (float y = 0.08f; y < 0.86f; y += 0.045f)
            {
                UiFactory.MakeImage("Rule", sheet.transform, new Vector2(0f, y), new Vector2(1f, y), Line).rectTransform.sizeDelta = new Vector2(0f, 2f);
            }

            UiFactory.MakeImage("Margin", sheet.transform, new Vector2(0.11f, 0f), new Vector2(0.11f, 0.93f), Margin).rectTransform.sizeDelta = new Vector2(2f, 0f);

            // Spiral: punched holes along the top edge with dark rings over them.
            const int rings = 10;
            for (int i = 0; i < rings; i++)
            {
                float x = 0.07f + i * (0.86f / (rings - 1));
                Image hole = UiFactory.MakeImage("Hole", sheet.transform, new Vector2(x, 0.955f), new Vector2(x, 0.955f), new Color(0.12f, 0.12f, 0.13f, 1f), sprite: art.Dot);
                hole.rectTransform.sizeDelta = new Vector2(12f, 12f);
                Image ring = UiFactory.MakeImage("Ring", sheet.transform, new Vector2(x, 0.975f), new Vector2(x, 0.975f), Ring, sprite: art.Rounded);
                ring.type = Image.Type.Sliced;
                ring.rectTransform.sizeDelta = new Vector2(9f, 34f);
                Image shine = UiFactory.MakeImage("Shine", ring.transform, new Vector2(0.25f, 0.15f), new Vector2(0.45f, 0.85f), new Color(1f, 1f, 1f, 0.25f));
                shine.raycastTarget = false;
            }

            TextMeshProUGUI title = Hand("Title", sheet.transform, new Vector2(0.14f, 0.86f), new Vector2(0.95f, 0.94f), 40f, handBold ?? hand, TextAlignmentOptions.MidlineLeft);
            SerializedWire.SetTextKey(title.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.CraftTitle);
            Image underline = UiFactory.MakeImage("Underline", sheet.transform, new Vector2(0.14f, 0.865f), new Vector2(0.6f, 0.865f), Ink);
            underline.rectTransform.sizeDelta = new Vector2(0f, 3f);
            underline.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 0.8f);

            // One line per recipe, then the bedsheet rope (recipe null).
            var recipes = new List<Recipe>();
            if (recipeBook != null) foreach (Recipe r in recipeBook.Recipes) if (r != null) recipes.Add(r);
            recipes.Add(null);
            LevelConfig config = survival != null ? survival.Config : null;
            var texts = new List<Object>();
            var results = new List<Object>();
            var scribbles = new List<Object>();
            float top = 0.83f;
            float step = Mathf.Min(0.09f, 0.5f / Mathf.Max(1, recipes.Count));
            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe r = recipes[i];
                float y1 = top - i * step, y0 = y1 - step;
                Transform row = UiFactory.MakeRect("Recipe" + i, sheet.transform, new Vector2(0.13f, y0), new Vector2(0.97f, y1));
                Item a = r != null ? r.InputA : config != null ? config.BedsheetItem : null;
                Item b = r != null ? r.InputB : null;
                Item output = r != null ? r.Output : config != null ? config.BedsheetRopeItem : null;
                Icon(row, a, 0f);
                if (b != null) Icon(row, b, 0.09f);
                Image result = Icon(row, output, 0.18f);
                results.Add(result);
                TextMeshProUGUI text = Hand("Text", row, new Vector2(0.28f, 0f), new Vector2(1f, 1f), 26f, hand, TextAlignmentOptions.MidlineLeft);
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 26f;
                texts.Add(text);

                // Pencil scribble over an undiscovered result: three crossed strokes.
                RectTransform scribble = UiFactory.MakeRect("Scribble", row, new Vector2(0.17f, 0.15f), new Vector2(0.27f, 0.85f));
                for (int s = 0; s < 3; s++)
                {
                    Image stroke = UiFactory.MakeImage("Stroke", scribble, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Pencil);
                    stroke.rectTransform.sizeDelta = new Vector2(0f, 3f);
                    stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -30f + s * 30f);
                }

                scribbles.Add(scribble.gameObject);
            }

            // Two pasted-in slots (slightly crooked tape-on cards) and the hand-drawn button.
            var slotIcons = new Object[2];
            var slotLabels = new Object[2];
            var slotButtons = new Object[2];
            for (int i = 0; i < 2; i++)
            {
                float x0 = 0.14f + i * 0.4f;
                Image card = UiFactory.MakeImage("Slot" + i, sheet.transform, new Vector2(x0, 0.13f), new Vector2(x0 + 0.32f, 0.3f), new Color(1f, 1f, 1f, 0.9f), raycast: true, sprite: art.Rounded);
                card.type = Image.Type.Sliced;
                card.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 2f : -2.5f);
                Image tape = UiFactory.MakeImage("Tape", card.transform, new Vector2(0.3f, 0.9f), new Vector2(0.7f, 1.08f), new Color(1f, 0.95f, 0.7f, 0.7f));
                tape.raycastTarget = false;
                Image icon = UiFactory.MakeImage("Icon", card.transform, new Vector2(0.3f, 0.35f), new Vector2(0.7f, 0.92f), Color.white);
                icon.preserveAspect = true;
                icon.enabled = false;
                slotIcons[i] = icon;
                TextMeshProUGUI label = Hand("Label", card.transform, new Vector2(0.03f, 0f), new Vector2(0.97f, 0.36f), 20f, hand, TextAlignmentOptions.Center);
                slotLabels[i] = label;
                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                slotButtons[i] = button;
            }

            TextMeshProUGUI plus = Hand("Plus", sheet.transform, new Vector2(0.46f, 0.15f), new Vector2(0.54f, 0.28f), 34f, handBold ?? hand, TextAlignmentOptions.Center);
            plus.text = "+";

            Image buttonInk = UiFactory.MakeImage("Combine", sheet.transform, new Vector2(0.3f, 0.025f), new Vector2(0.7f, 0.1f), new Color(Ink.r, Ink.g, Ink.b, 0.12f), raycast: true, sprite: art.Rounded);
            buttonInk.type = Image.Type.Sliced;
            buttonInk.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 1.2f);
            TextMeshProUGUI combineLabel = Hand("Label", buttonInk.transform, Vector2.zero, Vector2.one, 28f, handBold ?? hand, TextAlignmentOptions.Center);
            SerializedWire.SetTextKey(combineLabel.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.CraftCombine);
            Button combine = buttonInk.gameObject.AddComponent<Button>();
            combine.targetGraphic = buttonInk;

            TextMeshProUGUI hint = Hand("Hint", sheet.transform, new Vector2(0.14f, 0.3f), new Vector2(0.97f, 0.36f), 20f, hand, TextAlignmentOptions.MidlineLeft);
            hint.color = Pencil;
            SerializedWire.SetTextKey(hint.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.CraftHint);

            // The notebook button next to the bag header.
            Button open = null;
            Transform bag = root.Find("RightColumn/Column/Bag");
            if (bag != null)
            {
                Image icon = UiFactory.MakeImage("NotebookButton", bag, new Vector2(0.32f, 0.956f), new Vector2(0.6f, 0.999f), Paper, raycast: true, sprite: art.Rounded);
                icon.type = Image.Type.Sliced;
                TextMeshProUGUI c = Hand("Label", icon.transform, Vector2.zero, Vector2.one, 22f, handBold ?? hand, TextAlignmentOptions.Center);
                c.enableAutoSizing = true;
                c.fontSizeMin = 16f;
                c.fontSizeMax = 22f;
                SerializedWire.SetTextKey(c.gameObject.AddComponent<LocalizedText>(), "key", UiKeys.CraftOpen);
                open = icon.gameObject.AddComponent<Button>();
                open.targetGraphic = icon;
            }

            CraftNotebook notebook = holder.gameObject.AddComponent<CraftNotebook>();
            SerializedWire.Set(notebook, "gameManager", gameManager);
            SerializedWire.Set(notebook, "content", content);
            SerializedWire.Set(notebook, "survival", survival);
            SerializedWire.Set(notebook, "group", group);
            SerializedWire.Set(notebook, "page", page);
            SerializedWire.SetArray(notebook, "recipes", recipes.ConvertAll(r => (Object)r).ToArray());
            SerializedWire.SetArray(notebook, "lineTexts", texts.ToArray());
            SerializedWire.SetArray(notebook, "resultIcons", results.ToArray());
            SerializedWire.SetArray(notebook, "scribbles", scribbles.ToArray());
            SerializedWire.SetArray(notebook, "slotIcons", slotIcons);
            SerializedWire.SetArray(notebook, "slotLabels", slotLabels);
            SerializedWire.SetArray(notebook, "slotButtons", slotButtons);
            SerializedWire.Set(notebook, "combineButton", combine);
            if (open != null) SerializedWire.Set(notebook, "openButton", open);
            if (RustleCue != null) SerializedWire.Set(notebook, "rustleCue", RustleCue);
        }

        private static Image Icon(Transform row, Item item, float x)
        {
            Image icon = UiFactory.MakeImage("Icon", row, new Vector2(x, 0.05f), new Vector2(x + 0.08f, 0.95f), Color.white);
            icon.preserveAspect = true;
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = icon.sprite != null;
            return icon;
        }

        /// <summary>A handwriting label (Caveat), ink-coloured, never bold-styled (the readable-text pass would swap the font).</summary>
        private static TextMeshProUGUI Hand(string name, Transform parent, Vector2 min, Vector2 max, float size, TMP_FontAsset font, TextAlignmentOptions align)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel(name, parent, min, max, size, align, Ink);
            if (font != null) label.font = font;
            return label;
        }
    }
}
