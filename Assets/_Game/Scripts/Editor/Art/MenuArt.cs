using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Procedural UI art for the main menu and the end screen (no new hand art), written once to
    /// Art/Menu/Generated: the red primary-button gradient, a 2 px frame and a soft glow (9-slice), metal corner
    /// brackets, a diagonal shine, the hazard-stripe and scanline tiles, a vignette, soft dots / rings, film grain and
    /// the distressed title face texture. Also imports menu_bg / end_bg as full-screen sprites.
    /// </summary>
    public static class MenuArt
    {
        public const string Folder = "Assets/_Game/Art/Menu";
        public const string OutFolder = Folder + "/Generated";

        public sealed class Set
        {
            public Sprite MenuBackground, EndBackground;
            public Sprite RedGradient, Frame, Glow, Bracket, Shine, Vignette, Dot, Ring, Disc, Panel;
            public Texture2D Hazard, Scanlines, Grain, Scratch;
        }

        /// <summary>Writes (when changed) and imports every sprite / texture.</summary>
        public static Set Generate()
        {
            Directory.CreateDirectory(OutFolder);
            var set = new Set
            {
                MenuBackground = Background(Folder + "/menu_bg.png"),
                EndBackground = Background(Folder + "/end_bg.png"),
                RedGradient = Sprite("red_gradient", 16, 128, Vector4.zero, (x, y, w, h) =>
                {
                    // #6E0A0F at the bottom → #B3121A at the top.
                    Color c = Color.Lerp(Hex(0x6E0A0F), Hex(0xB3121A), y / (float)(h - 1));
                    return c;
                }),
                Frame = Sprite("frame_2px", 16, 16, new Vector4(4, 4, 4, 4), (x, y, w, h) =>
                    x < 2 || y < 2 || x >= w - 2 || y >= h - 2 ? Color.white : Clear),
                Glow = Sprite("glow", 128, 128, new Vector4(56, 56, 56, 56), (x, y, w, h) =>
                {
                    // Distance outside an inner rect (the panel edge at 40 px from the border) → soft falloff.
                    float dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - w * 0.5f) - (w * 0.5f - 40f));
                    float dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - h * 0.5f) - (h * 0.5f - 40f));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = dx <= 0f && dy <= 0f ? 1f : 0f;
                    float a = inside > 0f ? 0.35f : Mathf.Exp(-d * d / (2f * 13f * 13f));
                    return new Color(1f, 1f, 1f, a);
                }),
                Bracket = Sprite("bracket", 32, 32, Vector4.zero, (x, y, w, h) =>
                {
                    // Top-left "L" (4 px), brushed metal shading.
                    bool on = (x < 5 && y > 4) || (y > h - 6 && x < w - 4);
                    if (!on) return Clear;
                    float shade = 0.62f + 0.3f * ((x + (h - y)) % 7) / 7f;
                    return new Color(shade, shade * 0.98f, shade * 0.95f, 1f);
                }),
                Shine = Sprite("shine", 64, 64, Vector4.zero, (x, y, w, h) =>
                {
                    float d = ((x - w * 0.5f) + (y - h * 0.5f) * 0.6f) / (w * 0.18f);
                    return new Color(1f, 1f, 1f, 0.55f * Mathf.Exp(-d * d));
                }),
                Vignette = Sprite("vignette", 256, 256, Vector4.zero, (x, y, w, h) =>
                {
                    float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
                    float r = Mathf.Sqrt(u * u * 0.9f + v * v * 1.1f);
                    return new Color(0f, 0f, 0f, Mathf.Clamp01(Mathf.SmoothStep(0.7f, 1.6f, r)) * 0.7f);
                }),
                Dot = Sprite("soft_dot", 64, 64, Vector4.zero, (x, y, w, h) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w * 0.5f, h * 0.5f)) / (w * 0.5f);
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Exp(-d * d * 3.2f) - 0.04f));
                }),
                Ring = Sprite("ring", 128, 128, Vector4.zero, (x, y, w, h) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w * 0.5f, h * 0.5f));
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - Mathf.Abs(d - 58f) / 2.5f));
                }),
                Disc = Sprite("disc", 64, 64, Vector4.zero, (x, y, w, h) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(w * 0.5f, h * 0.5f));
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(30f - d));
                }),
                Panel = Sprite("panel", 32, 32, new Vector4(6, 6, 6, 6), (x, y, w, h) =>
                {
                    // Flat panel with a 1 px lighter edge (tinted at use).
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    return edge ? new Color(1f, 1f, 1f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);
                }),
                Hazard = Texture("hazard", 64, 64, true, (x, y, w, h) =>
                    ((x + y) / 16) % 2 == 0 ? Hex(0xE8B90E) : new Color(0.06f, 0.05f, 0.04f, 1f)),
                Scanlines = Texture("scanlines", 4, 4, true, (x, y, w, h) => y == 0 ? new Color(0f, 0f, 0f, 1f) : Clear),
                Grain = Texture("grain", 256, 256, true, Noise(61, (x, y, n) =>
                {
                    float g = n;
                    return new Color(g, g, g, Mathf.Abs(g - 0.5f) * 2f);
                })),
                Scratch = Texture("title_scratch", 512, 256, true, Noise(7, (x, y, n) =>
                {
                    // Mostly clean face; speckles and a few long diagonal scratches darken it (distressed print).
                    float speck = n > 0.95f ? 0.55f : n > 0.9f ? 0.82f : 1f;
                    float scratch = Mathf.Abs(Mathf.Sin((x * 0.9f + y * 2.3f) * 0.05f)) < 0.012f && n > 0.3f ? 0.55f : 1f;
                    float grit = 0.96f + 0.04f * n;
                    float v = speck * scratch * grit;
                    return new Color(v, v, v, 1f);
                }))
            };
            AssetDatabase.SaveAssets();
            return set;
        }

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        public static Color Hex(int rgb, float a = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        private static Func<int, int, int, int, Color> Noise(int seed, Func<int, int, float, Color> shade)
        {
            var random = new System.Random(seed);
            return (x, y, w, h) => shade(x, y, (float)random.NextDouble());
        }

        private static Sprite Background(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.maxTextureSize < 2048
                || importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite Sprite(string name, int w, int h, Vector4 border, Func<int, int, int, int, Color> pixel)
        {
            string path = Write(name, w, h, pixel);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = border;
                    importer.spritePixelsPerUnit = 100f;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D Texture(string name, int w, int h, bool repeat, Func<int, int, int, int, Color> pixel)
        {
            string path = Write(name, w, h, pixel);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                TextureWrapMode wrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (importer.textureType != TextureImporterType.Default || importer.wrapMode != wrap || importer.mipmapEnabled)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = wrap;
                    importer.filterMode = name == "scanlines" ? FilterMode.Point : FilterMode.Bilinear;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Writes the PNG only when its pixels changed (stable git diffs), then imports it.</summary>
        private static string Write(string name, int w, int h, Func<int, int, int, int, Color> pixel)
        {
            string path = $"{OutFolder}/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) px[y * w + x] = pixel(x, y, w, h);
            }

            tex.SetPixels(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            return path;
        }
    }
}
