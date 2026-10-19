using UnityEngine;

namespace SixtyOne.Text
{
    /// <summary>Loads TextDB from its settings before anything else wakes up.</summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class TextDBLoader : MonoBehaviour
    {
        [Tooltip("Settings asset holding the CSV reference.")]
        [SerializeField] private TextDBSettings settings;

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogWarning("[TextDB] TextDBLoader has no settings; falling back to editor lazy load.", this);
                TextDB.EnsureLoaded();
                return;
            }

            TextDB.Load(settings.Csv);
        }
    }
}
