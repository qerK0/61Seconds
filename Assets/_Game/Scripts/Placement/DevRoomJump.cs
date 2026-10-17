using SixtyOne.Core;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace SixtyOne.Placement
{
    /// <summary>
    /// Dev-only room jump (Editor and Development builds; compiled out of release builds): F9 opens a list of every
    /// room; picking one shows it at once and snaps the player and family there, keeping the current state and node.
    /// For checking hand-placed layouts in game.
    /// </summary>
    public sealed class DevRoomJump : MonoBehaviour
    {
        [Tooltip("Room switcher (all rooms).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Game manager (state).")]
        [SerializeField] private GameManager gameManager;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool open;
        private Vector2 scroll;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f9Key.wasPressedThisFrame) open = !open;
        }

        private void OnGUI()
        {
            if (!open || rooms == null) return;
            GUILayout.BeginArea(new Rect(10f, 60f, 260f, Mathf.Min(Screen.height - 80f, 640f)), GUI.skin.box);
            GUILayout.Label("Room jump (F9)");
            scroll = GUILayout.BeginScrollView(scroll);
            string current = gameManager != null ? gameManager.State.CurrentRoomId : null;
            foreach (Room room in rooms.Rooms)
            {
                if (room == null) continue;
                string label = room.RoomId == current ? "▶ " + room.RoomId : room.RoomId;
                if (GUILayout.Button(label))
                {
                    rooms.JumpTo(room.RoomId);
                    open = false;
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
#endif
    }
}
