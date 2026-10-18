using System.Collections.Generic;
using SixtyOne.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Text > Generate Fonts + Theme (re-runnable; Build Test Scene runs it too). Bakes TMP SDF assets
    /// for Inter Regular / SemiBold / Bold, EB Garamond, Oswald Bold / Medium and IBM Plex Mono Regular / Bold (OFL,
    /// Assets/_Game/Art/Fonts) with ASCII + full Turkish coverage (plus ’ and →), gives
    /// each a dynamic Inter fallback for anything else, makes Inter Regular the TMP default font and writes the
    /// UITheme asset (Assets/_Game/Data/UI/UITheme.asset).
    /// </summary>
    public static class FontAssetsGenerator
    {
        public const string FontFolder = "Assets/_Game/Art/Fonts";
        public const string ThemePath = "Assets/_Game/Data/UI/UITheme.asset";

        /// <summary>Baked into every static atlas: ASCII, Turkish (ç ğ ı İ ö ş ü Ç Ğ Ö Ş Ü, â î û) and punctuation.</summary>
        public static readonly string Characters = BuildCharacters();

        private const int SamplingSize = 90;
        private const int Padding = 9;
        private const int AtlasSize = 1024;

        // LED digits: few glyphs, so a big padding leaves room for the glow / underlay.
        private const string LedCharacters = "0123456789: ";
        private const int LedPadding = 20;
        public const string LedFontPath = FontFolder + "/TrueType (.ttf)/E1234-Regular.ttf";
        public const string SerifFolder = FontFolder + "/EBGaramond";
        public const string OswaldFolder = FontFolder + "/Oswald";
        public const string PlexFolder = FontFolder + "/IBMPlexMono";

        [MenuItem("SixtyOne/Text/Generate Fonts + Theme", priority = 40)]
        public static void GenerateMenu()
        {
            UITheme theme = Generate();
            Debug.Log("[Fonts] Inter SDF assets + UITheme ready: " + AssetDatabase.GetAssetPath(theme));
        }

        /// <summary>Creates or refreshes the font assets and the theme; returns the theme (fonts null if Inter is missing).</summary>
        public static UITheme Generate()
        {
            AssetUtil.EnsureFolder("Assets/_Game/Data/UI");
            UITheme theme = AssetUtil.LoadOrCreate<UITheme>(ThemePath);

            TMP_FontAsset dynamicFallback = Bake("Inter-Regular", "Inter-Regular Dynamic SDF", false, null);
            TMP_FontAsset regular = Bake("Inter-Regular", "Inter-Regular SDF", true, dynamicFallback);
            TMP_FontAsset semiBold = Bake("Inter-SemiBold", "Inter-SemiBold SDF", true, dynamicFallback);
            TMP_FontAsset bold = Bake("Inter-Bold", "Inter-Bold SDF", true, dynamicFallback);
            if (regular == null)
            {
                Debug.LogWarning("[Fonts] Inter TTFs not found in " + FontFolder + "; the theme keeps the TMP default font.");
            }

            theme.ConfigureFonts(regular, semiBold, bold);
            TMP_FontAsset led = Bake(LedFontPath, $"{FontFolder}/E1234 LED SDF.asset", LedCharacters, LedPadding, null);
            TMP_FontAsset serifRegular = Bake($"{SerifFolder}/EBGaramond-Regular.ttf", $"{SerifFolder}/EBGaramond-Regular SDF.asset", Characters, Padding, dynamicFallback);
            TMP_FontAsset serifSemiBold = Bake($"{SerifFolder}/EBGaramond-SemiBold.ttf", $"{SerifFolder}/EBGaramond-SemiBold SDF.asset", Characters, Padding, dynamicFallback);
            theme.ConfigureExtraFonts(led, serifRegular, serifSemiBold);
            // Main menu / end screen: Oswald (titles, buttons) and IBM Plex Mono (end screen body, readouts).
            TMP_FontAsset oswaldBold = Bake($"{OswaldFolder}/Oswald-Bold.ttf", $"{OswaldFolder}/Oswald-Bold SDF.asset", Characters, Padding, dynamicFallback);
            TMP_FontAsset oswaldMedium = Bake($"{OswaldFolder}/Oswald-Medium.ttf", $"{OswaldFolder}/Oswald-Medium SDF.asset", Characters, Padding, dynamicFallback);
            TMP_FontAsset plexRegular = Bake($"{PlexFolder}/IBMPlexMono-Regular.ttf", $"{PlexFolder}/IBMPlexMono-Regular SDF.asset", Characters, Padding, dynamicFallback);
            TMP_FontAsset plexBold = Bake($"{PlexFolder}/IBMPlexMono-Bold.ttf", $"{PlexFolder}/IBMPlexMono-Bold SDF.asset", Characters, Padding, dynamicFallback);
            theme.ConfigureMenuFonts(oswaldBold, oswaldMedium, plexRegular, plexBold);
            EditorUtility.SetDirty(theme);
            if (regular != null) SetTmpDefault(regular);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>The theme asset if it exists (no generation).</summary>
        public static UITheme LoadTheme() => AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);

        private static TMP_FontAsset Bake(string ttfName, string assetName, bool staticAtlas, TMP_FontAsset fallback)
        {
            return Bake($"{FontFolder}/{ttfName}.ttf", $"{FontFolder}/{assetName}.asset", staticAtlas ? Characters : null, Padding, fallback);
        }

        /// <summary>Bakes a font at any path; characters null = dynamic atlas (multi-atlas), else a static atlas.</summary>
        private static TMP_FontAsset Bake(string ttfPath, string path, string characters, int padding, TMP_FontAsset fallback)
        {
            bool staticAtlas = characters != null;
            string assetName = System.IO.Path.GetFileNameWithoutExtension(path);
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null) return null;

            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, !staticAtlas);
                asset.name = assetName;
                AssetDatabase.CreateAsset(asset, path);
                asset.atlasTextures[0].name = assetName + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
                asset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            else
            {
                // Re-run: refill the same asset so its GUID (and every reference) survives.
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.ClearFontAssetData(false);
            }

            if (staticAtlas)
            {
                if (!asset.TryAddCharacters(characters, out string missing) && !string.IsNullOrEmpty(missing))
                {
                    Debug.LogWarning($"[Fonts] {assetName} lacks: {missing}");
                }

                asset.atlasPopulationMode = AtlasPopulationMode.Static;
            }

            asset.fallbackFontAssetTable = fallback != null ? new List<TMP_FontAsset> { fallback } : new List<TMP_FontAsset>();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetTmpDefault(TMP_FontAsset font)
        {
            TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            SerializedProperty prop = so.FindProperty("m_defaultFontAsset");
            if (prop == null || prop.objectReferenceValue == font) return;
            prop.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static string BuildCharacters()
        {
            var chars = new System.Text.StringBuilder();
            for (char c = (char)32; c < 127; c++) chars.Append(c);
            chars.Append("çğıİöşüÇĞÖŞÜâîûÂÎÛéèêáàäëïôœß");
            chars.Append("“”‘’„«»‹›…–—•·°×÷±→←↑↓▲▼₺€%‰№§©®™");
            return chars.ToString();
        }
    }
}
