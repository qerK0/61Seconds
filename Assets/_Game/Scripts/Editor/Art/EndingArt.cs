using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SixtyOne.Audio;
using SixtyOne.Cinematic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace SixtyOne.Editor
{
    /// <summary>
    /// SixtyOne > Art > Import Ending Frames. Puts Art/anim/ending.mp4 (if present; imported without transcoding) into
    /// the config as the ending video, and imports every PNG in Art/anim/ENDING (frame0000.png ... any count) as an
    /// uncompressed full-rect sprite, sorts them numerically and writes them into Data/Cinematic/EndingCinematicConfig
    /// (the scene only references that asset, so nothing else is needed). An empty folder leaves the text fallback.
    /// </summary>
    public static class EndingArt
    {
        public const string FramesDir = "Assets/_Game/Art/anim/ENDING";
        public const string ConfigPath = "Assets/_Game/Data/Cinematic/EndingCinematicConfig.asset";
        public const string VideoPath = "Assets/_Game/Art/anim/ending.mp4";

        [MenuItem("SixtyOne/Art/Import Ending Frames", priority = 33)]
        public static void ImportMenu()
        {
            EndingCinematicConfig config = Import();
            Debug.Log($"[Ending] Video: {(config.Video != null ? config.Video.name + $" ({config.Video.width}x{config.Video.height}, {config.Video.length:0.0} s)" : "none")}. {config.Frames.Length} frame(s) at {config.Fps} fps in {ConfigPath}." + (config.Frames.Length == 0 ? " Fallback text plays until frames are dropped into " + FramesDir + "." : ""));
        }

        /// <summary>The config asset (created on first use) with the frames currently in the folder.</summary>
        public static EndingCinematicConfig Import(AudioCue cue = null)
        {
            AssetUtil.EnsureFolder("Assets/_Game/Data/Cinematic");
            AssetUtil.EnsureFolder(FramesDir);
            EndingCinematicConfig config = AssetUtil.LoadOrCreate<EndingCinematicConfig>(ConfigPath);
            var frames = new List<Sprite>();
            if (Directory.Exists(FramesDir))
            {
                var paths = new List<string>(Directory.GetFiles(FramesDir, "*.png"));
                paths.Sort((a, b) => Number(a).CompareTo(Number(b)) != 0 ? Number(a).CompareTo(Number(b)) : string.CompareOrdinal(a, b));
                foreach (string file in paths)
                {
                    string path = file.Replace(Path.DirectorySeparatorChar, '/');
                    if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                    {
                        bool changed = importer.textureType != TextureImporterType.Sprite || importer.textureCompression != TextureImporterCompression.Uncompressed
                            || importer.mipmapEnabled || importer.maxTextureSize < 4096;
                        if (changed)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.mipmapEnabled = false;
                            importer.maxTextureSize = 4096;
                            importer.SaveAndReimport();
                        }
                    }

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null) frames.Add(sprite);
                }
            }

            config.SetVideo(ImportVideo());
            config.SetFrames(frames.ToArray());
            if (cue != null) config.SetCue(cue);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        /// <summary>The ending video, imported as-is (H.264 + AAC play natively, so no transcode), or null.</summary>
        private static VideoClip ImportVideo()
        {
            if (AssetImporter.GetAtPath(VideoPath) is VideoClipImporter importer)
            {
                VideoImporterTargetSettings settings = importer.defaultTargetSettings;
                if (settings.enableTranscoding)
                {
                    settings.enableTranscoding = false;
                    importer.defaultTargetSettings = settings;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        }

        private static long Number(string path)
        {
            Match m = Regex.Match(Path.GetFileNameWithoutExtension(path), @"(\d+)(?!.*\d)");
            return m.Success && long.TryParse(m.Groups[1].Value, out long n) ? n : long.MaxValue;
        }
    }
}
