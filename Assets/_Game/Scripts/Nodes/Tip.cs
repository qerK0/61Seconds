using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>A real earthquake safety tip ("Deprem Notu") collected across loops.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Tip", fileName = "tip_new")]
    public sealed class Tip : ScriptableObject
    {
        [Tooltip("Unique id stored in collected tips.")]
        [SerializeField] private string id;

        [Tooltip("Short title.")]
        [SerializeField] private TextKey titleKey;

        [Tooltip("The tip itself. Must match AFAD guidance.")]
        [SerializeField] private TextKey textKey;

        [Tooltip("Optional icon.")]
        [SerializeField] private Sprite icon;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public TextKey TitleKey => titleKey;
        public TextKey TextKey => textKey;
        public Sprite Icon => icon;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string tipId, TextKey title, TextKey text)
        {
            id = tipId;
            titleKey = title;
            textKey = text;
        }
    }
}
