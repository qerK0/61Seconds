using SixtyOne.Text;
using UnityEngine.InputSystem;

namespace SixtyOne.Tutorial
{
    /// <summary>
    /// Names of the controls the game really reads, taken from the Input System so the tutorial hints match the
    /// player's device and keyboard layout: the pointer press that walks and clicks (IsoPicker), the keys that
    /// advance a hint (Space / Enter, like the speech bubbles) and the Craft Defteri key (C).
    /// </summary>
    public static class TutorialBindings
    {
        /// <summary>The walk / use control of the current pointer: "Sol tık" on a mouse, "Dokun" on a touch screen.</summary>
        public static string PointerPress()
        {
            Pointer pointer = Pointer.current;
            if (pointer is Touchscreen) return TextDB.Get(UiKeys.TutorialKeyTap);
            if (pointer == null || pointer is Mouse) return TextDB.Get(UiKeys.TutorialKeyClick);
            return pointer.press.displayName;
        }

        /// <summary>A keyboard key as the current layout names it (falls back to the key's enum name).</summary>
        public static string KeyName(Key key)
        {
            Keyboard keyboard = Keyboard.current;
            string name = keyboard != null ? keyboard[key].displayName : null;
            return string.IsNullOrEmpty(name) ? key.ToString() : name;
        }

        /// <summary>The Craft Defteri key (CraftNotebook toggles on C).</summary>
        public static string CraftKey() => KeyName(Key.C);

        /// <summary>"Sol tık / Space" style list of the controls that continue a click-through hint.</summary>
        public static string ContinueKeys() => PointerPress() + " / " + KeyName(Key.Space);

        /// <summary>True on the frame a continue control was pressed (pointer press, Space or Enter).</summary>
        public static bool ContinuePressed()
        {
            Keyboard k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) return true;
            Pointer p = Pointer.current;
            return p != null && p.press.wasPressedThisFrame;
        }
    }
}
