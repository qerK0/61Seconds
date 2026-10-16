using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builds hidden-item hotspots: an invisible collider, a faint "painted" overlay standing in for the item
    /// drawn into the room art (hidden once collected) and a sparkle hint (off until the sparkle rule allows it).
    /// No world label, so the player has to look.
    /// </summary>
    public static class HiddenItems
    {
        public static HiddenItemHotspot Build(Transform room, BuildingLayout.ItemSpec spec, Item item)
        {
            var go = new GameObject("Hidden_" + spec.Id);
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(spec.Gx, spec.Gy);
            go.AddComponent<CircleCollider2D>().radius = 0.26f;

            var overlay = new GameObject("PaintedOverlay");
            overlay.transform.SetParent(go.transform, false);
            overlay.transform.localScale = new Vector3(0.5f, 0.35f, 1f);
            SpriteRenderer paint = overlay.AddComponent<SpriteRenderer>();
            paint.sprite = SceneKit.Art.Block;
            Color faint = spec.Color * 0.6f;
            faint.a = 0.55f;
            paint.color = faint;

            var sparkleGo = new GameObject("Sparkle");
            sparkleGo.transform.SetParent(go.transform, false);
            sparkleGo.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            sparkleGo.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sparkle = sparkleGo.AddComponent<SpriteRenderer>();
            sparkle.sprite = SceneKit.Art.Dot;
            sparkle.color = new Color(1f, 0.97f, 0.7f, 0.8f);
            sparkle.enabled = false;

            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            HiddenItemHotspot hotspot = go.AddComponent<HiddenItemHotspot>();
            hotspot.Configure(spec.Id, item.NameKey, spec.Cost, item, null, default(TextKey), true, true, null, null, spec.Borrowed);
            hotspot.ConfigureHidden(overlay, sparkle);
            EditorUtility.SetDirty(hotspot);
            return hotspot;
        }
    }
}
