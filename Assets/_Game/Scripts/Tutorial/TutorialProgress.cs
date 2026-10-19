using UnityEngine;

namespace SixtyOne.Tutorial
{
    /// <summary>
    /// PlayerPrefs flags of the tutorial: whether the "Önce öğreticiyi oynamak ister misin?" question was already
    /// asked (it is asked once, on the first Oyunu Başlat), and whether the tutorial was ever finished.
    /// </summary>
    public static class TutorialProgress
    {
        private const string AskedKey = "61s.tutorial.asked";
        private const string DoneKey = "61s.tutorial.done";

        /// <summary>True once the first-run question was shown (either answer).</summary>
        public static bool Asked
        {
            get => PlayerPrefs.GetInt(AskedKey, 0) == 1;
            set => Save(AskedKey, value);
        }

        /// <summary>True once the tutorial reached its end panel (finished or skipped).</summary>
        public static bool Completed
        {
            get => PlayerPrefs.GetInt(DoneKey, 0) == 1;
            set => Save(DoneKey, value);
        }

        private static void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
