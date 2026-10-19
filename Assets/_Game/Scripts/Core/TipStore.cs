using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// The collected "Deprem Notu" tips across runs, saved in PlayerPrefs (comma-separated tip ids). A new run starts
    /// with them, the main menu's Galeri shows them, and "Yeniden Dene" keeps them.
    /// </summary>
    public static class TipStore
    {
        public const string PrefsKey = "SixtyOne.CollectedTips";

        /// <summary>Every saved tip id.</summary>
        public static HashSet<string> Load() => Parse(PlayerPrefs.GetString(PrefsKey, string.Empty));

        /// <summary>Copies the saved tips into a run's state (persistent data survives every rewind anyway).</summary>
        public static void LoadInto(GameState state)
        {
            if (state == null) return;
            foreach (string id in Load()) state.CollectedTips.Add(id);
        }

        /// <summary>Saves one more collected tip (no-op if already saved).</summary>
        public static void Add(string tipId)
        {
            if (string.IsNullOrEmpty(tipId)) return;
            HashSet<string> tips = Load();
            if (!tips.Add(tipId)) return;
            PlayerPrefs.SetString(PrefsKey, Serialize(tips));
            PlayerPrefs.Save();
        }

        /// <summary>Pure: the saved string → tip ids (empty entries ignored).</summary>
        public static HashSet<string> Parse(string saved)
        {
            var tips = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(saved)) return tips;
            foreach (string id in saved.Split(','))
            {
                string trimmed = id.Trim();
                if (trimmed.Length > 0) tips.Add(trimmed);
            }

            return tips;
        }

        /// <summary>Pure: tip ids → the saved string (sorted, so it is stable).</summary>
        public static string Serialize(IEnumerable<string> tips)
        {
            var list = new List<string>(tips);
            list.Sort(StringComparer.Ordinal);
            return string.Join(",", list);
        }
    }
}
