using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Sets private [SerializeField]s on components/assets by name (for code-built scenes).
    /// Logs an error if a field name is wrong so broken wiring is visible immediately.
    /// </summary>
    public static class SerializedWire
    {
        public static void Set(Object target, string field, Object value)
        {
            Apply(target, field, p => p.objectReferenceValue = value);
        }

        public static void SetString(Object target, string field, string value)
        {
            Apply(target, field, p => p.stringValue = value);
        }

        public static void SetTextKey(Object target, string field, string key)
        {
            Apply(target, field + ".key", p => p.stringValue = key);
        }

        public static void SetFloat(Object target, string field, float value)
        {
            Apply(target, field, p => p.floatValue = value);
        }

        public static void SetInt(Object target, string field, int value)
        {
            Apply(target, field, p => p.intValue = value);
        }

        public static void SetBool(Object target, string field, bool value)
        {
            Apply(target, field, p => p.boolValue = value);
        }

        /// <summary>Sets an enum by its underlying int value.</summary>
        public static void SetEnum(Object target, string field, int value)
        {
            Apply(target, field, p => p.intValue = value);
        }

        public static void SetColor(Object target, string field, Color value)
        {
            Apply(target, field, p => p.colorValue = value);
        }

        public static void SetRect(Object target, string field, Rect value)
        {
            Apply(target, field, p => p.rectValue = value);
        }

        /// <summary>Replaces an array/List of object references.</summary>
        public static void SetArray(Object target, string field, Object[] values)
        {
            Apply(target, field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            });
        }

        /// <summary>Replaces an array/List of strings.</summary>
        public static void SetStringArray(Object target, string field, string[] values)
        {
            Apply(target, field, p =>
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    p.GetArrayElementAtIndex(i).stringValue = values[i];
                }
            });
        }

        private static void Apply(Object target, string field, System.Action<SerializedProperty> set)
        {
            if (target == null)
            {
                Debug.LogError($"[SerializedWire] Null target for field '{field}'.");
                return;
            }

            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[SerializedWire] {target.GetType().Name} has no serialized field '{field}'.", target);
                return;
            }

            set(prop);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
