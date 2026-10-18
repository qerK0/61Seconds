using System.Collections.Generic;
using SixtyOne.Cinematic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SixtyOne.Editor
{
    /// <summary>Generated cinematic assets (per-act profiles, base volume profile, dust material).</summary>
    public sealed class CinematicAssets
    {
        public CinematicProfile[] ActProfiles;
        public VolumeProfile BaseVolume;
        public Material DustMaterial;
    }

    /// <summary>
    /// SixtyOne > Cinematic > Generate Profiles. Creates Assets/_Game/Data/Cinematic/: CinematicAct1-3
    /// (each act darker and harsher; all share the dust Color Lookup), CinematicBaseVolume (ACES, color adjustments,
    /// split toning, vignette, Thin1
    /// grain, bloom, color lookup, chromatic aberration, lens distortion) and the dust particle material.
    /// Existing profiles keep their tuned values; only missing ones get act defaults.
    /// </summary>
    public static class CinematicAssetsGenerator
    {
        public const string Folder = "Assets/_Game/Data/Cinematic";

        [MenuItem("SixtyOne/Cinematic/Generate Profiles", priority = 20)]
        public static void GenerateMenu()
        {
            Generate(null);
            Debug.Log("[Cinematic] Profiles generated under " + Folder + ".");
        }

        [MenuItem("SixtyOne/Cinematic/Reset Profiles To Act Defaults", priority = 21)]
        public static void ResetMenu()
        {
            for (int act = 1; act <= 3; act++)
            {
                CinematicProfile profile = AssetDatabase.LoadAssetAtPath<CinematicProfile>(ProfilePath(act));
                if (profile == null) continue;
                profile.ApplyActDefaults(act);
                EditorUtility.SetDirty(profile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Cinematic] Profiles reset to act defaults.");
        }

        public static CinematicAssets Generate(Texture dustTexture)
        {
            AssetUtil.EnsureFolder(Folder);
            var result = new CinematicAssets { ActProfiles = new CinematicProfile[3] };

            for (int act = 1; act <= 3; act++)
            {
                string path = ProfilePath(act);
                CinematicProfile existing = AssetDatabase.LoadAssetAtPath<CinematicProfile>(path);
                CinematicProfile profile = existing != null ? existing : AssetUtil.LoadOrCreate<CinematicProfile>(path);
                if (existing == null)
                {
                    profile.ApplyActDefaults(act);
                    EditorUtility.SetDirty(profile);
                }

                result.ActProfiles[act - 1] = profile;
            }

            // The shared dust LUT under every act (profiles that already have a LUT keep theirs).
            Texture dust = ImportLut(DustLutPath);
            ImportLut(NeutralLutPath);
            foreach (CinematicProfile profile in result.ActProfiles)
            {
                if (profile.Lut != null || dust == null) continue;
                profile.ConfigureLut(dust, DefaultLutContribution);
                EditorUtility.SetDirty(profile);
            }

            result.BaseVolume = BuildBaseVolume(result.ActProfiles[0]);
            result.DustMaterial = BuildDustMaterial(dustTexture);
            AssetDatabase.SaveAssets();
            return result;
        }

        private static string ProfilePath(int act) => $"{Folder}/CinematicAct{act}.asset";

        /// <summary>The "dust" grade: lifted blacks, slight desaturation, teal shadows, warm dusty highlights (32 strip).</summary>
        public const string DustLutPath = "Assets/_Game/Art/LUT/LUT_61s_Dust_32.png";

        /// <summary>Identity LUT for checking the import: at contribution 1 the image must not change.</summary>
        public const string NeutralLutPath = "Assets/_Game/Art/LUT/LUT_Neutral_32.png";

        public const float DefaultLutContribution = 0.6f;

        /// <summary>
        /// Imports a URP Color Lookup strip as URP expects it: Default texture, sRGB OFF (ColorLookup.ValidateLUT rejects
        /// sRGB formats; with sRGB on the neutral LUT shifts the image), no mipmaps, no compression,
        /// Clamp, Bilinear, no power-of-two rescale (a 32 strip is 1024 x 32), max size large enough to keep it 1:1.
        /// </summary>
        public static Texture ImportLut(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return null;
            bool changed = importer.textureType != TextureImporterType.Default || importer.sRGBTexture || importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.wrapMode != TextureWrapMode.Clamp
                || importer.filterMode != FilterMode.Bilinear || importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize < 2048
                || importer.alphaSource != TextureImporterAlphaSource.None;
            if (changed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Base look as a VolumeProfile (preview of act 1; the director overrides values at runtime on a copy).</summary>
        private static VolumeProfile BuildBaseVolume(CinematicProfile look)
        {
            string path = Folder + "/CinematicBaseVolume.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            foreach (VolumeComponent old in new List<VolumeComponent>(profile.components))
            {
                if (old != null) Object.DestroyImmediate(old, true);
            }

            profile.components.Clear();

            Add<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(look.PostExposure);
            color.contrast.Override(look.Contrast);
            color.saturation.Override(look.Saturation);
            color.colorFilter.Override(look.ColorFilter);

            SplitToning split = Add<SplitToning>(profile);
            split.shadows.Override(look.SplitShadows);
            split.highlights.Override(look.SplitHighlights);
            split.balance.Override(look.SplitBalance);

            Vignette vignette = Add<Vignette>(profile);
            vignette.color.Override(Color.black);
            vignette.intensity.Override(look.Vignette);
            vignette.smoothness.Override(look.VignetteSmoothness);

            FilmGrain grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(look.GrainIntensity);
            grain.response.Override(look.GrainResponse);

            Bloom bloom = Add<Bloom>(profile);
            bloom.threshold.Override(look.BloomThreshold);
            bloom.intensity.Override(look.BloomIntensity);
            bloom.scatter.Override(look.BloomScatter);

            ColorLookup lookup = Add<ColorLookup>(profile);
            lookup.texture.Override(look.Lut);
            lookup.contribution.Override(look.Lut != null ? look.LutContribution : 0f);

            Add<ChromaticAberration>(profile).intensity.Override(0f);
            Add<LensDistortion>(profile).intensity.Override(0f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static Material BuildDustMaterial(Texture texture)
        {
            string path = Folder + "/CinematicDust.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (texture != null) material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
