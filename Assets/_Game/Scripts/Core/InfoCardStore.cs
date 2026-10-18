using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// The info cards ("Deprem Bilgisi", 1..8) the player has seen, saved in PlayerPrefs across runs (comma-separated
    /// card numbers). The game picks unseen cards first; the main menu Galeri shows the seen ones.
    /// </summary>
    public static class InfoCardStore
    {
        public const string PrefsKey = "SixtyOne.SeenInfoCards";
        public const string LastKey = "SixtyOne.LastInfoCard";

        public static HashSet<int> Load() => Parse(PlayerPrefs.GetString(PrefsKey, string.Empty));

        /// <summary>The card shown last (0 = none yet).</summary>
        public static int LastShown => PlayerPrefs.GetInt(LastKey, 0);

        /// <summary>Marks a card seen and remembers it as the last shown.</summary>
        public static void MarkSeen(int card)
        {
            if (card < 1 || card > InfoCardRules.CardCount) return;
            HashSet<int> seen = Load();
            seen.Add(card);
            PlayerPrefs.SetString(PrefsKey, Serialize(seen));
            PlayerPrefs.SetInt(LastKey, card);
            PlayerPrefs.Save();
        }

        /// <summary>Pure: the saved string → card numbers (invalid entries ignored).</summary>
        public static HashSet<int> Parse(string saved)
        {
            var cards = new HashSet<int>();
            if (string.IsNullOrEmpty(saved)) return cards;
            foreach (string part in saved.Split(','))
            {
                if (int.TryParse(part.Trim(), out int card) && card >= 1 && card <= InfoCardRules.CardCount) cards.Add(card);
            }

            return cards;
        }

        /// <summary>Pure: card numbers → the saved string (sorted, so it is stable).</summary>
        public static string Serialize(IEnumerable<int> cards)
        {
            var list = new List<int>(cards);
            list.Sort();
            return string.Join(",", list);
        }
    }
}
