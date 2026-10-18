using System.Collections.Generic;
using System.IO;
using SixtyOne.Audio;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Audio > Import Team Audio. Moves the delivered clips from Assets/_Game/Voice to Assets/_Game/Audio
    /// (AssetDatabase moves keep the .meta files), applies import settings (short SFX: Decompress On Load; music,
    /// ambience and long loops: Streaming Vorbis) and puts each clip on its cue plus the dialogue voice profile.
    /// Run after SixtyOne > Generate Building Levels (it creates the cues). Re-running is safe.
    /// </summary>
    public static class TeamAudioImporter
    {
        private const string SourceDir = "Assets/_Game/Voice";
        private const string AudioRoot = "Assets/_Game/Audio";
        private const string CueDir = "Assets/_Game/Data/Audio";

        /// <summary>Cue id → clip path under Assets/_Game/Audio.</summary>
        private static readonly string[,] CueClips =
        {
            { "cue_afad_alert", "Generated/alarm.mp3" },
            { "cue_quake_f4", "7.1deprem.wav" },
            { "cue_quake_f3", "6.2deprem.ogg" },
            { "cue_quake_f2", "6.9deprem.wav" },
            { "cue_quake_rumble", "7.1deprem.wav" },
            { "cue_placement_alarm", "hırtladenkgelmedenöncec.mp3" },
            { "cue_furniture_crash", "heavyobjectfall.wav" },
            { "cue_pickup", "eşya alma.wav" },
            { "cue_door", "kapı acma kapama.wav" },
            { "cue_footsteps", "3lu grup yürüyüşü.mp3" },
            { "cue_craft", "Trimmed/craft_short.wav" },
            { "cue_whistle", "duduk.wav" },
            { "cue_dog_bark", "havlama.wav" },
            { "cue_dog_growl", "hrrlayankopke.mp3" },
            { "cue_electric", "Trimmed/electric_short.wav" },
            { "cue_heartbeat", "heartbeat.wav" },
            { "cue_amb_drip", "damlayan su.wav" },
            { "cue_music_prep", "Start_muzıgı_sakin_anlar.mp3" },
            { "cue_music_tension", "ilk deprem sonrası sarkı ve sigorta.mp3" },
            { "cue_music_outro", "dışarı varıncaki ses.mp3" },
            { "cue_death_sting", "Generated/sfx_death_sting.wav" },
            { "cue_rewind", "Generated/sfx_rewind.wav" },
            { "cue_glass_shatter", "Generated/sfx_glass_shatter.wav" },
            { "cue_choice_click", "Generated/sfx_choice_click.wav" },
            { "cue_denied", "Generated/sfx_denied.wav" },
            { "cue_qte_hit", "Generated/sfx_qte_hit.wav" },
            { "cue_qte_miss", "Generated/sfx_qte_miss.wav" },
            { "cue_qte_success", "Generated/sfx_qte_success.wav" },
            { "cue_building_alarm", "Generated/sfx_building_alarm_loop.wav" },
            { "cue_countdown_tick", "Generated/sfx_countdown_tick.wav" },
            { "cue_descend", "Generated/sfx_descend.wav" },
            { "cue_fire", "Generated/sfx_fire_explosion.wav" },
            { "cue_search", "Generated/sfx_search.wav" },
            { "cue_car_alarm", "Generated/sfx_car_alarm_loop.wav" },
            { "cue_gas_hiss", "Generated/sfx_gas_hiss_loop.wav" },
            { "cue_valve_creak", "Generated/sfx_valve_creak.wav" },
            { "cue_paper_rustle", "Generated/sfx_paper_rustle.wav" }
        };

        /// <summary>Music, ambience and long loops: streamed (Vorbis). Everything else decompresses on load.</summary>
        private static readonly HashSet<string> Streamed = new HashSet<string>
        {
            "Start_muzıgı_sakin_anlar.mp3", "ilk deprem sonrası sarkı ve sigorta.mp3", "dışarı varıncaki ses.mp3",
            "damlayan su.wav", "heartbeat.wav", "hırtladenkgelmedenöncec.mp3", "elektrik kacagı.wav", "craft sesi.wav",
            "3lu grup yürüyüşü.mp3", "loop.mp3"
        };

        [MenuItem("SixtyOne/Audio/Import Team Audio", priority = 30)]
        public static void ImportMenu()
        {
            int moved = MoveDelivered();
            int configured = ApplyImportSettings();
            int wired = WireCues();
            WireVoices();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Audio] Moved {moved} clips, import settings on {configured}, {wired} cues wired. Next: Build Test Scene.");
        }

        private static int MoveDelivered()
        {
            if (!AssetDatabase.IsValidFolder(SourceDir)) return 0;
            EnsureFolder(AudioRoot);
            int moved = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SourceDir }))
            {
                string from = AssetDatabase.GUIDToAssetPath(guid);
                string relative = from.Substring(SourceDir.Length + 1);
                string to = AudioRoot + "/" + relative;
                EnsureFolder(Path.GetDirectoryName(to).Replace('\\', '/'));
                if (AssetDatabase.LoadAssetAtPath<AudioClip>(to) != null)
                {
                    Debug.LogWarning($"[Audio] {to} already exists; left {from} in place.");
                    continue;
                }

                string error = AssetDatabase.MoveAsset(from, to);
                if (string.IsNullOrEmpty(error)) moved++;
                else Debug.LogError($"[Audio] Move {from} → {to} failed: {error}");
            }

            DeleteIfEmpty(SourceDir + "/Generated");
            DeleteIfEmpty(SourceDir);
            return moved;
        }

        private static int ApplyImportSettings()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer)) continue;
                bool streamed = Streamed.Contains(Path.GetFileName(path));
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = streamed ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = streamed ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = streamed ? 0.6f : 1f;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = streamed;
                importer.SaveAndReimport();
                count++;
            }

            return count;
        }

        private static int WireCues()
        {
            int wired = 0;
            for (int i = 0; i < CueClips.GetLength(0); i++)
            {
                AudioCue cue = AssetDatabase.LoadAssetAtPath<AudioCue>($"{CueDir}/{CueClips[i, 0]}.asset");
                AudioClip clip = Clip(CueClips[i, 1]);
                if (cue == null || clip == null)
                {
                    Debug.LogWarning($"[Audio] Skipped {CueClips[i, 0]} (cue {(cue != null ? "ok" : "missing: run Generate Building Levels")}, clip {(clip != null ? "ok" : "missing: " + CueClips[i, 1])}).");
                    continue;
                }

                cue.SetClips(clip);
                EditorUtility.SetDirty(cue);
                wired++;
            }

            return wired;
        }

        private static void WireVoices()
        {
            DialogueVoiceProfile profile = AssetDatabase.LoadAssetAtPath<DialogueVoiceProfile>($"{CueDir}/DialogueVoices.asset");
            if (profile == null)
            {
                Debug.LogWarning("[Audio] DialogueVoices.asset missing: run Generate Building Levels.");
                return;
            }

            AudioClip female = Clip("kadın dialog.mp3");
            AudioClip male = Clip("erkekkonuşma.mp3");
            profile.Configure(new[]
            {
                new DialogueVoiceProfile.Voice { speakerKey = "npc.sevim.name", clip = female, pitch = 1.15f },
                new DialogueVoiceProfile.Voice { speakerKey = "npc.cemil.name", clip = male, pitch = 1f }
            }, 0.8f);
            EditorUtility.SetDirty(profile);
        }

        private static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioRoot + "/" + relative);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void DeleteIfEmpty(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            if (AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length == 0) AssetDatabase.DeleteAsset(folder);
        }
    }
}
