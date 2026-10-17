using System.Collections.Generic;
using System.Text.RegularExpressions;
using SixtyOne.World;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Isometric tile art: import settings for the tile sheets and the two RoomTileSets.
    /// Tiles are looked up by name (case-insensitive, ignoring spaces/underscores/dashes) in Art/Tiles; a
    /// missing name falls back to the same sprite index in the Concept/ sheets (sliced as "&lt;sheet&gt;_&lt;n&gt;").
    /// SixtyOne > Art > Apply Tile Import Settings / Generate Tile Sets. Both are safe to re-run.
    /// </summary>
    public static class TileArt
    {
        public const string TilesDir = "Assets/_Game/Art/Tiles";
        public const string ConceptDir = "Assets/_Game/Art/Concept";
        public const string DataDir = "Assets/_Game/Data/Art";
        public const string HomeTileSetPath = DataDir + "/HomeTileSet.asset";
        public const string CorridorTileSetPath = DataDir + "/CorridorTileSet.asset";
        // 254, not 256: the sheets carry a 1 px anti-aliased outline on every diamond edge; 1 px of overlap hides it.
        public const float PixelsPerUnit = 254f;

        // Fallback sheets in Concept/ (keywords that must all appear in the requested name -> sheet file).
        private static readonly (string[] Keywords, string Sheet)[] Fallbacks =
        {
            (new[] { "wood", "floor" }, "Floor_Wood_01-256x128"),
            (new[] { "floor", "stone" }, "Floor_Stone_02-256x128-Photoroom"),
            (new[] { "flat", "stone", "window" }, "Flat_Stone_01_WindowA-SW-128x192"),
            (new[] { "flat", "stone" }, "Flat_Stone_01-SW-128x192")
        };

        /// <summary>Names that were not found in Art/Tiles during the last generation (with their fallback).</summary>
        public static readonly List<string> MissingReport = new List<string>();

        [MenuItem("SixtyOne/Art/Apply Tile Import Settings", priority = 40)]
        public static void ApplyImportSettingsMenu()
        {
            int count = ApplyImportSettings();
            Debug.Log($"[TileArt] Import settings applied to {count} tile texture(s).");
        }

        [MenuItem("SixtyOne/Art/Generate Tile Sets", priority = 41)]
        public static void GenerateMenu()
        {
            TileSets sets = Generate();
            Debug.Log($"[TileArt] Tile sets: Home={(sets.Home != null)}, Corridor={(sets.Corridor != null)}. Missing: " +
                      (MissingReport.Count == 0 ? "none" : string.Join("; ", MissingReport)));
        }

        /// <summary>Both room tile sets.</summary>
        public sealed class TileSets
        {
            public RoomTileSet Home;
            public RoomTileSet Corridor;
        }

        /// <summary>Applies import settings, then creates/updates HomeTileSet and CorridorTileSet.</summary>
        public static TileSets Generate()
        {
            ApplyImportSettings();
            MissingReport.Clear();
            AssetUtil.EnsureFolder(DataDir);

            var sets = new TileSets
            {
                Home = BuildSet(HomeTileSetPath, "HomeFloorTile", "Wood Floor 13", new[] { "Flat Stone 0" }, new[] { "Flat Stone 1" },
                    "Flat Stone Window 0", "Flat Stone Window 1", Color.white, Color.white),
                Corridor = BuildSet(CorridorTileSetPath, "CorridorFloorTile", "Floor Stone 0", new[] { "Flat Stone 12" }, new[] { "Flat Stone 13" },
                    "Flat Stone Window 12", "Flat Stone Window 13", new Color(0.92f, 0.92f, 0.94f, 1f), new Color(0.86f, 0.86f, 0.9f, 1f))
            };
            AssetDatabase.SaveAssets();
            return sets;
        }

        private static RoomTileSet BuildSet(string path, string tileName, string floorName, string[] left, string[] right,
            string leftWindow, string rightWindow, Color floorTint, Color wallTint)
        {
            Sprite floor = FindSprite(floorName);
            Tile tile = AssetUtil.LoadOrCreate<Tile>($"{DataDir}/{tileName}.asset");
            tile.sprite = floor;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);

            RoomTileSet set = AssetUtil.LoadOrCreate<RoomTileSet>(path);
            set.Configure(floor, tile, FindAll(left), FindAll(right), FindSprite(leftWindow), FindSprite(rightWindow), floorTint, wallTint);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static Sprite[] FindAll(string[] names)
        {
            var list = new List<Sprite>();
            foreach (string n in names)
            {
                Sprite s = FindSprite(n);
                if (s != null) list.Add(s);
            }

            return list.ToArray();
        }

        /// <summary>Sprite by artist name in Art/Tiles, else the same index in the Concept fallback sheet.</summary>
        public static Sprite FindSprite(string requested)
        {
            string key = Normalize(requested);
            if (AssetDatabase.IsValidFolder(TilesDir))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TilesDir }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        if (o is Sprite s && (Normalize(s.name) == key || Normalize(System.IO.Path.GetFileNameWithoutExtension(path)) == key)) return s;
                    }
                }
            }

            string lower = requested.ToLowerInvariant();
            Match number = Regex.Match(requested, @"(\d+)\s*$");
            int index = number.Success ? int.Parse(number.Groups[1].Value) : 0;
            foreach ((string[] keywords, string sheet) in Fallbacks)
            {
                bool all = true;
                foreach (string k in keywords) all &= lower.Contains(k);
                if (!all) continue;

                string sheetPath = $"{ConceptDir}/{sheet}.png";
                Sprite found = SheetSprite(sheetPath, index) ?? SheetSprite(sheetPath, 0);
                MissingReport.Add($"'{requested}' not in Art/Tiles -> {(found != null ? found.name : "NOTHING")}");
                return found;
            }

            MissingReport.Add($"'{requested}' not found and no fallback");
            return null;
        }

        private static Sprite SheetSprite(string sheetPath, int index)
        {
            string wanted = System.IO.Path.GetFileNameWithoutExtension(sheetPath) + "_" + index;
            foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath))
            {
                if (o is Sprite s && s.name == wanted) return s;
            }

            return null;
        }

        private static string Normalize(string s)
        {
            return Regex.Replace(s ?? string.Empty, @"[\s_\-]", string.Empty).ToLowerInvariant();
        }

        /// <summary>PPU 254 (see PixelsPerUnit), bilinear, uncompressed, no mips; floors pivot centre, walls bottom-centre. Slices unsliced sheets.</summary>
        public static int ApplyImportSettings()
        {
            var paths = new List<string>();
            if (AssetDatabase.IsValidFolder(TilesDir))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TilesDir })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            foreach ((string[] _, string sheet) in Fallbacks)
            {
                string p = $"{ConceptDir}/{sheet}.png";
                if (AssetDatabase.LoadMainAssetAtPath(p) != null && !paths.Contains(p)) paths.Add(p);
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            foreach (string path in paths) ApplyTo(path, factory);
            return paths.Count;
        }

        private static void ApplyTo(string path, SpriteDataProviderFactories factory)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.GetSourceTextureWidthAndHeight(out int texW, out int texH);
            Vector2Int cell = CellSize(path, texW, texH);
            bool floor = cell.x == cell.y * 2;
            bool sheet = cell.x < texW || cell.y < texH;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = sheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)(floor ? SpriteAlignment.Center : SpriteAlignment.BottomCenter);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            if (!sheet) return;
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = new List<SpriteRect>(provider.GetSpriteRects());
            if (rects.Count == 0) rects = GridRects(path, texW, texH, cell);
            foreach (SpriteRect r in rects)
            {
                r.alignment = floor ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
                r.pivot = floor ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
            }

            provider.SetSpriteRects(rects.ToArray());
            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                var pairs = new List<SpriteNameFileIdPair>();
                foreach (SpriteRect r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
                names.SetNameFileIdPairs(pairs);
            }

            provider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>Cell size from the "WxH" in the file name (e.g. -256x128), else the whole texture.</summary>
        private static Vector2Int CellSize(string path, int texW, int texH)
        {
            Match m = Regex.Match(System.IO.Path.GetFileNameWithoutExtension(path), @"(\d+)x(\d+)");
            return m.Success ? new Vector2Int(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : new Vector2Int(texW, texH);
        }

        /// <summary>Grid slices named "&lt;file&gt;_&lt;n&gt;", left-to-right, top-to-bottom (Unity's default order).</summary>
        private static List<SpriteRect> GridRects(string path, int texW, int texH, Vector2Int cell)
        {
            var rects = new List<SpriteRect>();
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            int cols = texW / cell.x;
            int rows = texH / cell.y;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    rects.Add(new SpriteRect
                    {
                        name = baseName + "_" + rects.Count,
                        rect = new Rect(col * cell.x, texH - (row + 1) * cell.y, cell.x, cell.y),
                        spriteID = GUID.Generate()
                    });
                }
            }

            return rects;
        }
    }
}
