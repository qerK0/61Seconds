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
        public const string DamagedCorridorTileSetPath = DataDir + "/DamagedCorridorTileSet.asset";
        public const string StreetTileSetPath = DataDir + "/StreetTileSet.asset";

        /// <summary>
        /// Corner-pair wall art in Concept/ (one PNG = left face + right face, 128 px each, 192 tall, like the sheet
        /// cells): sliced into "&lt;file&gt;_L" / "&lt;file&gt;_R". Stone walls with windows and cracked post-quake walls.
        /// </summary>
        public static readonly string[] CornerPairs =
        {
            "pencereler 1", "pencereler 2", "pencereler 22", "pencereler 222",
            "WhatsApp Image 2026-10-17 at 5.27.28 aa", "WhatsApp Image 2026-10-17 at 5.27.28 aan"
        };

        private const string HomeWindows = "pencereler 1";
        private const string CorridorWindows = "pencereler 222";
        private const string CrackedA = "WhatsApp Image 2026-10-17 at 5.27.28 aa";
        private const string CrackedB = "WhatsApp Image 2026-10-17 at 5.27.28 aan";
        // 254, not 256: the sheets carry a 1 px anti-aliased outline on every diamond edge; 1 px of overlap hides it.
        public const float PixelsPerUnit = 254f;

        // Fallback sheets in Concept/ (keywords that must all appear in the requested name -> sheet file).
        private static readonly (string[] Keywords, string Sheet)[] Fallbacks =
        {
            (new[] { "wood", "floor" }, "Floor_Wood_01-256x128"),
            (new[] { "floor", "rocky" }, "Floor_Rocky_01-256x128"),
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
            /// <summary>Post-quake corridors (F3/F2): the corridor stone with cracked segments mixed in.</summary>
            public RoomTileSet DamagedCorridor;
            /// <summary>The street: Rocky 17 with ~15% Rocky 3 patches, no walls.</summary>
            public RoomTileSet Street;
        }

        /// <summary>Applies import settings, then creates/updates HomeTileSet and CorridorTileSet.</summary>
        public static TileSets Generate()
        {
            ApplyImportSettings();
            MissingReport.Clear();
            AssetUtil.EnsureFolder(DataDir);

            foreach (string pair in CornerPairs) SliceCornerPair($"{ConceptDir}/{pair}.png");

            // parke.png (parquet) was compared with Wood Floor 13 and left out: one diamond with a burn stain and
            // notched edges, not 2:1 (332x152), so it repeats visibly and leaves seams.
            var sets = new TileSets
            {
                Home = BuildSet(HomeTileSetPath, "HomeFloorTile", "Wood Floor 13", new[] { "Flat Stone 0" }, new[] { "Flat Stone 1" },
                    HomeWindows + "_L", HomeWindows + "_R", Color.white, Color.white),
                Corridor = BuildSet(CorridorTileSetPath, "CorridorFloorTile", "Floor Stone 0", new[] { "Flat Stone 12" }, new[] { "Flat Stone 13" },
                    CorridorWindows + "_L", CorridorWindows + "_R", new Color(0.92f, 0.92f, 0.94f, 1f), new Color(0.86f, 0.86f, 0.9f, 1f)),
                // Cracked segments at irregular positions (index % 7), the rest plain corridor stone.
                DamagedCorridor = BuildSet(DamagedCorridorTileSetPath, "CorridorFloorTile", "Floor Stone 0",
                    new[] { "Flat Stone 12", CrackedA + "_L", "Flat Stone 12", "Flat Stone 12", CrackedB + "_L", "Flat Stone 12", "Flat Stone 12" },
                    new[] { "Flat Stone 13", "Flat Stone 13", CrackedB + "_R", "Flat Stone 13", "Flat Stone 13", CrackedA + "_R", "Flat Stone 13" },
                    CorridorWindows + "_L", CorridorWindows + "_R", new Color(0.92f, 0.92f, 0.94f, 1f), new Color(0.86f, 0.86f, 0.9f, 1f))
            };
            sets.Street = BuildStreetSet();

            // Every set can crack after a quake (WallCracks swaps these in, seeded per segment).
            Sprite[] crackedLeft = FindAll(new[] { CrackedA + "_L", CrackedB + "_L" });
            Sprite[] crackedRight = FindAll(new[] { CrackedB + "_R", CrackedA + "_R" });
            foreach (RoomTileSet set in new[] { sets.Home, sets.Corridor, sets.DamagedCorridor })
            {
                if (set == null) continue;
                set.ConfigureCracks(crackedLeft, crackedRight);
                EditorUtility.SetDirty(set);
            }

            AssetDatabase.SaveAssets();
            return sets;
        }

        /// <summary>Street floor: "Floor Rocky 17" with "Floor Rocky 3" patches (the Rocky sheet sliced like the others), no walls.</summary>
        private static RoomTileSet BuildStreetSet()
        {
            Sprite floor = FindSprite("Floor Rocky 17");
            Sprite patch = FindSprite("Floor Rocky 3");
            Tile tile = AssetUtil.LoadOrCreate<Tile>($"{DataDir}/StreetFloorTile.asset");
            tile.sprite = floor;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            Tile patchTile = AssetUtil.LoadOrCreate<Tile>($"{DataDir}/StreetPatchTile.asset");
            patchTile.sprite = patch;
            patchTile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(patchTile);

            RoomTileSet set = AssetUtil.LoadOrCreate<RoomTileSet>(StreetTileSetPath);
            set.Configure(floor, tile, new Sprite[0], new Sprite[0], null, null, new Color(0.9f, 0.9f, 0.92f, 1f), Color.white);
            set.ConfigurePatch(patch != null ? patchTile : null, 0.15f);
            EditorUtility.SetDirty(set);
            return set;
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
            Sprite corner = CornerSprite(requested);
            if (corner != null) return corner;
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

        /// <summary>"&lt;corner pair&gt;_L" / "_R" from Concept/, or null.</summary>
        private static Sprite CornerSprite(string requested)
        {
            if (requested == null || !(requested.EndsWith("_L") || requested.EndsWith("_R"))) return null;
            string file = requested.Substring(0, requested.Length - 2);
            foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath($"{ConceptDir}/{file}.png"))
            {
                if (o is Sprite s && s.name == requested) return s;
            }

            return null;
        }

        /// <summary>
        /// Slices a corner-pair PNG into its left and right wall faces (128 px wide each, opaque rows measured),
        /// PPU 254 and pivot bottom-centre like the sheet walls, so they line up with the existing segments.
        /// </summary>
        private static void SliceCornerPair(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            var texture = new Texture2D(2, 2);
            texture.LoadImage(System.IO.File.ReadAllBytes(path));
            int w = texture.width;
            int h = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            int bottom = h;
            int top = -1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (pixels[y * w + x].a <= 20) continue;
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                    break;
                }
            }

            if (top < bottom) return;
            int face = Mathf.Min(128, w / 2);
            string baseName = System.IO.Path.GetFileNameWithoutExtension(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = new List<SpriteRect>(provider.GetSpriteRects());
            var rects = new List<SpriteRect>();
            for (int side = 0; side < 2; side++)
            {
                string name = baseName + (side == 0 ? "_L" : "_R");
                SpriteRect rect = existing.Find(r => r.name == name) ?? new SpriteRect { name = name, spriteID = GUID.Generate() };
                rect.rect = new Rect(side * face, bottom, face, top - bottom + 1);
                rect.alignment = SpriteAlignment.BottomCenter;
                rect.pivot = new Vector2(0.5f, 0f);
                rects.Add(rect);
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
