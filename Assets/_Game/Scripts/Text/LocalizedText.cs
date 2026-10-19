using TMPro;
using UnityEngine;

namespace SixtyOne.Text
{
    /// <summary>Shows a TextDB key on a TMP_Text and refreshes on hot reload.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("Key to display.")]
        [SerializeField] private TextKey key;

        private TMP_Text label;
        private object[] args;

        public TextKey Key => key;

        /// <summary>Changes the key (and optional string.Format args) and refreshes.</summary>
        public void SetKey(TextKey newKey, params object[] formatArgs)
        {
            key = newKey;
            args = formatArgs;
            Refresh();
        }

        private void OnEnable()
        {
            TextDB.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            TextDB.Changed -= Refresh;
        }

        /// <summary>Re-reads the text.</summary>
        public void Refresh()
        {
            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }

            if (label == null || key.IsEmpty)
            {
                return;
            }

            label.text = args != null && args.Length > 0 ? TextDB.Format(key.Key, args) : key.Display;
        }
    }
}
