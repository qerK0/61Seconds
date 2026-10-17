using System.IO;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Door and elevator art (Art/Concept/door.png, asansorkapı.png): single sprites, pivot at the bottom-centre of
    /// the opaque door (measured from the source PNG), PPU chosen so the visible door is ~1.15x the player's height.
    /// The art faces the back-left wall; callers flip X for back-right walls.
    /// </summary>
    public static class DoorArt
    {
        public const string DoorPath = "Assets/_Game/Art/Concept/door.png";
        public const string ElevatorPath = "Assets/_Game/Art/Concept/asansorkapı.png";

        /// <summary>Visible door height relative to the player.</summary>
        public const float HeightRatio = 1.15f;

        public static float Height => CharacterArt.PlayerHeight * HeightRatio;

        private static Sprite door;
        private static Sprite elevator;

        public static Sprite Door => door != null ? door : door = Import(DoorPath);
        public static Sprite Elevator => elevator != null ? elevator : elevator = Import(ElevatorPath);

        /// <summary>Puts the door or elevator art on a renderer (white, unscaled parent assumed).</summary>
        public static bool Apply(SpriteRenderer renderer, Sprite sprite, bool rightWall)
        {
            if (renderer == null || sprite == null) return false;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.flipX = rightWall;
            renderer.transform.localScale = Vector3.one;
            return true;
        }

        private static Sprite Import(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || !File.Exists(path)) return null;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            int w = texture.width;
            int h = texture.height;
            Color32[] px = texture.GetPixels32();
            Object.DestroyImmediate(texture);

            int minX = w, maxX = 0, minY = h, maxY = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (px[y * w + x].a <= 40) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxY <= minY) return null;

            // Base at the centre column: the slanted bottom edge's midpoint sits on the wall line.
            int cx = (minX + maxX) / 2;
            int baseY = minY;
            for (int y = 0; y < h; y++)
            {
                if (px[y * w + cx].a > 40)
                {
                    baseY = y;
                    break;
                }
            }

            float ppu = (maxY - minY) / Height;
            var pivot = new Vector2(cx / (float)w, baseY / (float)h);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
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
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
