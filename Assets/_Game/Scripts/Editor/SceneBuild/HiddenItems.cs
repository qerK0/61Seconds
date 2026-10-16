using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>Builds hidden-item hotspots (filled in with the hidden item milestone).</summary>
    public static class HiddenItems
    {
        public static void Build(Transform room, BuildingLayout.ItemSpec spec, Item item)
        {
            SceneKit.Interactable(room, spec.Id, item, spec.Gx, spec.Gy, spec.Cost, spec.Surface, spec.Borrowed, spec.Color);
        }
    }
}
