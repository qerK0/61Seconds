using TMPro;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Shared world-label material: dark outline + underlay so labels read on light tiles and in post-quake darkness.</summary>
    internal static class WorldLabelStyle
    {
        private const string MaterialPath = "Assets/_Game/Data/WorldLabel_SDF.mat";

        /// <summary>Assigns the theme font (Inter SemiBold) and the shared outlined material to a world-space label.</summary>
        public static void Apply(TextMeshPro label)
        {
            if (label == null) return;
            SixtyOne.UI.UITheme theme = FontAssetsGenerator.LoadTheme();
            if (theme != null && theme.SemiBold != null) label.font = theme.SemiBold;
            if (label.font == null) return;
            label.fontSharedMaterial = GetOrCreate(label.font);
        }

        private static Material GetOrCreate(TMP_FontAsset font)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(font.material) { name = "WorldLabel_SDF" };
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            // Follow the current font (atlas, gradient scale) when the theme font changes.
            mat.shader = font.material.shader;
            mat.CopyPropertiesFromMaterial(font.material);

            // HDR face (>1) so white survives ACES + the negative cinematic exposure.
            mat.SetColor(ShaderUtilities.ID_FaceColor, new Color(1.7f, 1.7f, 1.7f, 1f));
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.8f));
            mat.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.5f);
            mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.35f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
