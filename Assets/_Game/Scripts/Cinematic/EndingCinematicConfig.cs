using SixtyOne.Audio;
using UnityEngine;
using UnityEngine.Video;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// The ending cinematic: a video (Art/anim/ending.mp4, played with its own sound) or, without one, the frames
    /// (Art/anim/ENDING), their frame rate and optional audio; both are filled by SixtyOne > Art > Import Ending Frames.
    /// No video and no frames = the text fallback. The scene only references this asset, so the clip is in the build.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Ending Cinematic Config")]
    public sealed class EndingCinematicConfig : ScriptableObject
    {
        [Tooltip("Ending video; wins over the frames and plays with its own audio track.")]
        [SerializeField] private VideoClip video;

        [Tooltip("Frames in order (frame0000, frame0001 ...).")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Playback frames per second.")]
        [SerializeField] private float fps = 12f;

        [Tooltip("Played with the frames (cue_ending); without clips the outro music plays instead.")]
        [SerializeField] private AudioCue cue;

        public VideoClip Video => video;
        public Sprite[] Frames => frames;
        public float Fps => Mathf.Max(1f, fps);
        public AudioCue Cue => cue;

        public void SetVideo(VideoClip clip) => video = clip;
        public void SetFrames(Sprite[] sprites) => frames = sprites ?? new Sprite[0];
        public void SetCue(AudioCue audio) => cue = audio;
    }
}
