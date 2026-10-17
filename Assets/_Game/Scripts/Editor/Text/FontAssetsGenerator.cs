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
    /// for Inter Regular / SemiBold / Bold (OFL, Assets/_Game/Art/Fonts) with ASCII + full Turkish coverage, gives
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
            EditorUtility.SetDirty(theme);
            if (regular != null) SetTmpDefault(regular);
            AssetDatabase.SaveAssets();
            return theme;
        }

        /// <summary>The theme asset if it exists (no generation).</summary>
        public static UITheme LoadTheme() => AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);

        private static TMP_FontAsset Bake(string ttfName, string assetName, bool staticAtlas, TMP_FontAsset fallback)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{ttfName}.ttf");
            if (font == null) return null;

            string path = $"{FontFolder}/{assetName}.asset";
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, !staticAtlas);
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
                if (!asset.TryAddCharacters(Characters, out string missing) && !string.IsNullOrEmpty(missing))
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
            chars.Append("“”‘’„«»…–—•·°×÷±→←↑↓▲▼₺€%‰№§©®™");
            return chars.ToString();
        }
    }
}
