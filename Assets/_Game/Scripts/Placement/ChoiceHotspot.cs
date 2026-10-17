using System.Collections.Generic;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// A person or object in the room that plays the current node's choice with this label key (talk to the
    /// neighbour, the trader, step into the elevator...). The player runs to it first; the choice leaves the panel.
    /// With hideWhenUnavailable it disappears once its choice is gone (the neighbour left).
    /// </summary>
    public sealed class ChoiceHotspot : RoomLink
    {
        private const float RefreshInterval = 0.2f;

        [Tooltip("Label key of the node choice this hotspot plays, e.g. f4.corridor.c_talk.")]
        [SerializeField] private TextKey choiceKey;

        [Tooltip("Hide renderers and collider while the current node has no such choice (people who left).")]
        [SerializeField] private bool hideWhenUnavailable;

        [Tooltip("More choice label keys this hotspot plays in other nodes (the same gas valve before and after the quake).")]
        [SerializeField] private string[] alsoKeys = new string[0];

        private Renderer[] renderers;
        private Collider2D[] colliders;
        private float nextRefresh;
        private bool shown = true;

        public string ChoiceKey => choiceKey.Key;

        /// <summary>Every choice label key this hotspot can play (main key first).</summary>
        public IEnumerable<string> ChoiceKeys
        {
            get
            {
                yield return choiceKey.Key;
                if (alsoKeys == null) yield break;
                foreach (string key in alsoKeys) if (!string.IsNullOrEmpty(key)) yield return key;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
        }

        private void LateUpdate()
        {
            if (!hideWhenUnavailable || Navigator == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshInterval;
            // Only re-evaluate in exploration; during the dialogue the person stays where they are.
            if (!Navigator.CanUseLinks) return;
            bool show = Navigator.Decide(this).Action != LinkAction.Unavailable;
            if (show == shown) return;
            shown = show;
            foreach (Renderer r in renderers) if (r != null) r.enabled = show;
            foreach (Collider2D c in colliders) if (c != null) c.enabled = show;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void ConfigureHotspot(TextKey key, bool hideWhenGone, string[] extraKeys = null)
        {
            choiceKey = key;
            hideWhenUnavailable = hideWhenGone;
            alsoKeys = extraKeys ?? new string[0];
        }
    }
}
