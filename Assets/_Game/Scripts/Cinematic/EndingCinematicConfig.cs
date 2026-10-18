using SixtyOne.Audio;
using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// The ending cinematic's frames (Art/anim/ENDING, filled by SixtyOne > Art > Import Ending Frames), its frame
    /// rate and its optional audio. Empty frames = the text fallback; the scene only references this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Ending Cinematic Config")]
    public sealed class EndingCinematicConfig : ScriptableObject
    {
        [Tooltip("Frames in order (frame0000, frame0001 ...).")]
        [SerializeField] private Sprite[] frames = new Sprite[0];

        [Tooltip("Playback frames per second.")]
        [SerializeField] private float fps = 12f;

        [Tooltip("Played with the frames (cue_ending); without clips the outro music plays instead.")]
        [SerializeField] private AudioCue cue;

        public Sprite[] Frames => frames;
        public float Fps => Mathf.Max(1f, fps);
        public AudioCue Cue => cue;

        public void SetFrames(Sprite[] sprites) => frames = sprites ?? new Sprite[0];
        public void SetCue(AudioCue audio) => cue = audio;
    }
}
