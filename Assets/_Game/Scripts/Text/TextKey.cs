using System;
using UnityEngine;

namespace SixtyOne.Text
{
    /// <summary>Serializable reference to a row of strings_tr.csv. Has a searchable Inspector dropdown.</summary>
    [Serializable]
    public struct TextKey : IEquatable<TextKey>
    {
        [Tooltip("Key in strings_tr.csv, e.g. act1.n01.body")]
        [SerializeField] private string key;

        public TextKey(string key)
        {
            this.key = key;
        }

        public string Key => key ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(key);

        /// <summary>Text or "[[key]]".</summary>
        public string Text => TextDB.Get(Key);

        /// <summary>Text, or magenta "[[key]]" for TMP.</summary>
        public string Display => TextDB.GetDisplay(Key);

        public static implicit operator TextKey(string value) => new TextKey(value);

        public bool Equals(TextKey other) => string.Equals(Key, other.Key, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TextKey other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();
        public override string ToString() => Key;
    }
}
