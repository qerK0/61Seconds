using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// An item hidden in the room art: an invisible collider the player has to find. Collecting it hides an
    /// optional painted overlay (the item drawn into the background). An optional sparkle appears when the
    /// GameSettings toggle is on and either this spot (or its item) is in the KnowledgeSet (found in an earlier
    /// loop; knowledge survives rewinds) or the room has been open for HiddenSparkleAfterSeconds.
    /// </summary>
    public sealed class HiddenItemHotspot : WorldInteractable
    {
        [Tooltip("Item painted into the room art; hidden once collected. Optional.")]
        [SerializeField] private GameObject paintedOverlay;

        [Tooltip("Sparkle hint shown by the rules above. Optional.")]
        [SerializeField] private SpriteRenderer sparkle;

        [Tooltip("Sparkle pulse speed (cycles per second, unscaled time).")]
        [Min(0.1f)]
        [SerializeField] private float sparkleSpeed = 1.2f;

        private float secondsInRoom;
        private Vector3 sparkleScale = Vector3.one;

        /// <summary>Authoring helper for scene builders (call after Configure).</summary>
        public void ConfigureHidden(GameObject overlay, SpriteRenderer sparkleRenderer)
        {
            paintedOverlay = overlay;
            sparkle = sparkleRenderer;
        }

        protected override void Reset()
        {
            base.Reset();
            ApplyPreset(2f, isOneShot: true, hide: true);
        }

        protected override void Awake()
        {
            base.Awake();
            if (sparkle != null) sparkleScale = sparkle.transform.localScale;
        }

        protected override void OnEnable()
        {
            secondsInRoom = 0f;
            base.OnEnable();
        }

        /// <summary>KnowledgeSet entry recorded when this spot is found (persists across rewinds).</summary>
        public string KnowledgeKey => "hidden:" + Id;

        public override void Refresh()
        {
            base.Refresh();
            bool used = IsUsed;
            GameManager gm = GameManager.Instance;
            if (used && gm != null) gm.State.Knowledge.Add(KnowledgeKey);
            if (paintedOverlay != null) paintedOverlay.SetActive(!used);
            if (sparkle != null) sparkle.enabled = !used && !IsGone && ShouldSparkle();
        }

        protected override void Update()
        {
            base.Update();
            secondsInRoom += Time.unscaledDeltaTime;
            if (sparkle == null) return;

            bool show = !IsUsed && !IsGone && ShouldSparkle();
            if (sparkle.enabled != show) sparkle.enabled = show;
            if (!show) return;

            float k = (Mathf.Sin(Time.unscaledTime * sparkleSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            sparkle.transform.localScale = sparkleScale * Mathf.Lerp(0.6f, 1.1f, k);
            Color c = sparkle.color;
            c.a = Mathf.Lerp(0.25f, 0.9f, k);
            sparkle.color = c;
        }

        private bool ShouldSparkle()
        {
            GameManager gm = GameManager.Instance;
            GameSettings settings = gm != null ? gm.Settings : null;
            if (settings == null || !settings.HiddenItemSparkle) return false;
            return SparkleRule(IsKnownHiddenItem(gm), secondsInRoom, settings.HiddenSparkleAfterSeconds);
        }

        private bool IsKnownHiddenItem(GameManager gm)
        {
            if (gm == null) return false;
            return gm.State.Knowledge.Contains(KnowledgeKey) || (GrantsItem != null && gm.State.Knowledge.Contains(GrantsItem.Id));
        }

        /// <summary>Pure sparkle rule (setting already on): known from an earlier loop, or searched long enough.</summary>
        public static bool SparkleRule(bool known, float secondsInRoom, float afterSeconds)
        {
            return known || (afterSeconds > 0f && secondsInRoom >= afterSeconds);
        }
    }
}
