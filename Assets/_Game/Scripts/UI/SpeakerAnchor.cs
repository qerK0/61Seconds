using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>
    /// Marks a world character that can speak (NPC hotspot, Mehmet): its speaker text key, accent colour and head
    /// point (top of its visible sprites). Speech bubbles find the active anchor by key.
    /// </summary>
    public sealed class SpeakerAnchor : MonoBehaviour
    {
        /// <summary>Mehmet (the player): thoughts and decisions go on him.</summary>
        public const string PlayerKey = "npc.mehmet.name";

        [Tooltip("Speaker name text key (npc.sevim.name, npc.mehmet.name, ...).")]
        [SerializeField] private string speakerKey;

        [Tooltip("Name-tab / hover colour.")]
        [SerializeField] private Color accent = new Color(0.2f, 0.2f, 0.2f, 1f);

        [Tooltip("Head gap above the sprite top (world units at scale 1).")]
        [SerializeField] private float headGap = 0.05f;

        private static readonly List<SpeakerAnchor> Active = new List<SpeakerAnchor>();
        private SpriteRenderer[] renderers = new SpriteRenderer[0];

        public string SpeakerKey => speakerKey;
        public Color Accent => accent;

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        /// <summary>World point just above the head (top of the enabled sprites; the transform when none).</summary>
        public Vector3 HeadWorld
        {
            get
            {
                float top = float.NegativeInfinity;
                foreach (SpriteRenderer r in renderers)
                {
                    if (r == null || !r.enabled || r.sprite == null) continue;
                    if (r.gameObject.name == "Shadow" || IsUnderHover(r.transform)) continue;
                    top = Mathf.Max(top, r.bounds.max.y);
                }

                Vector3 p = transform.position;
                float scale = transform.lossyScale.y;
                return new Vector3(p.x, (float.IsNegativeInfinity(top) ? p.y : top) + headGap * scale, p.z);
            }
        }

        /// <summary>The hover arrow / label floats above the head: not part of the body.</summary>
        private bool IsUnderHover(Transform t)
        {
            for (; t != null && t != transform; t = t.parent)
            {
                if (t.name == "Hover") return true;
            }

            return false;
        }

        /// <summary>The active anchor for a speaker key (null when that speaker is not in the shown rooms).</summary>
        public static SpeakerAnchor Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (SpeakerAnchor anchor in Active)
            {
                if (anchor != null && anchor.speakerKey == key && anchor.isActiveAndEnabled) return anchor;
            }

            return null;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(string key, Color color)
        {
            speakerKey = key;
            accent = color;
        }
    }
}
