using System.IO;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Hot-reloads TextDB whenever strings_tr.csv is (re)imported, including during Play Mode.</summary>
    public sealed class TextCsvPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (path != TextDB.DefaultCsvPath)
                {
                    continue;
                }

                TextDB.LoadFromCsv(File.ReadAllText(path));
                Debug.Log("[TextDB] Reloaded " + path);
            }
        }
    }
}
