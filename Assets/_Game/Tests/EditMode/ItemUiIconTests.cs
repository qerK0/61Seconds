using NUnit.Framework;
using SixtyOne.Editor;
using SixtyOne.Nodes;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Every item has a UI icon that centres in a slot: square, centre pivot (placeholder when no art).</summary>
    public class ItemUiIconTests
    {
        [Test]
        public void EveryItemHasASquareCentredUiIcon()
        {
            foreach (string id in BuildingLevelsGenerator.ItemIds)
            {
                var item = AssetDatabase.LoadAssetAtPath<Item>($"{BuildingLevelsGenerator.DataRoot}/Items/item.{id}.asset");
                if (item == null) Assert.Ignore($"item.{id} not generated (run SixtyOne > Generate Building Levels).");
                Sprite icon = item.UiIcon;
                Assert.IsNotNull(icon, id);
                Assert.AreEqual(icon.rect.width, icon.rect.height, 0.5f, $"{id}: square");
                Vector2 pivot = icon.pivot / icon.rect.size;
                Assert.AreEqual(0.5f, pivot.x, 0.01f, $"{id}: centre pivot");
                Assert.AreEqual(0.5f, pivot.y, 0.01f, $"{id}: centre pivot");
            }
        }
    }
}
