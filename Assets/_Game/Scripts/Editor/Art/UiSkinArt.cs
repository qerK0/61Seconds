using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Pixel-art inventory skin from Art/uı/examples.png (480x320): Point filter, uncompressed, no mips, sliced
    /// into named sprites (backpack, backpack_body, slot_cell, key_panel). The backpack's drawn 2x4 cell grid
    /// cannot be 9-sliced, so bag_frame.png is generated next to it: the backpack body with its interior filled
    /// flat, 9-sliced at its border. Pixel rects were measured from the sheet (top-left origin, see comments).
    /// </summary>
    public static class UiSkinArt
    {
        public const string SheetPath = "Assets/_Game/Art/uı/examples.png";
        public const string FramePath = "Assets/_Game/Art/uı/bag_frame.png";

        /// <summary>Canvas pixels per sheet pixel (integer, so pixels stay crisp at 1080p).</summary>
        public const int Scale = 3;

        // Sheet rects as (x, yTop, w, h) in source pixels.
        private static readonly RectInt Backpack = new RectInt(102, 16, 100, 140);
        private static readonly RectInt BackpackBody = new RectInt(128, 16, 64, 95);
        private static readonly RectInt Cell = new RectInt(145, 33, 14, 14);
        private static readonly RectInt KeyPanel = new RectInt(324, 20, 40, 24);

        // Inside the body's inner frame line (body-local, top-left origin): filled flat for bag_frame.png.
        private static readonly RectInt BodyInterior = new RectInt(7, 10, 50, 77);

        public sealed class Sprites
        {
            public Sprite Frame;
            public Sprite Cell;
            public Sprite KeyPanel;
        }

        public static Sprites Import()
        {
            var result = new Sprites();
            if (!(AssetImporter.GetAtPath(SheetPath) is TextureImporter importer)) return result;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.spritePixelsPerUnit = 100f;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            int sheetHeight = 320;
            importer.GetSourceTextureWidthAndHeight(out _, out sheetHeight);
            var existing = new List<SpriteRect>(provider.GetSpriteRects());
            SpriteRect[] rects =
            {
                Rect(existing, "backpack", Backpack, sheetHeight, Vector4.zero),
                Rect(existing, "backpack_body", BackpackBody, sheetHeight, new Vector4(7, 8, 7, 10)),
                Rect(existing, "slot_cell", Cell, sheetHeight, new Vector4(3, 3, 3, 3)),
                Rect(existing, "key_panel", KeyPanel, sheetHeight, new Vector4(4, 4, 4, 4))
            };
            provider.SetSpriteRects(rects);
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                var pairs = new List<SpriteNameFileIdPair>();
                foreach (SpriteRect r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
                names.SetNameFileIdPairs(pairs);
            }

            provider.Apply();
            importer.SaveAndReimport();

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(SheetPath))
            {
                if (!(o is Sprite s)) continue;
                if (s.name == "slot_cell") result.Cell = s;
                else if (s.name == "key_panel") result.KeyPanel = s;
            }

            result.Frame = BuildFrame(sheetHeight);
            return result;
        }

        private static SpriteRect Rect(List<SpriteRect> existing, string name, RectInt r, int sheetHeight, Vector4 border)
        {
            // Keep the sprite id on re-runs so references survive.
            SpriteRect rect = existing.Find(e => e.name == name) ?? new SpriteRect { name = name, spriteID = GUID.Generate() };
            rect.rect = new Rect(r.x, sheetHeight - r.y - r.height, r.width, r.height);
            rect.alignment = SpriteAlignment.Center;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.border = border;
            return rect;
        }

        /// <summary>Backpack body with a flat interior (no drawn cells), 9-sliced at its frame (L7 B8 R7 T10).</summary>
        private static Sprite BuildFrame(int sheetHeight)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(SheetPath));
            var frame = new Texture2D(BackpackBody.width, BackpackBody.height, TextureFormat.RGBA32, false);
            Color32 fill = source.GetPixel(BackpackBody.x + BodyInterior.x + 1, sheetHeight - 1 - (BackpackBody.y + BodyInterior.y + 1));
            for (int y = 0; y < BackpackBody.height; y++)
            {
                for (int x = 0; x < BackpackBody.width; x++)
                {
                    int top = BackpackBody.height - 1 - y; // body-local row from the top
                    bool inside = x >= BodyInterior.x && x < BodyInterior.xMax && top >= BodyInterior.y && top < BodyInterior.yMax;
                    Color32 c = inside ? fill : (Color32)source.GetPixel(BackpackBody.x + x, sheetHeight - BackpackBody.y - BackpackBody.height + y);
                    frame.SetPixel(x, y, c);
                }
            }

            File.WriteAllBytes(FramePath, frame.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(frame);
            AssetDatabase.ImportAsset(FramePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(FramePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(7, 8, 7, 10);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        }
    }
}
