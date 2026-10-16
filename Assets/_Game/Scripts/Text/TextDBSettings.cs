using UnityEngine;

namespace SixtyOne.Text
{
    /// <summary>Points TextDB at the CSV TextAsset (Assets/_Game/Content/Text/strings_tr.csv).</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Text DB Settings", fileName = "TextDBSettings")]
    public sealed class TextDBSettings : ScriptableObject
    {
        [Tooltip("strings_tr.csv imported as a TextAsset.")]
        [SerializeField] private TextAsset csv;

        public TextAsset Csv => csv;

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(TextAsset csvAsset)
        {
            csv = csvAsset;
        }
    }
}
