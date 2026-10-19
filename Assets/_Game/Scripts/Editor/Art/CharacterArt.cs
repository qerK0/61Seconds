using System.Collections.Generic;
using System.IO;
using SixtyOne.Core;
using SixtyOne.Placement;
using UnityEditor;
using UnityEditor.U2D.Sprites;
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

        /// <summary>Root of the NPC idle folders (&lt;ID&gt;_IDLE/frame0000.png ...).</summary>
        public const string CharRoot = "Assets/_Game/Art/char";

        /// <summary>NPC idle sets: id (HotspotSpec.Idle), folder and height relative to the player.</summary>
        public static readonly (string Id, string Folder, float HeightRatio)[] NpcIdles =
        {
            ("sevim", CharRoot + "/SEVIM_IDLE", 0.9f),   // an older neighbour, a little shorter than the wife
            ("cemil", CharRoot + "/CEMIL_IDLE", 1f),
            ("robber", CharRoot + "/ROBBER_IDLE", 1f),
            ("dog", CharRoot + "/DOG_IDLE", 0.55f)       // a big rabid dog, not a puppy
        };

        /// <summary>Idle loop speed for every NPC (frames per second).</summary>
        public const float NpcIdleFps = 9f;

        /// <summary>
        /// Idle loop speed for one NPC's frame set. The robber's art comes as a short 8-frame loop (~8 fps) or a smooth
        /// 101-frame export (~24 fps): the speed follows the frame count, never a fixed count.
        /// </summary>
        public static float NpcIdleFpsFor(string id, int frameCount)
        {
            if (id == "robber") return frameCount >= 24 ? 24f : 8f;
            return NpcIdleFps;
        }

        /// <summary>True if an NPC's idle art faces right (checked visually: Cemil and the robber look left).</summary>
        public static bool NpcFacesRight(string id) => id != "cemil" && id != "robber";

        /// <summary>Max texture size of NPC idle frames (1080x1920 canvases; the trimmed sprite keeps its detail).</summary>
        private const int NpcMaxTextureSize = 1024;

        /// <summary>Height ratio of an NPC idle id (1 when unknown).</summary>
        public static float NpcHeightRatio(string id)
        {
            foreach (var npc in NpcIdles)
            {
                if (npc.Id == id) return npc.HeightRatio;
            }

            return 1f;
        }

        /// <summary>Shared pixels-per-unit (source pixels) for all three characters.</summary>
        public const float PixelsPerUnit = 1690f;

        /// <summary>Visible heights the game aims for, in world units (a floor tile is 1 wide, 0.5 deep).</summary>
        public const float PlayerHeight = 0.8f;

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
                Frames crouch = ImportCrouch(member);
                Debug.Log($"[CharacterArt] {member} crouch: {crouch.Sprites.Length} frames, frame 0 height {crouch.VisibleHeight:0.00}u, body scale {crouch.BodyScale:0.000}.");
            }

            foreach (var npc in NpcIdles)
            {
                Frames idle = ImportIdle(npc.Id);
                Debug.Log(idle.Sprites.Length > 0
                    ? $"[CharacterArt] {npc.Id} idle: {idle.Sprites.Length} frames, visible height {idle.VisibleHeight:0.00}u, body scale {idle.BodyScale:0.000}."
                    : $"[CharacterArt] {npc.Id} idle: no frames in {npc.Folder} (neutral silhouette).");
            }
        }

        /// <summary>
        /// Imports an NPC's idle frames (any count, auto-detected when the folder exists) with the family's PPU and
        /// foot pivot. No folder or no frames = an empty set (the scene builder uses the neutral silhouette).
        /// </summary>
        public static Frames ImportIdle(string id)
        {
            foreach (var npc in NpcIdles)
            {
                if (npc.Id == id) return ImportTrimmed(SortedFramePaths(npc.Folder, "frame"), npc.HeightRatio);
            }

            return new Frames();
        }

        /// <summary>
        /// NPC idle import: every frame becomes one sprite trimmed to the opaque bounds shared by ALL frames (so the
        /// loop never jitters), pivot bottom-centre = the lowest opaque row (the feet). Shared PPU with the family.
        /// </summary>
        private static Frames ImportTrimmed(List<string> paths, float heightRatio)
        {
            var result = new Frames { Member = MemberType.Player };
            if (paths.Count == 0) return result;

            RectInt union = new RectInt(0, 0, 0, 0);
            bool any = false;
            foreach (string path in paths)
            {
                RectInt r = OpaqueBounds(path);
                if (r.width <= 0) continue;
                if (!any) union = r;
                else
                {
                    int xMin = Mathf.Min(union.xMin, r.xMin), yMin = Mathf.Min(union.yMin, r.yMin);
                    int xMax = Mathf.Max(union.xMax, r.xMax), yMax = Mathf.Max(union.yMax, r.yMax);
                    union = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
                }

                any = true;
            }

            if (!any) return result;

            var sprites = new List<Sprite>();
            foreach (string path in paths)
            {
                Sprite sprite = ApplyTrimmedImport(path, union);
                if (sprite != null) sprites.Add(sprite);
            }

            result.Sprites = sprites.ToArray();
            result.VisibleHeight = union.height / PixelsPerUnit;
            float target = PlayerHeight * heightRatio;
            result.BodyScale = result.VisibleHeight > 0f ? target / result.VisibleHeight : 1f;
            return result;
        }

        /// <summary>Opaque pixel bounds (alpha &gt; 20, bottom-left origin) of a source PNG; empty when fully clear.</summary>
        private static RectInt OpaqueBounds(string path)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            int w = texture.width, h = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (pixels[row + x].a <= 20) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX < minX ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static Sprite ApplyTrimmedImport(string path, RectInt rect)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return null;
            string spriteName = Path.GetFileNameWithoutExtension(path) + "_idle";
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.maxTextureSize = NpcMaxTextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = new List<SpriteRect>(provider.GetSpriteRects());
            SpriteRect sprite = existing.Find(r => r.name == spriteName) ?? new SpriteRect { name = spriteName, spriteID = GUID.Generate() };
            sprite.rect = new Rect(rect.x, rect.y, rect.width, rect.height);
            sprite.alignment = SpriteAlignment.BottomCenter;
            sprite.pivot = new Vector2(0.5f, 0f);
            provider.SetSpriteRects(new[] { sprite });
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null) names.SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(sprite.name, sprite.spriteID) });
            provider.Apply();
            importer.SaveAndReimport();

            foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (o is Sprite s && s.name == spriteName) return s;
            }

            return null;
        }

        /// <summary>Drop-Cover-Hold frames per character (frame 0 standing → last = held crouch).</summary>
        public static string CrouchFolder(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return CharRoot + "/CROUCH_ANNE";
                case MemberType.Child: return CharRoot + "/CROUCH_CHILD";
                default: return CharRoot + "/CROUCH_MEHMET";
            }
        }

        /// <summary>
        /// Imports a character's crouch frames (any count) with the run frames' settings, ONE shared pivot (the
        /// lowest opaque row over all frames, frame 0's horizontal foot centre) and ONE scale so frame 0 is as tall
        /// as the character in game. No per-frame trim or pivot (the sequence would jitter). Empty when missing.
        /// </summary>
        public static Frames ImportCrouch(MemberType member)
        {
            var result = new Frames { Member = member };
            List<string> paths = SortedFramePaths(CrouchFolder(member), "frame");
            if (paths.Count == 0) return result;

            int minY = int.MaxValue;
            int width = 1, height = 1;
            foreach (string path in paths)
            {
                Measure(path, out int w, out int h, out int bottom, out int _);
                width = w;
                height = h;
                if (bottom < minY) minY = bottom;
            }

            Measure(paths[0], out int _, out int _, out int bottom0, out int top0);
            float footX = FootCentreX(paths[0], bottom0, top0);
            var pivot = new Vector2(Mathf.Clamp01(footX / width), Mathf.Clamp01(minY / (float)height));
            var sprites = new List<Sprite>();
            foreach (string path in paths)
            {
                ApplyImport(path, pivot);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }

            result.Sprites = sprites.ToArray();
            result.VisibleHeight = (top0 - bottom0 + 1) / PixelsPerUnit;
            float target = PlayerHeight * TargetHeightRatio(member);
            result.BodyScale = result.VisibleHeight > 0f ? target / result.VisibleHeight : 1f;
            return result;
        }

        /// <summary>Horizontal centre (px) of the opaque pixels in the lowest 5% of a frame's figure: the feet.</summary>
        private static float FootCentreX(string path, int bottom, int top)
        {
            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            int w = texture.width;
            Color32[] pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            int band = Mathf.Max(2, Mathf.RoundToInt((top - bottom) * 0.05f));
            int minX = w, maxX = -1;
            for (int y = bottom; y <= bottom + band && y * w < pixels.Length; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 20) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                }
            }

            return maxX < minX ? w * 0.5f : (minX + maxX) * 0.5f;
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
