using System.Collections.Generic;
using System.IO;
using SixtyOne.Core;
using SixtyOne.Placement;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Art > Import Character Frames (re-runnable). Run frames in Assets/_Game/Art/char/RUN:
    /// annerun (n) = wife, cocukrun (n) = child, mehmetrun (n) = player, sorted NUMERICALLY by n. Import: Sprite,
    /// Max Size 512, compressed, no mipmaps, one shared PPU, pivot bottom-centre at the feet (the lowest opaque
    /// row over all of a character's frames, so the run never floats or sinks). Source files are never resized.
    /// </summary>
    public static class CharacterArt
    {
        public const string Folder = "Assets/_Game/Art/char/RUN";

        /// <summary>Sevim Teyze's idle loop (frame0000.png ...), imported the same way as the family.</summary>
        public const string SevimFolder = "Assets/_Game/Art/char/SEVIM_IDLE";

        /// <summary>Sevim's height relative to the player (an older neighbour, a little shorter than the wife).</summary>
        public const float SevimHeightRatio = 0.9f;

        /// <summary>Shared pixels-per-unit (source pixels) for all three characters.</summary>
        public const float PixelsPerUnit = 1690f;

        /// <summary>Visible heights the game aims for, in world units (a floor tile is 1 wide, 0.5 deep).</summary>
        public const float PlayerHeight = 0.9f;

        /// <summary>Per-character art prefix.</summary>
        public static string Prefix(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return "annerun";
                case MemberType.Child: return "cocukrun";
                default: return "mehmetrun";
            }
        }

        /// <summary>
        /// Target height relative to the player (wife slightly shorter, child ~0.75). The source art is drawn at
        /// different scales (wife ~3% taller than the player, child ~0.59), so the body transform corrects it
        /// while the PPU stays shared.
        /// </summary>
        public static float TargetHeightRatio(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return 0.94f;
                case MemberType.Child: return 0.75f;
                default: return 1f;
            }
        }

        /// <summary>Most neutral-looking frame to hold while idle (0-based, checked visually).</summary>
        public static int IdleFrame(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return 9;   // annerun (10): dress hanging straight
                case MemberType.Child: return 2;  // cocukrun (3): feet together
                default: return 2;                // mehmetrun (3): feet together
            }
        }

        /// <summary>True if the run art faces right (the animator flips it when running left).</summary>
        public static bool FacesRight(MemberType member) => true;

        /// <summary>Imported frames of one character plus measurements.</summary>
        public sealed class Frames
        {
            public MemberType Member;
            public Sprite[] Sprites = new Sprite[0];
            /// <summary>Visible height in world units at the shared PPU (feet to top of head, max over frames).</summary>
            public float VisibleHeight;
            /// <summary>Uniform scale for the body so the character reaches its target height.</summary>
            public float BodyScale = 1f;
        }

        [MenuItem("SixtyOne/Art/Import Character Frames", priority = 30)]
        public static void ImportMenu()
        {
            foreach (MemberType member in new[] { MemberType.Player, MemberType.Wife, MemberType.Child })
            {
                Frames frames = Import(member);
                Debug.Log($"[CharacterArt] {Prefix(member)}: {frames.Sprites.Length} frames, visible height {frames.VisibleHeight:0.00}u, body scale {frames.BodyScale:0.000}.");
            }

            Frames sevim = ImportSevim();
            Debug.Log($"[CharacterArt] sevim idle: {sevim.Sprites.Length} frames, visible height {sevim.VisibleHeight:0.00}u, body scale {sevim.BodyScale:0.000}.");
        }

        /// <summary>Imports Sevim Teyze's idle frames (any count) with the family's PPU and foot pivot.</summary>
        public static Frames ImportSevim()
        {
            return ImportPaths(SortedFramePaths(SevimFolder, "frame"), SevimHeightRatio, MemberType.Player);
        }

        /// <summary>Imports (or re-imports) one character's frames and returns them in numeric order.</summary>
        public static Frames Import(MemberType member)
        {
            return ImportPaths(SortedFramePaths(Prefix(member)), TargetHeightRatio(member), member);
        }

        private static Frames ImportPaths(List<string> paths, float heightRatio, MemberType member)
        {
            var result = new Frames { Member = member };
            if (paths.Count == 0) return result;

            // Measure from the source PNGs (full resolution, never modified).
            int minY = int.MaxValue;
            int maxY = 0;
            int height = 0;
            foreach (string path in paths)
            {
                Measure(path, out int w, out int h, out int bottom, out int top);
                height = h;
                if (bottom < minY) minY = bottom;
                if (top > maxY) maxY = top;
            }

            float pivotY = height > 0 ? Mathf.Clamp01(minY / (float)height) : 0f;
            var sprites = new List<Sprite>();
            foreach (string path in paths)
            {
                ApplyImport(path, new Vector2(0.5f, pivotY));
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }

            result.Sprites = sprites.ToArray();
            result.VisibleHeight = (maxY - minY) / PixelsPerUnit;
            float target = PlayerHeight * heightRatio;
            result.BodyScale = result.VisibleHeight > 0f ? target / result.VisibleHeight : 1f;
            return result;
        }

        /// <summary>Frame paths for a prefix, sorted by the number in parentheses ("x (2)" before "x (10)").</summary>
        public static List<string> SortedFramePaths(string prefix) => SortedFramePaths(Folder, prefix);

        /// <summary>Frame paths in a folder for a prefix, numeric order ("x (2)" before "x (10)", zero-padded names in order).</summary>
        public static List<string> SortedFramePaths(string folder, string prefix)
        {
            var paths = new List<string>();
            if (!Directory.Exists(folder)) return paths;
            foreach (string file in Directory.GetFiles(folder, prefix + "*.png"))
            {
                paths.Add(file.Replace('\\', '/'));
            }

            FrameOrder.Sort(paths);
            return paths;
        }

        /// <summary>The number in parentheses of a frame file name, or 0.</summary>
        public static int FrameNumber(string path) => FrameOrder.NumberOf(path);

        /// <summary>Opaque rows (from the bottom) of a source PNG.</summary>
        private static void Measure(string path, out int width, out int height, out int bottom, out int top)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            width = texture.width;
            height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            bottom = height;
            top = 0;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a <= 20) continue;
                    if (y < bottom) bottom = y;
                    if (y > top) top = y;
                    break;
                }
            }

            Object.DestroyImmediate(texture);
        }

        private static void ApplyImport(string path, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
