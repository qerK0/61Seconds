using SixtyOne.Core;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// A 7-segment LED clock face: lit MM:SS digits with a soft glow (material instance) over an unlit "88:88"
    /// ghost. The colon blinks once per second while the clock runs and stays solid while timers are paused.
    /// The owning display decides the time, colour and whether it flickers.
    /// </summary>
    public sealed class LedClock : MonoBehaviour
    {
        [Tooltip("Lit digits (LED font).")]
        [SerializeField] private TMP_Text digits;

        [Tooltip("Unlit '88:88' behind the digits (same font, low alpha).")]
        [SerializeField] private TMP_Text ghost;

        [Tooltip("Ghost alpha relative to the current colour.")]
        [Range(0f, 1f)]
        [SerializeField] private float ghostAlpha = 0.12f;

        [Tooltip("Glow strength (TMP _GlowOuter).")]
        [Range(0f, 1f)]
        [SerializeField] private float glowOuter = 0.35f;

        private Material glowMaterial;
        private Color lastGlow = Color.clear;

        /// <summary>The colour last shown (for labels that match it).</summary>
        public Color Current { get; private set; }

        private void Awake()
        {
            if (digits == null) return;
            // Instance so the glow colour never edits the shared font material.
            glowMaterial = digits.fontMaterial;
            glowMaterial.EnableKeyword(ShaderUtilities.Keyword_Glow);
            glowMaterial.SetFloat(ShaderUtilities.ID_GlowOuter, glowOuter);
            glowMaterial.SetFloat(ShaderUtilities.ID_GlowInner, 0.05f);
            glowMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.75f);
            glowMaterial.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
            digits.UpdateMeshPadding();
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
        }

        /// <summary>Shows a time. running = the countdown is actually advancing (colon blinks).</summary>
        public void Show(float remaining, Color color, bool running, bool flicker)
        {
            if (digits == null) return;
            int total = Mathf.Max(0, Mathf.CeilToInt(remaining));
            bool colonOn = !running || Mathf.Repeat(remaining, 1f) >= 0.5f;
            string colon = colonOn ? ":" : "<alpha=#00>:<alpha=#FF>";
            digits.text = $"{total / 60:00}{colon}{total % 60:00}";

            float brightness = flicker ? Flicker() : 1f;
            Color lit = new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
            digits.color = lit;
            Current = color;
            if (ghost != null) ghost.color = new Color(color.r, color.g, color.b, ghostAlpha);

            if (glowMaterial != null && lit != lastGlow)
            {
                lastGlow = lit;
                glowMaterial.SetColor(ShaderUtilities.ID_GlowColor, new Color(lit.r, lit.g, lit.b, 0.55f));
            }
        }

        /// <summary>Mostly steady, with short random dips (a failing LED).</summary>
        private static float Flicker()
        {
            float t = Time.unscaledTime;
            float n = Mathf.PerlinNoise(t * 9f, 0.37f);
            return n < 0.22f ? 0.55f : n < 0.3f ? 0.8f : 1f;
        }

        /// <summary>Reduce Effects (GameSettings) is on.</summary>
        public static bool ReduceEffects => GameManager.Instance != null && GameManager.Instance.Settings != null && GameManager.Instance.Settings.ReduceEffects;

        /// <summary>True while a countdown driven by NavigationEvents.TimerDelta is advancing.</summary>
        public static bool TimersRunning => !NavigationEvents.TimersPaused && Time.timeScale > 0f;
    }
}
