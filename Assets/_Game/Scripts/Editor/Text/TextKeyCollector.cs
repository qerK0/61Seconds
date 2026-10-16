using System;
using System.Collections.Generic;
using System.Reflection;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Finds every text key referenced by ScriptableObjects and prefabs under Assets/_Game,
    /// open scenes, and [TextKeyConstants] classes. Maps key -> referencing objects (null for code).
    /// </summary>
    public static class TextKeyCollector
    {
        private const string Root = "Assets/_Game";

        public static Dictionary<string, List<Object>> Collect()
        {
            var result = new Dictionary<string, List<Object>>(StringComparer.Ordinal);

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is ScriptableObject)
                    {
                        Scan(asset, result);
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Root }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null)
                {
                    ScanGameObject(prefab, result);
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ScanGameObject(root, result);
                }
            }

            foreach (Type type in TypeCache.GetTypesWithAttribute<TextKeyConstantsAttribute>())
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.IsLiteral && field.FieldType == typeof(string))
                    {
                        Add(result, (string)field.GetRawConstantValue(), null);
                    }
                }
            }

            return result;
        }

        /// <summary>Keys referenced but absent from the CSV (or with empty text if includeEmpty).</summary>
        public static List<string> FindMissing(Dictionary<string, List<Object>> refs, bool includeEmpty)
        {
            var missing = new List<string>();
            foreach (string key in refs.Keys)
            {
                bool absent = !TextDB.HasKey(key);
                bool empty = !absent && !TextDB.TryGet(key, out _);
                if (absent || (includeEmpty && empty))
                {
                    missing.Add(key);
                }
            }

            missing.Sort(StringComparer.Ordinal);
            return missing;
        }

        private static void ScanGameObject(GameObject go, Dictionary<string, List<Object>> result)
        {
            foreach (MonoBehaviour behaviour in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                {
                    Scan(behaviour, result);
                }
            }
        }

        private static void Scan(Object target, Dictionary<string, List<Object>> result)
        {
            var so = new SerializedObject(target);
            SerializedProperty it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.Generic && it.type == nameof(TextKey))
                {
                    SerializedProperty keyProp = it.FindPropertyRelative("key");
                    if (keyProp != null)
                    {
                        Add(result, keyProp.stringValue, target);
                    }
                }
            }
        }

        private static void Add(Dictionary<string, List<Object>> result, string key, Object owner)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (!result.TryGetValue(key, out List<Object> owners))
            {
                owners = new List<Object>();
                result[key] = owners;
            }

            if (owner != null && !owners.Contains(owner))
            {
                owners.Add(owner);
            }
        }
    }
}
