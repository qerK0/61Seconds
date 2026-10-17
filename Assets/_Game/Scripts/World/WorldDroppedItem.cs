using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>An item the player dropped in a room. Clicking it puts it back in the bag (if a slot is free).</summary>
    public sealed class WorldDroppedItem : MonoBehaviour, IClickable, IApproachable
    {
        [Tooltip("Dropped item id.")]
        [SerializeField] private string itemId;

        [Tooltip("Room the item lies in.")]
        [SerializeField] private string roomId;

        [Tooltip("Building-timer seconds to pick it up again.")]
        [Min(0f)]
        [SerializeField] private float pickUpSeconds = 1f;

        public string ItemId => itemId;
        public string RoomId => roomId;

        /// <summary>The player runs here before using it.</summary>
        public Vector3 ApproachPoint => transform.position;

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked;
            }
        }

        /// <summary>Set by DroppedItemsPresenter when spawned.</summary>
        public void Bind(string item, string room, float seconds)
        {
            itemId = item;
            roomId = room;
            pickUpSeconds = seconds;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable) return;
            if (gm.State.Inventory.IsFull)
            {
                GameEvents.RaiseMessage(UiKeys.BagFull);
                return;
            }

            ActionCostRouter.SpendTime(pickUpSeconds);
            if (gm.IsDead) return;
            if (StateOps.PickUpDropped(gm.State, itemId, roomId)) GameEvents.RaiseMessage(UiKeys.PickedUpAgain);
        }
    }
}
