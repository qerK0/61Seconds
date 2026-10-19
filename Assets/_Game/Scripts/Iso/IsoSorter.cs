using UnityEngine;
using UnityEngine.Rendering;

namespace SixtyOne.Iso
{
    /// <summary>How often IsoSorter recomputes.</summary>
    public enum IsoSortMode
    {
        /// <summary>Once on Start (props, walls).</summary>
        Static,
        /// <summary>Every LateUpdate (characters, moving things).</summary>
        Dynamic
    }

    /// <summary>
    /// Sets sortingOrder from world Y (lower Y = in front). Uses a SortingGroup on this object if
    /// present, otherwise the renderers on this object (and children if enabled).
    /// </summary>
    public sealed class IsoSorter : MonoBehaviour
    {
        [Tooltip("Static = compute once, Dynamic = every LateUpdate.")]
        [SerializeField] private IsoSortMode mode = IsoSortMode.Static;

        [Tooltip("Added to the computed order (fine-tune ties).")]
        [SerializeField] private int offset;

        [Tooltip("Orders per world unit of Y.")]
        [SerializeField] private float precision = 100f;

        [Tooltip("Optional feet point. Defaults to this transform.")]
        [SerializeField] private Transform pivot;

        [Tooltip("Also sort child renderers when there is no SortingGroup.")]
        [SerializeField] private bool includeChildren;

        private SortingGroup group;
        private Renderer[] renderers;

        /// <summary>Last applied sortingOrder.</summary>
        public int SortingOrder { get; private set; }

        public IsoSortMode Mode
        {
            get => mode;
            set => mode = value;
        }

        private void Awake()
        {
            Cache();
        }

        private void Start()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (mode == IsoSortMode.Dynamic) Apply();
        }

        private void OnValidate()
        {
            renderers = null;
            group = null;
        }

        /// <summary>Recomputes and applies the order now.</summary>
        public void Apply()
        {
            if (renderers == null && group == null) Cache();

            float y = pivot != null ? pivot.position.y : transform.position.y;
            SortingOrder = IsoMath.SortingOrderForY(y, precision, offset);

            if (group != null)
            {
                group.sortingOrder = SortingOrder;
                return;
            }

            foreach (Renderer r in renderers)
            {
                if (r != null) r.sortingOrder = SortingOrder;
            }
        }

        private void Cache()
        {
            group = GetComponent<SortingGroup>();
            renderers = includeChildren ? GetComponentsInChildren<Renderer>(true) : GetComponents<Renderer>();
        }
    }
}
