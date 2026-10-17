using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// The cracked variants of a room's wall segments. DebrisPresenter calls Apply on room enter and after each
    /// quake; CrackRules decides per segment (seeded, monotonic), so a wall that cracked stays cracked.
    /// </summary>
    public sealed class WallCracks : MonoBehaviour
    {
        /// <summary>One wall segment: its renderer, the plain look and the cracked look.</summary>
        [Serializable]
        public sealed class Segment
        {
            [Tooltip("Segment code + row, e.g. L3_r0 (the crack seed key).")]
            public string Key;

            public SpriteRenderer Renderer;
            public Sprite Plain;
            public bool PlainFlip;
            public Sprite Cracked;
            public bool CrackedFlip;
        }

        [Tooltip("Wall segments that can crack (windows and gaps are left out).")]
        [SerializeField] private List<Segment> segments = new List<Segment>();

        public IReadOnlyList<Segment> Segments => segments;

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Add(string key, SpriteRenderer renderer, Sprite plain, bool plainFlip, Sprite cracked, bool crackedFlip)
        {
            if (renderer == null || cracked == null) return;
            segments.Add(new Segment { Key = key, Renderer = renderer, Plain = plain, PlainFlip = plainFlip, Cracked = cracked, CrackedFlip = crackedFlip });
        }

        /// <summary>Shows each segment plain or cracked for the given crack chance.</summary>
        public void Apply(string roomId, int checkpointSeed, float chance)
        {
            foreach (Segment s in segments)
            {
                if (s == null || s.Renderer == null) continue;
                bool cracked = CrackRules.IsCracked(roomId, s.Key, checkpointSeed, chance);
                s.Renderer.sprite = cracked ? s.Cracked : s.Plain;
                s.Renderer.flipX = cracked ? s.CrackedFlip : s.PlainFlip;
            }
        }
    }
}
