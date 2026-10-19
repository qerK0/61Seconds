using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Full-screen darkening from gas poisoning or hunger/thirst exhaustion, driven by max(poison, exhaustion)
    /// (on top of the cinematic vignette/desaturation). Gas keeps its greenish tint and dizzy wobble; exhaustion is
    /// neutral black with a slow heartbeat-like double pulse. Reduce Effects keeps the fade and drops the pulse.
    /// Never fully black: capped at maxAlpha until death. Never blocks clicks.
    /// </summary>
    public sealed class PoisonOverlay : MonoBehaviour
    {
        [Tooltip("Full-screen image (tinted at runtime).")]
        [SerializeField] private Image overlay;

        [Tooltip("Alpha at poison/exhaustion 1.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float maxAlpha = 0.7f;

        [Tooltip("Dizziness pulse amplitude (alpha).")]
        [SerializeField] private float pulse = 0.06f;

        [Tooltip("Tint while gas poisoning dominates.")]
        [SerializeField] private Color gasTint = new Color(0.02f, 0.05f, 0.02f, 1f);

        [Tooltip("Tint while exhaustion dominates.")]
        [SerializeField] private Color exhaustionTint = new Color(0f, 0f, 0f, 1f);

        [Tooltip("Heartbeat pulse amplitude (alpha) for exhaustion.")]
        [SerializeField] private float heartbeatPulse = 0.1f;

        [Tooltip("Heartbeats per second at full exhaustion (slower when milder).")]
        [SerializeField] private float heartbeatRate = 1.1f;

        private float beatPhase;

        private void Awake()
        {
            if (overlay != null) overlay.raycastTarget = false;
        }

        private void Update()
        {
            if (overlay == null) return;
            GameManager gm = GameManager.Instance;
            float poison = gm != null ? gm.State.Poison : 0f;
            float exhaustion = gm != null ? gm.State.Exhaustion : 0f;
            bool reduce = gm != null && gm.Settings != null && gm.Settings.ReduceEffects;
            float level = Mathf.Max(poison, exhaustion);
            bool exhausted = exhaustion > poison;

            float wobble = 0f;
            if (!reduce && level > 0.05f)
            {
                if (exhausted)
                {
                    beatPhase += Time.unscaledDeltaTime * Mathf.Lerp(0.6f, heartbeatRate, level);
                    wobble = HeartbeatShape(beatPhase) * heartbeatPulse * level;
                }
                else
                {
                    wobble = Mathf.Sin(Time.unscaledTime * 2.2f) * pulse * level;
                }
            }

            Color c = exhausted ? exhaustionTint : gasTint;
            c.a = Mathf.Clamp(level * maxAlpha + wobble, 0f, maxAlpha + heartbeatPulse);
            overlay.color = c;
            overlay.enabled = c.a > 0.001f;
        }

        /// <summary>Lub-dub: two short bumps per beat (0..1), then rest.</summary>
        private static float HeartbeatShape(float phase)
        {
            float t = phase - Mathf.Floor(phase);
            float lub = Mathf.Exp(-Mathf.Pow((t - 0.08f) / 0.05f, 2f));
            float dub = 0.7f * Mathf.Exp(-Mathf.Pow((t - 0.28f) / 0.05f, 2f));
            return lub + dub;
        }
    }
}
