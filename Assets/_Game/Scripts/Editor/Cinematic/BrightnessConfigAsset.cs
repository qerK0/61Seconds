using SixtyOne.Cinematic;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Cinematic > Create Brightness Config: Data/Cinematic/Resources/BrightnessConfig.asset, the one place
    /// to tweak the overall brightness (CinematicDirector loads it from Resources; no scene rebuild needed).
    /// An existing asset is kept as it is.
    /// </summary>
    public static class BrightnessConfigAsset
    {
        public const string Path = CinematicAssetsGenerator.Folder + "/Resources/" + BrightnessConfig.ResourcePath + ".asset";

        [MenuItem("SixtyOne/Cinematic/Create Brightness Config", priority = 22)]
        public static void CreateMenu()
        {
            BrightnessConfig config = Ensure();
            Selection.activeObject = config;
            Debug.Log("[Cinematic] Brightness config at " + Path + ".");
        }

        public static BrightnessConfig Ensure()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BrightnessConfig>(Path);
            if (existing != null) return existing;
            BrightnessConfig created = AssetUtil.LoadOrCreate<BrightnessConfig>(Path);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
