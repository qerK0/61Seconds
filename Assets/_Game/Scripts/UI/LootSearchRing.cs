using SixtyOne.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Radial progress ring (+ "Aranıyor…") over the container being searched. Listens to LootEvents.</summary>
    public sealed class LootSearchRing : MonoBehaviour
    {
        [Tooltip("Camera that renders the world (screen position of the container).")]
        [SerializeField] private Camera worldCamera;

        [Tooltip("Root moved to the container (never blocks clicks).")]
        [SerializeField] private RectTransform root;

        [Tooltip("Filled image (Radial 360).")]
        [SerializeField] private Image fill;

        [Tooltip("'Aranıyor…' label. Optional.")]
        [SerializeField] private TMP_Text label;

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            Show(false);
        }

        private void OnEnable()
        {
            LootEvents.OnSearchProgress += OnProgress;
            LootEvents.OnSearchEnded += OnEnded;
        }

        private void OnDisable()
        {
            LootEvents.OnSearchProgress -= OnProgress;
            LootEvents.OnSearchEnded -= OnEnded;
        }

        private void OnProgress(Vector3 world, float progress)
        {
            Show(true);
            if (fill != null) fill.fillAmount = progress;
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null || root == null || canvas == null) return;
            Vector2 screen = cam.WorldToScreenPoint(world);
            RectTransform canvasRect = canvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local)) root.anchoredPosition = local;
        }

        private void OnEnded() => Show(false);

        private void Show(bool on)
        {
            if (root != null) root.gameObject.SetActive(on);
        }
    }
}
