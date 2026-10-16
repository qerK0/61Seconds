using UnityEngine;
using UnityEngine.Rendering;

namespace SixtyOne.Iso
{
    /// <summary>Misc isometric helpers.</summary>
    public static class IsoUtil
    {
        /// <summary>Effective draw order of a component: SortingGroup, else Renderer, else IsoSorter.</summary>
        public static int GetSortingOrder(Component component)
        {
            SortingGroup group = component.GetComponentInParent<SortingGroup>();
            if (group != null) return group.sortingOrder;

            Renderer renderer = component.GetComponent<Renderer>();
            if (renderer == null) renderer = component.GetComponentInParent<Renderer>();
            if (renderer != null) return renderer.sortingOrder;

            IsoSorter sorter = component.GetComponentInParent<IsoSorter>();
            return sorter != null ? sorter.SortingOrder : 0;
        }
    }
}
