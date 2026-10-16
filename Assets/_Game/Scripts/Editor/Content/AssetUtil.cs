using System.IO;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Folder + ScriptableObject helpers for generators (idempotent: reuse existing assets).</summary>
    public static class AssetUtil
    {
        /// <summary>Creates every missing folder in a project path like Assets/_Game/Data/Nodes.</summary>
        public static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Loads the asset at path, or creates it. Replaces an asset of the wrong type.</summary>
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(Path.GetDirectoryName(path));
            Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing is T typed && existing.GetType() == typeof(T))
            {
                return typed;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
