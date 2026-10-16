using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Text
{
    /// <summary>
    /// Key -> Turkish text lookup loaded from strings_tr.csv (columns key,text,notes).
    /// Missing or empty keys render as [[key]] (magenta in UI via GetDisplay).
    /// </summary>
    public static class TextDB
    {
        /// <summary>Project path of the lore CSV.</summary>
        public const string DefaultCsvPath = "Assets/_Game/Content/Text/strings_tr.csv";

        private static readonly Dictionary<string, string> entries = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Raised after every (re)load. UI listens to refresh.</summary>
        public static event Action Changed;

        public static bool IsLoaded { get; private set; }

        /// <summary>All keys present in the CSV (including empty-text rows).</summary>
        public static IEnumerable<string> Keys
        {
            get
            {
                EnsureLoaded();
                return entries.Keys;
            }
        }

        /// <summary>Loads from a TextAsset.</summary>
        public static void Load(TextAsset csv)
        {
            if (csv == null)
            {
                Debug.LogWarning("[TextDB] No CSV assigned.");
                return;
            }

            LoadFromCsv(csv.text);
        }

        /// <summary>Parses CSV text and replaces all entries.</summary>
        public static void LoadFromCsv(string csv)
        {
            entries.Clear();
            List<List<string>> rows = CsvParser.Parse(csv);
            for (int r = 0; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                string key = row.Count > 0 ? row[0].Trim() : string.Empty;
                if (key.Length == 0 || (r == 0 && key == "key"))
                {
                    continue;
                }

                if (entries.ContainsKey(key))
                {
                    Debug.LogWarning($"[TextDB] Duplicate key '{key}' (row {r + 1}); last one wins.");
                }

                entries[key] = row.Count > 1 ? row[1] : string.Empty;
            }

            IsLoaded = true;
            Changed?.Invoke();
        }

        /// <summary>In the Editor, lazily loads the CSV from disk if nothing loaded it yet.</summary>
        public static void EnsureLoaded()
        {
#if UNITY_EDITOR
            if (!IsLoaded && System.IO.File.Exists(DefaultCsvPath))
            {
                LoadFromCsv(ReadCsvFile(DefaultCsvPath));
            }
#endif
        }

        /// <summary>Reads a CSV file even while a spreadsheet app (Excel, LibreOffice) holds it open for writing.</summary>
        public static string ReadCsvFile(string path)
        {
            using var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete);
            using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>True if the key has a row in the CSV (even with empty text).</summary>
        public static bool HasKey(string key)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(key) && entries.ContainsKey(key);
        }

        /// <summary>Gets non-empty text for a key.</summary>
        public static bool TryGet(string key, out string text)
        {
            EnsureLoaded();
            text = null;
            return !string.IsNullOrEmpty(key) && entries.TryGetValue(key, out text) && !string.IsNullOrEmpty(text);
        }

        /// <summary>Text for the key, or "[[key]]".</summary>
        public static string Get(string key)
        {
            return TryGet(key, out string text) ? text : $"[[{key}]]";
        }

        /// <summary>Like Get, but missing keys are wrapped in a magenta rich-text tag for TMP.</summary>
        public static string GetDisplay(string key)
        {
            return TryGet(key, out string text) ? text : $"<color=#FF00FF>[[{key}]]</color>";
        }

        /// <summary>string.Format on the display text; never throws on bad placeholders.</summary>
        public static string Format(string key, params object[] args)
        {
            string format = GetDisplay(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }
    }
}
