using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Items;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// A backpack side pocket dedicated to one key item (left = flashlight, right = whistle). Shows the icon when
    /// owned and a faint silhouette when not; never droppable. A click uses the item where it has a use
    /// (KeyItemPocketRules: the node's whistle choice, the flashlight toggle), otherwise shows its name.
    /// </summary>
    public sealed class KeyItemPocket : MonoBehaviour
    {
        [Tooltip("Key item kept in this pocket.")]
        [SerializeField] private Item item;

        [Tooltip("Item icon.")]
        [SerializeField] private Image icon;

        [Tooltip("Click target (the pocket area).")]
        [SerializeField] private Button button;

        [Tooltip("Current node (for item-use choices).")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Icon tint while owned.")]
        [SerializeField] private Color ownedColor = Color.white;

        [Tooltip("Silhouette tint while missing.")]
        [SerializeField] private Color missingColor = new Color(0f, 0f, 0f, 0.55f);

        [Tooltip("Icon tint while the flashlight is switched off.")]
        [SerializeField] private Color offColor = new Color(0.55f, 0.55f, 0.6f, 1f);

        [Tooltip("Click sound.")]
        [SerializeField] private AudioCue clickCue;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            InventoryEvents.OnKeyItemAcquired += OnKeyItem;
            InventoryEvents.OnInventoryChanged += Refresh;
            GameEvents.Rewound += OnRewound;
            GameEvents.FlagChanged += OnFlag;
            Refresh();
        }

        private void OnDisable()
        {
            InventoryEvents.OnKeyItemAcquired -= OnKeyItem;
            InventoryEvents.OnInventoryChanged -= Refresh;
            GameEvents.Rewound -= OnRewound;
            GameEvents.FlagChanged -= OnFlag;
        }

        private void OnKeyItem(string _) => Refresh();

        private void OnRewound(string _) => Refresh();

        private void OnFlag(string flag, bool _)
        {
            if (flag == FlagNames.FlashlightOff) Refresh();
        }

        private Vector2 authoredPosition;
        private Vector2 authoredSize;
        private bool authoredCached;

        private void Refresh()
        {
            if (icon == null || item == null) return;
            GameState state = GameManager.Instance != null ? GameManager.Instance.State : null;
            bool owned = state != null && StateOps.HasItem(state, item.Id);
            // The pocket icon has an authored spot in the backpack art: restore it, then the UI sprite.
            RectTransform rect = icon.rectTransform;
            if (!authoredCached)
            {
                authoredPosition = rect.anchoredPosition;
                authoredSize = rect.sizeDelta;
                authoredCached = true;
            }

            rect.anchoredPosition = authoredPosition;
            rect.sizeDelta = authoredSize;
            SlotIcon.SetSprite(icon, item);
            bool off = owned && item.Id == KeyItemPocketRules.FlashlightId && !KeyItemPocketRules.FlashlightOn(state);
            icon.color = !owned ? missingColor : off ? offColor : ownedColor;
        }

        private void OnClick()
        {
            GameManager gm = GameManager.Instance;
            if (item == null || gm == null || gm.IsDead || InputGate.IsBlocked) return;
            AudioService.PlayUiCue(clickCue);
            switch (KeyItemPocketRules.Decide(nodePlayer != null ? nodePlayer.Current : null, gm.State, item.Id, out int choice))
            {
                case PocketAction.UseChoice:
                    nodePlayer.Choose(choice);
                    break;
                case PocketAction.ToggleFlashlight:
                    bool nowOff = !gm.State.Flags.Get(FlagNames.FlashlightOff);
                    StateOps.SetFlag(gm.State, FlagNames.FlashlightOff, nowOff);
                    GameEvents.RaiseMessage(nowOff ? UiKeys.FlashlightOff : UiKeys.FlashlightOn);
                    Refresh();
                    break;
                default:
                    GameEvents.RaiseMessage(item.NameKey.Key);
                    break;
            }
        }
    }
}
