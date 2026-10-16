using SixtyOne.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Inspector for TextKey: key field, searchable dropdown button and a Turkish preview line.</summary>
    [CustomPropertyDrawer(typeof(TextKey))]
    public sealed class TextKeyDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 24f;
        private static GUIStyle previewStyle;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty keyProp = property.FindPropertyRelative("key");
            label = EditorGUI.BeginProperty(position, label, property);

            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect field = EditorGUI.PrefixLabel(line, label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect textRect = new Rect(field.x, field.y, field.width - ButtonWidth - 2f, field.height);
            Rect buttonRect = new Rect(textRect.xMax + 2f, field.y, ButtonWidth, field.height);
            EditorGUI.PropertyField(textRect, keyProp, GUIContent.none);

            if (GUI.Button(buttonRect, new GUIContent("▾", "Pick a key from strings_tr.csv")))
            {
                TextDB.EnsureLoaded();
                var dropdown = new TextKeyDropdown(new AdvancedDropdownState(), property.serializedObject, keyProp.propertyPath);
                dropdown.Show(field);
            }

            Rect previewRect = new Rect(field.x, line.yMax + EditorGUIUtility.standardVerticalSpacing, field.width, EditorGUIUtility.singleLineHeight);
            DrawPreview(previewRect, keyProp.hasMultipleDifferentValues ? null : keyProp.stringValue);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static void DrawPreview(Rect rect, string key)
        {
            if (previewStyle == null)
            {
                previewStyle = new GUIStyle(EditorStyles.miniLabel) { richText = false, clipping = TextClipping.Clip };
            }

            string text;
            Color color = previewStyle.normal.textColor;
            if (string.IsNullOrEmpty(key))
            {
                text = "(no key)";
            }
            else if (TextDB.TryGet(key, out string value))
            {
                text = value.Replace('\n', ' ');
            }
            else
            {
                text = TextDB.HasKey(key) ? "EMPTY TEXT in CSV" : "MISSING in strings_tr.csv";
                color = Color.magenta;
            }

            Color old = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUI.LabelField(rect, text, previewStyle);
            GUI.contentColor = old;
        }
    }
}
