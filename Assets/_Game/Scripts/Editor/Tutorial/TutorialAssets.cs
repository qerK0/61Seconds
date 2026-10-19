using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Read-only loader of the building data the tutorial scene reuses (items, configs, cues, cinematic profiles,
    /// the UI theme). Unlike the building generator it never regenerates or saves those assets.
    /// </summary>
    public static class TutorialAssets
    {
        /// <summary>The game's generated data as a BuildingAssets (only what the tutorial needs), or null if missing.</summary>
        public static BuildingAssets Load()
        {
            string root = BuildingLevelsGenerator.DataRoot;
            var data = new BuildingAssets
            {
                Content = Load<GameContent>(root + "/GameContent.asset"),
                TextSettings = Load<TextDBSettings>(root + "/TextDBSettings.asset"),
                Settings = Load<GameSettings>(root + "/GameSettings.asset"),
                Level = Load<LevelConfig>(root + "/LevelConfig.asset"),
                AudioMap = Load<AudioEventMap>(BuildingLevelsGenerator.AudioDir + "/AudioEventMap.asset"),
                RecipeBook = Load<RecipeBook>(BuildingLevelsGenerator.RecipesDir + "/RecipeBook.asset"),
                Transition = Load<TransitionConfig>(root + "/TransitionConfig.asset"),
                Movement = Load<MovementConfig>(root + "/MovementConfig.asset"),
                Visual = Load<RoomVisualConfig>(root + "/RoomVisualConfig.asset")
            };

            if (data.Content == null || data.Level == null || data.Movement == null)
            {
                Debug.LogError("[Tutorial] Building data is missing: run SixtyOne > Generate Building Levels once first.");
                return null;
            }

            foreach (Item item in data.Content.Items)
            {
                if (item != null) data.Items[item.Id] = item;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:AudioCue", new[] { BuildingLevelsGenerator.AudioDir }))
            {
                var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(AssetDatabase.GUIDToAssetPath(guid));
                if (cue != null) data.Cues.Add(cue);
            }

            data.SearchCue = Cue(data, "cue_search");
            data.PaperCue = Cue(data, "cue_paper_rustle");
            data.MusicPrep = Cue(data, "cue_music_prep");
            return data;
        }

        /// <summary>The cue with this id from the loaded library (null if missing).</summary>
        public static AudioCue Cue(BuildingAssets data, string id) => data.Cues.Find(c => c != null && c.Id == id);

        /// <summary>The existing act / street profiles, base volume and dust material (CinematicAssetsGenerator's output).</summary>
        public static CinematicAssets Cinematic()
        {
            string folder = CinematicAssetsGenerator.Folder;
            var result = new CinematicAssets
            {
                ActProfiles = new CinematicProfile[3],
                StreetProfile = Load<CinematicProfile>(folder + "/CinematicStreet.asset"),
                BaseVolume = Load<VolumeProfile>(folder + "/CinematicBaseVolume.asset"),
                DustMaterial = Load<Material>(folder + "/CinematicDust.mat")
            };
            for (int act = 1; act <= 3; act++) result.ActProfiles[act - 1] = Load<CinematicProfile>($"{folder}/CinematicAct{act}.asset");
            return result;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
