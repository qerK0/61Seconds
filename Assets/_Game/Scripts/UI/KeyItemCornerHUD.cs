using System;
using System.Collections;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Corner HUD for key items (flashlight, whistle). They never use a bag slot; this shows one icon per key
    /// item: dim while missing, lit (with a short pop) once owned. Hovering a slot shows its tooltip.
    /// </summary>
    public sealed class KeyItemCornerHUD : MonoBehaviour
    {
        /// <summary>One corner slot.</summary>
        [Serializable]
        public sealed class Slot
        {
            [Tooltip("Key item shown here.")]
            public Item item;

            [Tooltip("Icon image.")]
            public Image icon;

            [Tooltip("Frame tinted by owned state. Optional.")]
            public Image frame;
        }

        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("One slot per key item.")]
        [SerializeField] private Slot[] slots = new Slot[0];

        [Tooltip("Tooltip label (item name + owned/missing). Optional.")]
        [SerializeField] private TMP_Text tooltip;

        [Tooltip("Icon tint while owned.")]
        [SerializeField] private Color ownedColor = Color.white;

        [Tooltip("Icon tint while missing.")]
        [SerializeField] private Color missingColor = new Color(1f, 1f, 1f, 0.18f);

        [Tooltip("Frame tint while owned.")]
        [SerializeField] private Color ownedFrameColor = new Color(1f, 0.82f, 0.2f, 0.9f);

        [Tooltip("Frame tint while missing.")]
        [SerializeField] private Color missingFrameColor = new Color(1f, 1f, 1f, 0.12f);

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        /// <summary>True if the corner HUD shows the key item as owned (pure; used by tests).</summary>
        public static bool IsShownAsOwned(GameState state, string itemId) => state != null && state.KeyItems.Has(itemId);

        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                Slot slot = slots[i];
                if (slot == null || slot.icon == null) continue;
                HoverRelay relay = slot.icon.gameObject.GetComponent<HoverRelay>();
                if (relay == null) relay = slot.icon.gameObject.AddComponent<HoverRelay>();
                relay.Entered = () => ShowTooltip(slot);
                relay.Exited = HideTooltip;
            }

            HideTooltip();
        }

        private void OnEnable()
        {
            InventoryEvents.OnInventoryChanged += Refresh;
            InventoryEvents.OnKeyItemAcquired += OnAcquired;
            GameEvents.Rewound += OnRewound;
            Refresh();
        }

        private void OnDisable()
        {
            InventoryEvents.OnInventoryChanged -= Refresh;
            InventoryEvents.OnKeyItemAcquired -= OnAcquired;
            GameEvents.Rewound -= OnRewound;
        }

        private void OnRewound(string _) => Refresh();

        public void Refresh()
        {
            GameManager gm = Gm;
            foreach (Slot slot in slots)
            {
                if (slot == null || slot.item == null) continue;
                bool owned = gm != null && IsShownAsOwned(gm.State, slot.item.Id);
                if (slot.icon != null)
                {
                    if (slot.item.Icon != null) slot.icon.sprite = slot.item.Icon;
                    slot.icon.color = owned ? ownedColor : missingColor;
                }

                if (slot.frame != null) slot.frame.color = owned ? ownedFrameColor : missingFrameColor;
            }
        }

        private void OnAcquired(string itemId)
        {
            Refresh();
            foreach (Slot slot in slots)
            {
                if (slot != null && slot.item != null && slot.item.Id == itemId && slot.icon != null)
                {
                    StartCoroutine(Pop(slot.icon.transform));
                    ShowTooltip(slot);
                }
            }
        }

        private void ShowTooltip(Slot slot)
        {
            if (tooltip == null || slot == null || slot.item == null) return;
            GameManager gm = Gm;
            bool owned = gm != null && IsShownAsOwned(gm.State, slot.item.Id);
            tooltip.text = TextDB.Format(owned ? UiKeys.KeyItemOwned : UiKeys.KeyItemMissing, slot.item.NameKey.Display);
            tooltip.gameObject.SetActive(true);
        }

        private void HideTooltip()
        {
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }

        private static IEnumerator Pop(Transform target)
        {
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            const float duration = 0.35f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                target.localScale = baseScale * (1f + 0.35f * k);
                yield return null;
            }

            target.localScale = baseScale;
        }
    }
}
