using SixtyOne.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Full-screen darkening from gas poisoning (on top of the cinematic vignette/desaturation). Never fully
    /// black: capped at maxAlpha until death. Pulses slightly to read as dizziness. Never blocks clicks.
    /// </summary>
    public sealed class PoisonOverlay : MonoBehaviour
    {
        [Tooltip("Full-screen image (black/greenish).")]
        [SerializeField] private Image overlay;

        [Tooltip("Alpha at poison 1.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float maxAlpha = 0.7f;

        [Tooltip("Dizziness pulse amplitude (alpha).")]
        [SerializeField] private float pulse = 0.06f;

        private void Awake()
        {
            if (overlay != null) overlay.raycastTarget = false;
        }

        private void Update()
        {
            if (overlay == null) return;
            GameManager gm = GameManager.Instance;
            float poison = gm != null ? gm.State.Poison : 0f;
            float wobble = poison > 0.05f ? Mathf.Sin(Time.unscaledTime * 2.2f) * pulse * poison : 0f;
            Color c = overlay.color;
            c.a = Mathf.Clamp(poison * maxAlpha + wobble, 0f, maxAlpha);
            overlay.color = c;
            overlay.enabled = c.a > 0.001f;
        }
    }
}
