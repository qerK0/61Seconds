using System.IO;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Door and elevator art (Art/Concept/kapi.png, else door.png; asansorkapı.png) fitted to a back-wall gap. A uniform scale cannot
    /// fill the gap width, reach the wanted height and keep the wall slant at once, so a wall-fitted copy is generated
    /// (Art/Concept/Generated/*_wall.png): each source column is remapped between the door's fitted bottom and top
    /// lines so that, at one shared PPU, the bottom edge lies exactly on the gap's floor edge (2:1 slant), the width
    /// fills one wall segment and the height is HeightOfWall x the stacked wall. Pivot = bottom-edge midpoint
    /// (the gap's edge centre). The art faces the back-left wall; callers flip X for back-right walls.
    /// </summary>
    public static class DoorArt
    {
        /// <summary>The door art: kapi.png (new), else the older door.png.</summary>
        public const string NewDoorPath = "Assets/_Game/Art/Concept/kapi.png";
        public const string OldDoorPath = "Assets/_Game/Art/Concept/door.png";
        public static string DoorPath => File.Exists(NewDoorPath) ? NewDoorPath : OldDoorPath;
        public const string ElevatorPath = "Assets/_Game/Art/Concept/asansorkapı.png";
        private const string OutFolder = "Assets/_Game/Art/Concept/Generated";

        /// <summary>Door top as a fraction of the stacked wall height (per pixel column).</summary>
        public const float HeightOfWall = 0.9f;

        /// <summary>World width of one wall segment (one tile edge), which the door fills.</summary>
        public const float Width = 0.5f;

        /// <summary>Rise of the floor edge across one segment (2:1 isometric).</summary>
        public const float Rise = Width * 0.5f;

        private static int wallRows = 2;
        private static Sprite door;
        private static Sprite elevator;

        /// <summary>Visible door height (vertical, per column) for the configured wall rows.</summary>
        public static float Height => HeightOfWall * RoomTiles.WallColumnHeight(wallRows);

        /// <summary>Collider box for a door/elevator whose pivot is its bottom-edge midpoint.</summary>
        public static Vector2 ColliderSize => new Vector2(Width, Height + Rise);

        public static Vector2 ColliderOffset => new Vector2(0f, (Height + Rise) * 0.5f);

        public static Sprite Door => door != null ? door : door = Build(DoorPath, "door_wall");
        public static Sprite Elevator => elevator != null ? elevator : elevator = Build(ElevatorPath, "elevator_wall");

        /// <summary>Sets the wall rows the art is fitted to (the scene builder calls it before building rooms).</summary>
        public static void Configure(int rows)
        {
            rows = Mathf.Max(1, rows);
            if (rows == wallRows && door != null) return;
            wallRows = rows;
            door = null;
            elevator = null;
        }

        /// <summary>Puts the door or elevator art on a renderer (white, unscaled; pivot at the renderer's position).</summary>
        public static bool Apply(SpriteRenderer renderer, Sprite sprite, bool rightWall)
        {
            if (renderer == null || sprite == null) return false;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.flipX = rightWall;
            renderer.transform.localScale = Vector3.one;
            return true;
        }

        private static Sprite Build(string sourcePath, string outName)
        {
            if (!File.Exists(sourcePath)) return null;
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(sourcePath));
            int w = source.width;
            int h = source.height;
            Color32[] px = source.GetPixels32();

            // Per column: lowest and highest opaque row (bottom-origin).
            int x0 = w, x1 = -1;
            var bottom = new int[w];
            var top = new int[w];
            for (int x = 0; x < w; x++)
            {
                bottom[x] = -1;
                top[x] = -1;
                for (int y = 0; y < h; y++) if (px[y * w + x].a > 40) { bottom[x] = y; break; }
                if (bottom[x] < 0) continue;
                for (int y = h - 1; y >= 0; y--) if (px[y * w + x].a > 40) { top[x] = y; break; }
                x0 = Mathf.Min(x0, x);
                x1 = Mathf.Max(x1, x);
            }

            if (x1 <= x0)
            {
                Object.DestroyImmediate(source);
                return null;
            }

            // Straight bottom / top lines fitted over the door face (skip the thin side edge and the far rim).
            int span = x1 - x0;
            FitLine(bottom, x0 + span * 15 / 100, x1 - span * 10 / 100, out float bA, out float bB);
            FitLine(top, x0 + span * 15 / 100, x1 - span * 10 / 100, out float tA, out float tB);

            int outW = span + 1;
            float ppu = outW / Width;
            float doorPx = Height * ppu;
            float risePx = Rise * ppu;
            int margin = Mathf.CeilToInt(doorPx * 0.15f);
            int outH = Mathf.CeilToInt(doorPx + risePx) + 2 * margin;
            var output = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            var clear = new Color(0f, 0f, 0f, 0f);
            for (int xo = 0; xo < outW; xo++)
            {
                float xs = x0 + xo;
                float srcBottom = bA + bB * xs;
                float srcTop = tA + tB * xs;
                float outBottom = margin + risePx * xo / outW;
                for (int yo = 0; yo < outH; yo++)
                {
                    float rel = (yo - outBottom) / doorPx;
                    float ys = srcBottom + rel * (srcTop - srcBottom);
                    Color c = ys < 0f || ys >= h ? clear : source.GetPixelBilinear((xs + 0.5f) / w, (ys + 0.5f) / h);
                    output.SetPixel(xo, yo, c);
                }
            }

            Object.DestroyImmediate(source);
            if (!AssetDatabase.IsValidFolder(OutFolder)) AssetDatabase.CreateFolder("Assets/_Game/Art/Concept", "Generated");
            string outPath = $"{OutFolder}/{outName}.png";
            File.WriteAllBytes(outPath, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(outPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(outPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            // Bottom-edge midpoint: centre column, on the remapped bottom line.
            settings.spritePivot = new Vector2(0.5f, (margin + risePx * 0.5f) / outH);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(outPath);
        }

        /// <summary>Least-squares line y = a + b*x over the columns in [from, to] that have a value.</summary>
        private static void FitLine(int[] values, int from, int to, out float a, out float b)
        {
            double n = 0, sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (int x = from; x <= to; x++)
            {
                if (values[x] < 0) continue;
                n++;
                sx += x;
                sy += values[x];
                sxx += (double)x * x;
                sxy += (double)x * values[x];
            }

            double d = n * sxx - sx * sx;
            b = d != 0 ? (float)((n * sxy - sx * sy) / d) : 0f;
            a = n > 0 ? (float)((sy - b * sx) / n) : 0f;
        }
    }
}
