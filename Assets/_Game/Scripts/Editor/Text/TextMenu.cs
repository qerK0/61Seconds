using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SixtyOne.Editor
{
    /// <summary>SixtyOne > Text menu: Validate, Export Missing Keys, Reload.</summary>
    public static class TextMenu
    {
        [MenuItem("SixtyOne/Text/Validate")]
        public static void Validate()
        {
            Reload();
            Dictionary<string, List<Object>> refs = TextKeyCollector.Collect();
            int missingCount = 0;
            int emptyCount = 0;

            foreach (KeyValuePair<string, List<Object>> pair in refs.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                bool absent = !TextDB.HasKey(pair.Key);
                bool empty = !absent && !TextDB.TryGet(pair.Key, out _);
                if (!absent && !empty) continue;

                if (absent) missingCount++; else emptyCount++;
                string problem = absent ? "MISSING" : "EMPTY";
                if (pair.Value.Count == 0)
                {
                    Debug.LogWarning($"[Text] {problem} key '{pair.Key}' (used from code)");
                }

                foreach (Object owner in pair.Value)
                {
                    // Context object makes the console entry ping the asset when clicked.
                    Debug.LogWarning($"[Text] {problem} key '{pair.Key}' used by {owner.name}", owner);
                }
            }

            List<string> unused = TextDB.Keys.Where(k => !refs.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
            if (unused.Count > 0)
            {
                Debug.Log($"[Text] {unused.Count} CSV key(s) not referenced by assets/open scenes/code:\n" + string.Join("\n", unused));
            }

            string summary = $"[Text] Validate: {refs.Count} referenced, {missingCount} missing, {emptyCount} empty, {unused.Count} unused.";
            if (missingCount + emptyCount > 0) Debug.LogWarning(summary); else Debug.Log(summary);
        }

        [MenuItem("SixtyOne/Text/Export Missing Keys")]
        public static void ExportMissingKeys()
        {
            Reload();
            List<string> missing = TextKeyCollector.FindMissing(TextKeyCollector.Collect(), includeEmpty: false);
            if (missing.Count == 0)
            {
                Debug.Log("[Text] No missing keys.");
                return;
            }

            string path = TextDB.DefaultCsvPath;
            var builder = new StringBuilder();
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                builder.Append("key,text,notes\n");
            }
            else
            {
                string existing = TextDB.ReadCsvFile(path);
                if (existing.Length > 0 && !existing.EndsWith("\n"))
                {
                    builder.Append('\n');
                }
            }

            foreach (string key in missing)
            {
                builder.Append(CsvParser.Escape(key)).Append(",,\n");
            }

            File.AppendAllText(path, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            Debug.Log($"[Text] Appended {missing.Count} empty row(s) to {path}:\n" + string.Join("\n", missing));
        }

        [MenuItem("SixtyOne/Text/Reload CSV")]
        public static void Reload()
        {
            if (File.Exists(TextDB.DefaultCsvPath))
            {
                TextDB.LoadFromCsv(TextDB.ReadCsvFile(TextDB.DefaultCsvPath));
            }
            else
            {
                Debug.LogWarning("[Text] CSV not found at " + TextDB.DefaultCsvPath);
            }
        }
    }
}
