using System;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>One spoken line. Showing it costs building-timer seconds.</summary>
    [Serializable]
    public sealed class DialogueLine
    {
        [Tooltip("Speaker name.")]
        [SerializeField] private TextKey speakerKey;

        [Tooltip("The line.")]
        [SerializeField] private TextKey textKey;

        [Tooltip("Building-timer seconds this line costs.")]
        [Min(0f)]
        [SerializeField] private float timeCost = 3f;

        public TextKey SpeakerKey => speakerKey;
        public TextKey TextKey => textKey;
        public float TimeCost => timeCost;

        public DialogueLine()
        {
        }

        public DialogueLine(TextKey speaker, TextKey text, float seconds)
        {
            speakerKey = speaker;
            textKey = text;
            timeCost = seconds;
        }
    }
}
