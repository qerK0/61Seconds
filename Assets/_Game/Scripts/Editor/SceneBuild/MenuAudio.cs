using SixtyOne.Audio;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Audio cues of the main menu and the end screen. The clips come from tools/gen_sfx.py (sfx_menu_*; run
    /// `py tools/gen_sfx.py Assets/_Game/Audio/Generated sfx_menu_`); music and the quake rumble reuse the game's cues.
    /// </summary>
    public static class MenuAudio
    {
        private const string CueDir = "Assets/_Game/Data/Audio";
        private const string ClipDir = "Assets/_Game/Audio/Generated";

        public sealed class Cues
        {
            public AudioCue Hover, Press, Thud, Boom, Type, RumbleLoop, Click, MusicPrep, MusicOutro, QuakeRumble;

            public AudioCue[] All => new[] { Hover, Press, Thud, Boom, Type, RumbleLoop, Click, MusicPrep, MusicOutro, QuakeRumble };
        }

        public static Cues Load()
        {
            return new Cues
            {
                Hover = Cue("cue_menu_hover", "sfx_menu_hover", 0.35f, 0.97f, 1.04f),
                Press = Cue("cue_menu_press", "sfx_menu_press", 0.8f),
                Thud = Cue("cue_menu_thud", "sfx_menu_thud", 0.45f, 0.95f, 1.05f),
                Boom = Cue("cue_menu_boom", "sfx_menu_boom", 1f),
                Type = Cue("cue_menu_type", "sfx_menu_type", 0.25f, 0.92f, 1.1f),
                RumbleLoop = Cue("cue_menu_rumble_loop", "sfx_menu_rumble_loop", 0.3f, loop: true),
                Click = AssetDatabase.LoadAssetAtPath<AudioCue>($"{CueDir}/cue_choice_click.asset"),
                MusicPrep = AssetDatabase.LoadAssetAtPath<AudioCue>($"{CueDir}/cue_music_prep.asset"),
                MusicOutro = AssetDatabase.LoadAssetAtPath<AudioCue>($"{CueDir}/cue_music_outro.asset"),
                QuakeRumble = AssetDatabase.LoadAssetAtPath<AudioCue>($"{CueDir}/cue_quake_rumble.asset")
            };
        }

        private static AudioCue Cue(string id, string clip, float volume, float pitchMin = 1f, float pitchMax = 1f, bool loop = false)
        {
            AudioCue cue = AssetUtil.LoadOrCreate<AudioCue>($"{CueDir}/{id}.asset");
            cue.Configure(id, volume, loop, pitchMin, pitchMax);
            AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>($"{ClipDir}/{clip}.wav");
            if (audio != null) cue.SetClips(audio);
            else Debug.LogWarning($"[Menu] {ClipDir}/{clip}.wav missing: run py tools/gen_sfx.py {ClipDir} sfx_menu_");
            EditorUtility.SetDirty(cue);
            return cue;
        }
    }
}
