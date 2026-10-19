using System;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>
    /// Player options shared by the main menu and the pause menu, saved in PlayerPrefs: master / music / effects
    /// volume (0..1), Parlaklık (0..1, 0.5 = default) and Reduce Effects. Master drives AudioListener.volume; music and effects scale the cue
    /// volumes in the audio services; Reduce Effects is copied onto GameManager's GameSettings.
    /// </summary>
    public static class UserSettings
    {
        private const string MasterKey = "61s.volume.master";
        private const string MusicKey = "61s.volume.music";
        private const string SfxKey = "61s.volume.sfx";
        private const string ReduceKey = "61s.reduceEffects";
        private const string BrightnessKey = "61s.brightness";

        private static bool loaded;
        private static float master = 1f;
        private static float music = 1f;
        private static float sfx = 1f;
        private static bool reduceEffects;
        private static float brightness = 0.5f;

        /// <summary>Raised after any value changes (already saved and applied).</summary>
        public static event Action Changed;

        public static float Master
        {
            get { Load(); return master; }
            set { Load(); master = Mathf.Clamp01(value); Save(); }
        }

        public static float Music
        {
            get { Load(); return music; }
            set { Load(); music = Mathf.Clamp01(value); Save(); }
        }

        public static float Sfx
        {
            get { Load(); return sfx; }
            set { Load(); sfx = Mathf.Clamp01(value); Save(); }
        }

        /// <summary>Parlaklık slider (0..1): 0.5 = BrightnessConfig's values, the ends = ± its user range in EV.</summary>
        public static float Brightness
        {
            get { Load(); return brightness; }
            set { Load(); brightness = Mathf.Clamp01(value); Save(); }
        }

        public static bool ReduceEffects
        {
            get { Load(); return reduceEffects; }
            set { Load(); reduceEffects = value; Save(); }
        }

        /// <summary>Applies the stored values (master volume, Reduce Effects on the given settings asset).</summary>
        public static void Apply(GameSettings settings = null)
        {
            Load();
            AudioListener.volume = master;
            if (settings != null) settings.ReduceEffects = reduceEffects;
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(MasterKey, 1f);
            music = PlayerPrefs.GetFloat(MusicKey, 1f);
            sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
            reduceEffects = PlayerPrefs.GetInt(ReduceKey, 0) == 1;
            brightness = PlayerPrefs.GetFloat(BrightnessKey, 0.5f);
        }

        private static void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, master);
            PlayerPrefs.SetFloat(MusicKey, music);
            PlayerPrefs.SetFloat(SfxKey, sfx);
            PlayerPrefs.SetInt(ReduceKey, reduceEffects ? 1 : 0);
            PlayerPrefs.SetFloat(BrightnessKey, brightness);
            PlayerPrefs.Save();
            Apply(GameManager.Instance != null ? GameManager.Instance.Settings : null);
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            loaded = false;
            Changed = null;
        }
    }
}
