using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// A tiny control-room readout: a text key formatted every tick. {0} = a counting clock (mm:ss, starting at
    /// clockStart), {1} / {2} = coordinates whose last digit jitters.
    /// </summary>
    public sealed class HudReadout : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Format key (ui.menu.*).")]
        [SerializeField] private string key;

        [Tooltip("Seconds between updates.")]
        [SerializeField] private float tick = 0.5f;

        [Tooltip("Clock start (seconds) for {0}.")]
        [SerializeField] private float clockStart = 61f;

        [Tooltip("Base coordinates for {1} and {2}.")]
        [SerializeField] private Vector2 coordinates = new Vector2(41.0082f, 28.9784f);

        private float next;

        public void Configure(TMP_Text target, string formatKey, float seconds, float clock)
        {
            label = target;
            key = formatKey;
            tick = seconds;
            clockStart = clock;
        }

        private void Update()
        {
            if (label == null || Time.unscaledTime < next) return;
            next = Time.unscaledTime + tick;
            int clock = Mathf.FloorToInt(clockStart + Time.unscaledTime);
            string time = $"{clock / 60:00}:{clock % 60:00}";
            string lat = (coordinates.x + Random.Range(-4, 5) * 0.0001f).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
            string lon = (coordinates.y + Random.Range(-4, 5) * 0.0001f).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);
            label.text = TextDB.Format(key, time, lat, lon);
        }
    }
}
