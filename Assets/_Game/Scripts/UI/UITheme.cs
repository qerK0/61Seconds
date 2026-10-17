using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// One place for the HUD look: fonts (Inter Regular / SemiBold / Bold), sizes at the 1920x1080 reference,
    /// colours and spacing. The scene builder applies it to every UI element it creates; ThemedText applies it
    /// at runtime to text created elsewhere.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/UI Theme", fileName = "UITheme")]
    public sealed class UITheme : ScriptableObject
    {
        [Header("Fonts")]
        [Tooltip("Body text (dialogue, descriptions).")]
        [SerializeField] private TMP_FontAsset regular;

        [Tooltip("Speaker names, choice labels, titles.")]
        [SerializeField] private TMP_FontAsset semiBold;

        [Tooltip("Timer, big numbers, emphasis.")]
        [SerializeField] private TMP_FontAsset bold;

        [Tooltip("7-segment LED digits (E1234) for the building timer and prep countdown.")]
        [SerializeField] private TMP_FontAsset led;

        [Tooltip("EB Garamond Regular: cinematic subtitle lines and menu text.")]
        [SerializeField] private TMP_FontAsset serifRegular;

        [Tooltip("EB Garamond SemiBold: subtitle speaker names and the menu title.")]
        [SerializeField] private TMP_FontAsset serifSemiBold;

        [Header("Sizes (px at 1080p; nothing below 16)")]
        [SerializeField] private float timerSize = 64f;
        [SerializeField] private float titleSize = 30f;
        [SerializeField] private float speakerSize = 26f;
        [SerializeField] private float bodySize = 25f;
        [SerializeField] private float choiceSize = 23f;
        [SerializeField] private float labelSize = 16f;
        [SerializeField] private float smallSize = 18f;

        [Tooltip("Extra line spacing for body text (TMP units).")]
        [SerializeField] private float bodyLineSpacing = 12f;

        [Tooltip("Letter spacing for all-caps small labels (TMP units).")]
        [SerializeField] private float labelLetterSpacing = 8f;

        [Header("Colours")]
        [SerializeField] private Color text = new Color(0.96f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color mutedText = new Color(0.78f, 0.78f, 0.8f, 1f);
        [SerializeField] private Color accent = new Color(1f, 0.76f, 0.29f, 1f);
        [SerializeField] private Color danger = new Color(1f, 0.42f, 0.36f, 1f);
        [SerializeField] private Color positive = new Color(0.45f, 0.86f, 0.55f, 1f);
        [SerializeField] private Color panel = new Color(0.06f, 0.065f, 0.08f, 0.86f);
        [SerializeField] private Color button = new Color(0.12f, 0.13f, 0.16f, 0.88f);
        [SerializeField] private Color buttonHover = new Color(0.2f, 0.21f, 0.26f, 0.95f);
        [SerializeField] private Color buttonPressed = new Color(0.28f, 0.24f, 0.16f, 1f);
        [SerializeField] private Color buttonDisabled = new Color(0.1f, 0.1f, 0.12f, 0.7f);

        [Header("Spacing (px at 1080p)")]
        [SerializeField] private float padding = 20f;
        [SerializeField] private float gap = 10f;
        [SerializeField] private float choiceHeight = 56f;
        [SerializeField] private float accentBarWidth = 5f;

        [Header("Motion")]
        [Tooltip("Typewriter speed (characters per second).")]
        [SerializeField] private float typewriterCharsPerSecond = 90f;

        [Tooltip("Panel slide in/out seconds (unscaled).")]
        [SerializeField] private float slideSeconds = 0.22f;

        public TMP_FontAsset Regular => regular;
        public TMP_FontAsset SemiBold => semiBold != null ? semiBold : regular;
        public TMP_FontAsset Bold => bold != null ? bold : SemiBold;
        public TMP_FontAsset Led => led != null ? led : Bold;
        public TMP_FontAsset SerifRegular => serifRegular != null ? serifRegular : Regular;
        public TMP_FontAsset SerifSemiBold => serifSemiBold != null ? serifSemiBold : SerifRegular;
        public float TimerSize => timerSize;
        public float TitleSize => titleSize;
        public float SpeakerSize => speakerSize;
        public float BodySize => bodySize;
        public float ChoiceSize => choiceSize;
        public float LabelSize => labelSize;
        public float SmallSize => smallSize;
        public float BodyLineSpacing => bodyLineSpacing;
        public float LabelLetterSpacing => labelLetterSpacing;
        public Color Text => text;
        public Color MutedText => mutedText;
        public Color Accent => accent;
        public Color Danger => danger;
        public Color Positive => positive;
        public Color Panel => panel;
        public Color Button => button;
        public Color ButtonHover => buttonHover;
        public Color ButtonPressed => buttonPressed;
        public Color ButtonDisabled => buttonDisabled;
        public float Padding => padding;
        public float Gap => gap;
        public float ChoiceHeight => choiceHeight;
        public float AccentBarWidth => accentBarWidth;
        public float TypewriterCharsPerSecond => typewriterCharsPerSecond;
        public float SlideSeconds => slideSeconds;

        /// <summary>Authoring helper for the font generator.</summary>
        public void ConfigureFonts(TMP_FontAsset regularFont, TMP_FontAsset semiBoldFont, TMP_FontAsset boldFont)
        {
            regular = regularFont;
            semiBold = semiBoldFont;
            bold = boldFont;
        }

        /// <summary>Authoring helper for the font generator (LED digits + EB Garamond).</summary>
        public void ConfigureExtraFonts(TMP_FontAsset ledFont, TMP_FontAsset serifRegularFont, TMP_FontAsset serifSemiBoldFont)
        {
            led = ledFont;
            serifRegular = serifRegularFont;
            serifSemiBold = serifSemiBoldFont;
        }

        /// <summary>Applies a text role (font, size, colour, spacing) to a label.</summary>
        public void Apply(TMP_Text label, TextRole role)
        {
            if (label == null) return;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = 0f;
            label.lineSpacing = 0f;
            switch (role)
            {
                case TextRole.Timer:
                    Set(label, Bold, timerSize, text);
                    break;
                case TextRole.Title:
                    Set(label, Bold, titleSize, accent);
                    break;
                case TextRole.Speaker:
                    Set(label, SemiBold, speakerSize, accent);
                    break;
                case TextRole.Body:
                    Set(label, Regular, bodySize, text);
                    label.lineSpacing = bodyLineSpacing;
                    break;
                case TextRole.Choice:
                    Set(label, SemiBold, choiceSize, text);
                    break;
                case TextRole.Label:
                    Set(label, SemiBold, labelSize, mutedText);
                    label.fontStyle = FontStyles.UpperCase;
                    label.characterSpacing = labelLetterSpacing;
                    break;
                default:
                    Set(label, Regular, smallSize, text);
                    break;
            }
        }

        private static void Set(TMP_Text label, TMP_FontAsset font, float size, Color color)
        {
            if (font != null) label.font = font;
            label.fontSize = size;
            label.color = color;
        }
    }

    /// <summary>Text roles of the HUD (UITheme.Apply).</summary>
    public enum TextRole
    {
        Small,
        Timer,
        Title,
        Speaker,
        Body,
        Choice,
        /// <summary>All-caps, letter-spaced small label.</summary>
        Label
    }
}
