using SixtyOne.Prep;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>Buttons to walk between prep rooms (each walk costs countdown time). Visible during prep only.</summary>
    public sealed class RoomTabs : MonoBehaviour
    {
        [Tooltip("Prep driver (travel requests).")]
        [SerializeField] private PrepController prep;

        [Tooltip("Shown only during prep.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Room id per button (same order as buttons).")]
        [SerializeField] private string[] roomIds = new string[0];

        [Tooltip("One button per room.")]
        [SerializeField] private Button[] buttons = new Button[0];

        [Tooltip("Tint of the current room's button.")]
        [SerializeField] private Color currentColor = new Color(1f, 0.82f, 0.2f, 1f);

        [Tooltip("Tint of other buttons.")]
        [SerializeField] private Color otherColor = new Color(0.18f, 0.22f, 0.32f, 1f);

        [Tooltip("Room switcher (to know the current room).")]
        [SerializeField] private SixtyOne.Placement.RoomSwitcher rooms;

        private void Awake()
        {
            for (int i = 0; i < buttons.Length && i < roomIds.Length; i++)
            {
                string roomId = roomIds[i];
                if (buttons[i] != null) buttons[i].onClick.AddListener(() => { if (prep != null) prep.RequestTravel(roomId); });
            }
        }

        private void Update()
        {
            bool visible = prep != null && prep.IsPrepActive;
            UIVisibility.Set(group, visible);
            if (!visible) return;

            string current = rooms != null && rooms.Current != null ? rooms.Current.RoomId : null;
            for (int i = 0; i < buttons.Length && i < roomIds.Length; i++)
            {
                if (buttons[i] == null) continue;
                buttons[i].interactable = prep.CanInteract;
                if (buttons[i].targetGraphic != null) buttons[i].targetGraphic.color = roomIds[i] == current ? currentColor : otherColor;
            }
        }
    }
}
