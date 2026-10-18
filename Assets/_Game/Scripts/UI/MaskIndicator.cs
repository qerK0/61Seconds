using SixtyOne.Building;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Small "maske takılı" chip (the worn wet-cloth icon + label) shown while the player carries the wet cloth in a
    /// room with leaking gas, so it is clear why the poison builds up slower. Purely visual; no new mechanic.
    /// </summary>
    public sealed class MaskIndicator : MonoBehaviour
    {
        [Tooltip("Reads whether the mask is worn.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Fades the chip in and out (never blocks clicks).")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Fade speed (alpha per second, unscaled).")]
        [SerializeField] private float fadeSpeed = 4f;

        private void Awake()
        {
            if (group == null) return;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void Update()
        {
            if (group == null) return;
            float target = survival != null && survival.MaskWorn ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
