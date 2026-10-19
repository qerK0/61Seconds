using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Placeholder sprites generated as PNGs (white shapes, tinted by SpriteRenderer color).</summary>
    public sealed class PlaceholderSprites
    {
        public Sprite Tile;
        public Sprite AnchorDecal;
        public Sprite Block;
        public Sprite Character;
        public Sprite Ring;
        public Sprite Dot;
        public Sprite Lock;
        /// <summary>Soft ellipse under characters' feet.</summary>
        public Sprite Shadow;
        /// <summary>Soft white ellipse (tinted hazes).</summary>
        public Sprite Glow;
        /// <summary>Rounded rectangle, 9-sliced (UI panels and buttons).</summary>
        public Sprite Rounded;
        /// <summary>Upward chevron (door hover).</summary>
        public Sprite Arrow;
    }

    /// <summary>Writes simple shapes to Assets/_Game/Art/Placeholder and imports them as sprites.</summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/_Game/Art/Placeholder";
        public const float PixelsPerUnit = 128f;

        public static PlaceholderSprites Generate()
        {
            AssetUtil.EnsureFolder(Folder);
            var sprites = new PlaceholderSprites
            {
                Tile = Make("ph_tile", 128, 64, new Vector2(0.5f, 0.5f), Diamond(0.06f, 0.8f)),
                AnchorDecal = Make("ph_anchor", 128, 64, new Vector2(0.5f, 0.5f), Diamond(0.12f, 0.55f)),
                Block = Make("ph_block", 64, 128, new Vector2(0.5f, 0f), Block),
                Character = Make("ph_character", 48, 96, new Vector2(0.5f, 0f), Capsule),
                Ring = Make("ph_ring", 96, 48, new Vector2(0.5f, 0.5f), Ring),
                Dot = Make("ph_dot", 48, 48, new Vector2(0.5f, 0.5f), Dot),
                Lock = Make("ph_lock", 32, 32, new Vector2(0.5f, 0.5f), Lock),
                Shadow = Make("ph_shadow", 128, 64, new Vector2(0.5f, 0.5f), Shadow),
                Glow = Make("ph_glow", 128, 64, new Vector2(0.5f, 0.5f), Glow),
                Rounded = Make("ph_rounded", 64, 64, new Vector2(0.5f, 0.5f), Rounded, new Vector4(22f, 22f, 22f, 22f)),
                Arrow = Make("ph_arrow", 48, 48, new Vector2(0.5f, 0.5f), Arrow)
            };
            return sprites;
        }

        private static Sprite Make(string fileName, int width, int height, Vector2 pivot, Func<float, float, float, float, Color> shader, Vector4 border = default)
        {
            string path = $"{Folder}/{fileName}.png";
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Normalized coords in 0..1 using pixel centers.
                    pixels[y * width + x] = shader((x + 0.5f) / width, (y + 0.5f) / height, width, height);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Func<float, float, float, float, Color> Diamond(float border, float fillAlpha)
        {
            return (u, v, w, h) =>
            {
                float d = Mathf.Abs(u - 0.5f) * 2f + Mathf.Abs(v - 0.5f) * 2f;
                if (d > 1f) return Color.clear;
                return d > 1f - border ? new Color(0.55f, 0.55f, 0.55f, 1f) : new Color(1f, 1f, 1f, fillAlpha);
            };
        }

        private static Color Block(float u, float v, float w, float h)
        {
            bool edge = u < 0.06f || u > 0.94f || v < 0.03f || v > 0.97f;
            if (edge) return new Color(0.45f, 0.45f, 0.45f, 1f);
            return v > 0.8f ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);
        }

        private static Color Capsule(float u, float v, float w, float h)
        {
            float px = u * w;
            float py = v * h;
            float r = w * 0.5f;
            float cy = Mathf.Clamp(py, r, h - r);
            float dist = Vector2.Distance(new Vector2(px, py), new Vector2(r, cy));
            if (dist > r) return Color.clear;
            return dist > r - 2.5f ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.white;
        }

        private static Color Ring(float u, float v, float w, float h)
        {
            float d = Mathf.Sqrt(Mathf.Pow((u - 0.5f) * 2f, 2f) + Mathf.Pow((v - 0.5f) * 2f, 2f));
            return d <= 1f && d >= 0.72f ? Color.white : Color.clear;
        }

        private static Color Dot(float u, float v, float w, float h)
        {
            float d = Mathf.Sqrt(Mathf.Pow((u - 0.5f) * 2f, 2f) + Mathf.Pow((v - 0.5f) * 2f, 2f));
            if (d > 1f) return Color.clear;
            return d > 0.82f ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.white;
        }

        private static Color Glow(float u, float v, float w, float h)
        {
            Color c = Shadow(u, v, w, h);
            return new Color(1f, 1f, 1f, c.a);
        }

        private static Color Shadow(float u, float v, float w, float h)
        {
            float d = Mathf.Sqrt(Mathf.Pow((u - 0.5f) * 2f, 2f) + Mathf.Pow((v - 0.5f) * 2f, 2f));
            float a = Mathf.Clamp01(1f - d);
            return new Color(0f, 0f, 0f, a * a * (3f - 2f * a));
        }

        private static Color Rounded(float u, float v, float w, float h)
        {
            // 64 px square, 18 px corner radius, 1 px anti-aliased edge.
            const float radius = 18f;
            float px = u * w;
            float py = v * h;
            float cx = Mathf.Clamp(px, radius, w - radius);
            float cy = Mathf.Clamp(py, radius, h - radius);
            float dist = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy));
            float a = Mathf.Clamp01(radius - dist + 0.5f);
            return new Color(1f, 1f, 1f, a);
        }

        private static Color Arrow(float u, float v, float w, float h)
        {
            // Upward chevron: two strokes meeting at the top centre.
            float x = Mathf.Abs(u - 0.5f);
            float target = 0.78f - x * 1.1f;
            float thickness = 0.13f;
            bool stroke = x < 0.42f && Mathf.Abs(v - target) < thickness;
            return stroke ? Color.white : Color.clear;
        }

        private static Color Lock(float u, float v, float w, float h)
        {
            bool body = u > 0.2f && u < 0.8f && v > 0.08f && v < 0.55f;
            float d = Mathf.Sqrt(Mathf.Pow((u - 0.5f) / 0.25f, 2f) + Mathf.Pow((v - 0.55f) / 0.32f, 2f));
            bool shackle = v >= 0.55f && d <= 1f && d >= 0.62f;
            return body || shackle ? Color.white : Color.clear;
        }
    }
}
