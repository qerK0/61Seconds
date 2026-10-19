using SixtyOne.Audio;
using SixtyOne.Cinematic;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builds the EndingCinematic overlay: full-screen black, the frame image (aspect kept = black letterbox) and the
    /// centred IBM Plex Mono fallback line. Frames come from EndingCinematicConfig (SixtyOne > Art > Import Ending Frames).
    /// </summary>
    public static class TestSceneEnding
    {
        /// <summary>Outro music (fallback audio) and cue_ending, set by the scene builder.</summary>
        public static AudioCue Outro { get; set; }
        public static AudioCue EndingCue { get; set; }

        /// <summary>The overlay built by the last Build (wired to the EpilogueController).</summary>
        public static EndingCinematic Last { get; private set; }

        public static EndingCinematic Build(Transform root, UITheme theme)
        {
            Image black = UiFactory.MakeImage("EndingCinematic", root, Vector2.zero, Vector2.one, Color.black, raycast: true);
            CanvasGroup group = black.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Image frame = UiFactory.MakeImage("Frame", black.transform, Vector2.zero, Vector2.one, Color.white);
            frame.preserveAspect = true;
            frame.enabled = false;
            TextMeshProUGUI text = UiFactory.MakeLabel("Fallback", black.transform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), 34f, TextAlignmentOptions.Center, new Color(0.9f, 0.92f, 0.88f, 1f));
            if (theme != null && theme.Mono != null) text.font = theme.Mono;
            text.alpha = 0f;

            EndingCinematic ending = black.gameObject.AddComponent<EndingCinematic>();
            SerializedWire.Set(ending, "config", EndingArt.Import(EndingCue));
            SerializedWire.Set(ending, "root", group);
            SerializedWire.Set(ending, "frame", frame);
            SerializedWire.Set(ending, "fallbackText", text);
            if (Outro != null) SerializedWire.Set(ending, "outroMusic", Outro);
            Last = ending;
            return ending;
        }
    }
}
