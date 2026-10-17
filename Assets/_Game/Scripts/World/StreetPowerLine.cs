using SixtyOne.Core;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// The sparking downed power line across the street: clicking it electrocutes the player (tip.electric). Its
    /// cells are blocked on the street grid; the spark sprites at the cut end flicker. Walking into the spark cells
    /// is checked by StreetQuakeDirector.
    /// </summary>
    public sealed class StreetPowerLine : MonoBehaviour, IClickable
    {
        [Tooltip("Death line.")]
        [SerializeField] private string deathKey = "death.street_power";

        [Tooltip("Tip unlocked by the death.")]
        [SerializeField] private string tipId = "tip.electric";

        [Tooltip("Spark sprites at the cut end (flicker).")]
        [SerializeField] private SpriteRenderer[] sparks = new SpriteRenderer[0];

        private Vector3[] sparkScales = new Vector3[0];

        public string DeathKey => deathKey;
        public string TipId => tipId;

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && !InputGate.IsBlocked;
            }
        }

        private void Awake()
        {
            sparkScales = new Vector3[sparks.Length];
            for (int i = 0; i < sparks.Length; i++) if (sparks[i] != null) sparkScales[i] = sparks[i].transform.localScale;
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(string death, string tip, SpriteRenderer[] sparkRenderers)
        {
            deathKey = death;
            tipId = tip;
            sparks = sparkRenderers ?? new SpriteRenderer[0];
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable) return;
            gm.Die(Death());
        }

        /// <summary>The electrocution death (macro rewind to the street checkpoint).</summary>
        public DeathInfo Death()
        {
            return new DeathInfo { Victim = MemberType.Player, TextKey = deathKey, TipId = tipId, RewindTarget = RewindTarget.Checkpoint };
        }

        private void Update()
        {
            for (int i = 0; i < sparks.Length; i++)
            {
                if (sparks[i] == null) continue;
                float flicker = Random.value;
                Color c = sparks[i].color;
                c.a = flicker > 0.35f ? Mathf.Lerp(0.5f, 1f, flicker) : 0.1f;
                sparks[i].color = c;
                sparks[i].transform.localScale = sparkScales[i] * Mathf.Lerp(0.7f, 1.25f, Random.value);
            }
        }
    }
}
