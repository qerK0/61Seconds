using System.Collections.Generic;
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
    }
}
