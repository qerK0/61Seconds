using System.Collections.Generic;
using System.IO;
using SixtyOne.World;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Art > Import Prop Art. Imports the furniture / fixture art in Art/Concept as sprites with a
    /// bottom-centre pivot on the opaque pixels, and a PPU that puts each prop at its target size in room-local
    /// world units (before RoomVisualConfig.WorldScale): table ~0.45 high, wardrobe / bookshelf ~1.1, stove ~0.5,
    /// chair ~0.55, valve / fuse box ~0.25, wall lamp ~0.2, debris ~0.35 wide; from Art/items: wardrobe (gardrop, the
    /// default) / wardrobe v2 ~1.1, chair (sandalye, the default) ~0.55, nightstand (komidin) ~0.32, kitchen cabinet ~0.6, toolbox ~0.25,
    /// armchair / sofa ~0.5, bed ~0.45, floor hole ~1.4 wide (centre-pivot floor decal). The chandelier frames keep their
    /// whole (shared) canvas so the swing and the fallen frame line up. Writes Data/Art/PropArtSet.asset
    /// (prop name → sprite). Safe to re-run; local tunes the targets by eye.
    /// </summary>
    public static class PropArt
    {
        public const string ConceptDir = "Assets/_Game/Art/Concept";
        public const string SetPath = "Assets/_Game/Data/Art/PropArtSet.asset";

        private enum Measure { Height, Width, Canvas }

        private sealed class Entry
        {
            public string File;
            public float Target;
            public Measure By;
            public string[] Names;
            /// <summary>Folder of the file (Art/Concept by default; the newer furniture lives in Art/items).</summary>
            public string Dir = ConceptDir;
        }

        public const string ItemsDir = "Assets/_Game/Art/items";

        private static Entry E(string file, float target, Measure by, params string[] names) => new Entry { File = file, Target = target, By = by, Names = names };

        private static Entry I(string file, float target, Measure by, params string[] names) => new Entry { File = file, Target = target, By = by, Names = names, Dir = ItemsDir };

        /// <summary>Palette label of a prop name (variants of the default art get a readable name).</summary>
        public static string Label(string name)
        {
            switch (name)
            {
                case "WardrobeOld": return "Wardrobe (eski)";
                case "WardrobeV2": return "Wardrobe v2";
                case "ChairOld": return "Chair (masaa)";
                default: return name;
            }
        }

        /// <summary>Floor decals: centre pivot, sorted on the floor layer (under characters).</summary>
        public static bool IsFloorDecal(string name) => name == "FloorHole";

        /// <summary>Target height (room-local units) of a prop name from the table, or 0 when it is not sized by height.</summary>
        public static float TargetHeight(string name)
        {
            foreach (Entry e in Entries)
            {
                if (e.By == Measure.Height && System.Array.IndexOf(e.Names, name) >= 0) return e.Target;
            }

            return 0f;
        }

        /// <summary>Target sizes (tune by eye locally). ComfyUI_00045_ / _00053_ are flat textures: never used in the world.</summary>
        private static readonly Entry[] Entries =
        {
            E("masA", 0.45f, Measure.Height, "Table", "Desk"),
            // New defaults from Art/items (gardrop, sandalye); the older art stays as palette variants.
            I("gardrop", 1.1f, Measure.Height, "Wardrobe"),
            I("sandalye", 0.55f, Measure.Height, "Chair"),
            I("komidin", 0.32f, Measure.Height, "Nightstand"),
            E("masaa", 0.55f, Measure.Height, "ChairOld"),
            E("dolap", 1.1f, Measure.Height, "WardrobeOld"),
            E("ComfyUI_00021_", 1.1f, Measure.Height, "NeighbourWardrobe", "Cabinet"),
            E("bokkshelf", 1.1f, Measure.Height, "Bookshelf"),
            E("ocak", 0.5f, Measure.Height, "Stove"),
            E("vana", 0.25f, Measure.Height, "Valve"),
            E("sigorta", 0.25f, Measure.Height, "FuseBox"),
            E("lamba", 0.2f, Measure.Height, "WallLamp"),
            I("dolapv2", 1.1f, Measure.Height, "WardrobeV2"),
            I("mutfak dolabi", 0.6f, Measure.Height, "KitchenCabinet"),
            I("alet canta", 0.25f, Measure.Height, "Toolbox"),
            I("koltuk", 0.5f, Measure.Height, "Armchair", "Sofa"),
            I("yatak", 0.45f, Measure.Height, "Bed"),
            I("zemin delik", 1.4f, Measure.Width, "FloorHole")
        };

        private static readonly string[] ChandelierFiles = { "frame0000", "frame0001", "frame0002", "frame0003", "frame0004", "frame0005" };

        /// <summary>Whole chandelier canvas height (ceiling to floor) in room-local units.</summary>
        public const float ChandelierCanvasHeight = 1.35f;

        private static readonly string[] DebrisFiles = { "enkaz 1 ", "enkaz 2" };

        /// <summary>Debris width in room-local units.</summary>
        public const float DebrisWidth = 0.35f;

        private static PropArtSet current;

        /// <summary>The set generated during this editor session (imports it on first use).</summary>
        public static PropArtSet Current => current != null ? current : current = Generate();

        [MenuItem("SixtyOne/Art/Import Prop Art", priority = 42)]
        public static void ImportMenu()
        {
            current = Generate();
            int mapped = 0;
            foreach (PropArtSet.Entry e in current.Entries) if (e.Sprite != null) mapped++;
            Debug.Log($"[PropArt] {mapped} props mapped, {current.ChandelierFrames.Length} chandelier frames, {current.Debris.Length} debris sprites -> {SetPath}. Next: Build Test Scene.");
        }

        /// <summary>Imports every prop sprite and (re)writes the PropArtSet.</summary>
        public static PropArtSet Generate()
        {
            AssetUtil.EnsureFolder("Assets/_Game/Data/Art");
            var mapped = new List<PropArtSet.Entry>();
            foreach (Entry e in Entries)
            {
                Sprite sprite = Import(e.File, e.Target, e.By, e.Dir, System.Array.Exists(e.Names, IsFloorDecal));
                if (sprite == null)
                {
                    Debug.LogWarning($"[PropArt] {e.File}.png missing; {string.Join("/", e.Names)} keep the coloured block.");
                    continue;
                }

                foreach (string n in e.Names) mapped.Add(new PropArtSet.Entry { Name = n, Sprite = sprite });
            }

            var chandelier = new List<Sprite>();
            foreach (string f in ChandelierFiles)
            {
                Sprite s = Import(f, ChandelierCanvasHeight, Measure.Canvas);
                if (s != null) chandelier.Add(s);
            }

            var debris = new List<Sprite>();
            foreach (string f in DebrisFiles)
            {
                Sprite s = Import(f, DebrisWidth, Measure.Width);
                if (s != null) debris.Add(s);
            }

            PropArtSet set = AssetUtil.LoadOrCreate<PropArtSet>(SetPath);
            set.Configure(mapped, chandelier.ToArray(), debris.ToArray());
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            current = set;
            return set;
        }

        /// <summary>
        /// One sprite per file: the opaque pixels (alpha &gt; 20) as the sprite rect (or the whole canvas), pivot
        /// bottom-centre, PPU = measured pixels / target units.
        /// </summary>
        private static Sprite Import(string file, float target, Measure by, string dir = ConceptDir, bool centre = false)
        {
            string path = $"{dir}/{file}.png";
            if (!File.Exists(path) || !(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(path));
            int w = texture.width;
            int h = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);

            RectInt rect = by == Measure.Canvas ? new RectInt(0, 0, w, h) : OpaqueBounds(pixels, w, h);
            if (rect.width <= 0 || rect.height <= 0) rect = new RectInt(0, 0, w, h);
            float measured = by == Measure.Width ? rect.width : rect.height;
            float ppu = Mathf.Max(1f, measured / Mathf.Max(0.01f, target));
            string spriteName = file.Trim() + "_prop";

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = new List<SpriteRect>(provider.GetSpriteRects());
            SpriteRect sprite = existing.Find(r => r.name == spriteName) ?? new SpriteRect { name = spriteName, spriteID = GUID.Generate() };
            sprite.rect = new Rect(rect.x, rect.y, rect.width, rect.height);
            sprite.alignment = centre ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
            sprite.pivot = centre ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
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

        /// <summary>Opaque pixel bounds (bottom-left origin) of an image.</summary>
        private static RectInt OpaqueBounds(Color32[] pixels, int w, int h)
        {
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 20) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            return maxX < minX ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
