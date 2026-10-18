using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Item icons from Assets/_Game/Art/items: imported as single 2D sprites (alpha is transparency, no mipmaps, max 256)
    /// with a PPU that makes the longest side ~0.42 world units, so the same sprite reads in the bag, the key-item HUD
    /// and as a world pickup. Items without a file keep the placeholder.
    /// </summary>
    public static class ItemIconArt
    {
        public const string Folder = "Assets/_Game/Art/items";

        /// <summary>World size of an icon's longest side (before the room scale).</summary>
        public const float WorldSize = 0.42f;

        /// <summary>Item id → icon file name (without .png). crowbar and mask are unused.</summary>
        private static readonly Dictionary<string, string> Files = new Dictionary<string, string>
        {
            { "food", "bread" }, { "water", "water bottle" }, { "flashlight", "flashlight" }, { "gloves", "gloves" },
            { "bedsheet", "sheet" }, { "tape", "bant" }, { "gold", "golds" }, { "lighter", "lighter" },
            { "first_aid", "medkit" }, { "rope", "rope" }, { "bedsheet_rope", "sheet rope" }, { "whistle", "whsitle" },
            { "wrench", "wrench" }
        };

        /// <summary>Imports (if needed) and returns the icon of an item, or null.</summary>
        public static Sprite For(string itemId)
        {
            if (!Files.TryGetValue(itemId, out string file)) return null;
            string path = $"{Folder}/{file}.png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float ppu = Mathf.Max(1f, Mathf.Max(width, height) / WorldSize);
            bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(importer.spritePixelsPerUnit, ppu) || importer.mipmapEnabled || !importer.alphaIsTransparency || importer.maxTextureSize != 256;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 256;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Generated UI icons (one PNG per item; the world sprites are left as they are).</summary>
        public const string UiFolder = Folder + "/ui";

        /// <summary>UI icon size (px, square).</summary>
        private const int UiSize = 256;

        /// <summary>
        /// The item's UI icon: its art cropped to the opaque pixels, padded to a square (aspect kept), centre pivot,
        /// written to Art/items/ui/&lt;id&gt;_ui.png. Items without art get the shared placeholder (a clean outlined box).
        /// </summary>
        public static Sprite UiFor(string itemId)
        {
            string source = Files.TryGetValue(itemId, out string file) ? $"{Folder}/{file}.png" : null;
            if (source == null || !File.Exists(source)) return Placeholder();
            return WriteSprite($"{UiFolder}/{itemId}_ui.png", SquareCrop(source));
        }

        private static Sprite Placeholder()
        {
            return WriteSprite($"{UiFolder}/placeholder_ui.png", PlaceholderPixels());
        }

        /// <summary>Opaque bounds of the source, centred in a transparent square, scaled to UiSize.</summary>
        private static Color32[] SquareCrop(string path)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(path));
            int w = source.width, h = source.height;
            Color32[] pixels = source.GetPixels32();
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 8) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX)
            {
                minX = 0;
                minY = 0;
                maxX = w - 1;
                maxY = h - 1;
            }

            float side = Mathf.Max(maxX - minX + 1, maxY - minY + 1);
            float cx = (minX + maxX + 1) * 0.5f, cy = (minY + maxY + 1) * 0.5f;
            var result = new Color32[UiSize * UiSize];
            for (int y = 0; y < UiSize; y++)
            {
                for (int x = 0; x < UiSize; x++)
                {
                    float sx = cx + ((x + 0.5f) / UiSize - 0.5f) * side;
                    float sy = cy + ((y + 0.5f) / UiSize - 0.5f) * side;
                    bool inside = sx >= minX && sx <= maxX + 1 && sy >= minY && sy <= maxY + 1;
                    result[y * UiSize + x] = inside ? (Color32)source.GetPixelBilinear(sx / w, sy / h) : new Color32(0, 0, 0, 0);
                }
            }

            Object.DestroyImmediate(source);
            return result;
        }

        /// <summary>Rounded outlined box (white, 30% fill) for items without art.</summary>
        private static Color32[] PlaceholderPixels()
        {
            var result = new Color32[UiSize * UiSize];
            const float half = UiSize * 0.36f, radius = UiSize * 0.1f, border = UiSize * 0.035f;
            for (int y = 0; y < UiSize; y++)
            {
                for (int x = 0; x < UiSize; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - UiSize * 0.5f) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - UiSize * 0.5f) - (half - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    byte a = d > 0.5f ? (byte)0 : d > -border ? (byte)235 : (byte)70;
                    result[y * UiSize + x] = new Color32(255, 255, 255, a);
                }
            }

            return result;
        }

        /// <summary>Writes the PNG (only when it changed) and imports it as a centre-pivot UI sprite.</summary>
        private static Sprite WriteSprite(string path, Color32[] pixels)
        {
            Directory.CreateDirectory(UiFolder);
            var texture = new Texture2D(UiSize, UiSize, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.spritePivot != new Vector2(0.5f, 0.5f) || importer.mipmapEnabled || importer.maxTextureSize != UiSize;
                if (changed)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.maxTextureSize = UiSize;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
