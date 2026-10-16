using System.Collections.Generic;
using System.Linq;
using SixtyOne.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Searchable dropdown of all CSV keys, grouped by their first dot segment.</summary>
    public sealed class TextKeyDropdown : AdvancedDropdown
    {
        private sealed class KeyItem : AdvancedDropdownItem
        {
            public readonly string Key;

            public KeyItem(string key) : base(key)
            {
                Key = key;
            }
        }

        private readonly SerializedObject serializedObject;
        private readonly string propertyPath;

        public TextKeyDropdown(AdvancedDropdownState state, SerializedObject serializedObject, string propertyPath) : base(state)
        {
            this.serializedObject = serializedObject;
            this.propertyPath = propertyPath;
            minimumSize = new Vector2(320f, 400f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("strings_tr.csv");
            var groups = new Dictionary<string, AdvancedDropdownItem>();

            foreach (string key in TextDB.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
            {
                int dot = key.IndexOf('.');
                string groupName = dot > 0 ? key.Substring(0, dot) : "(root)";
                if (!groups.TryGetValue(groupName, out AdvancedDropdownItem group))
                {
                    group = new AdvancedDropdownItem(groupName);
                    groups[groupName] = group;
                    root.AddChild(group);
                }

                group.AddChild(new KeyItem(key));
            }

            if (groups.Count == 0)
            {
                root.AddChild(new AdvancedDropdownItem("(CSV empty or missing)"));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (!(item is KeyItem keyItem) || serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            serializedObject.Update();
            SerializedProperty prop = serializedObject.FindProperty(propertyPath);
            if (prop != null)
            {
                prop.stringValue = keyItem.Key;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
