using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Editor;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Solid props block their base cells; every room of the saved layout stays fully connected.</summary>
    public class PropCollisionTests
    {
        [Test]
        public void FixturesAreNotSolidFurnitureIs()
        {
            foreach (string name in new[] { "WallLamp", "Valve", "FuseBox", "CeilingLamp", "Sign", "Window" }) Assert.IsFalse(PropFootprint.IsSolid(name), name);
            foreach (string name in new[] { "Wardrobe", "Bed", "Table", "Chair", "Stove", "Bookshelf", "FloorHole" }) Assert.IsTrue(PropFootprint.IsSolid(name), name);
        }

        [Test]
        public void BottomPivotFootprintSitsBehindThePivot()
        {
            BuildingLayout.RoomSpec room = BuildingLayout.Rooms().Find(r => r.Id == BuildingLevelsGenerator.Flat11Bedroom);
            List<Vector2Int> cells = PropFootprint.Cells(room, 1.0f, 1.0f, 0.6f, false, out Vector2Int centre);
            Assert.AreEqual(new Vector2Int(1, 1), centre);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 1) }, cells);
            // A wide base covers the cells behind its front corner, never the ones in front of it.
            cells = PropFootprint.Cells(room, 1.0f, 1.0f, 1.9f, false, out _);
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(2, 2) }, cells);
        }

        [Test]
        public void SavedLayoutRoomsAreFullyConnected()
        {
            foreach (BuildingLayout.RoomSpec room in LayoutOverrides.Rooms())
            {
                CollectionAssert.IsEmpty(LayoutOverrides.Check(room), room.Id);
                GridNav nav = BuildingLayout.NavFor(room);
                Vector2Int spawn = GridNav.CellOf(room.PlayerSpawn);
                Assert.IsTrue(nav.IsWalkable(spawn), $"{room.Id}: the player spawn is inside a prop");
                HashSet<Vector2Int> reach = nav.ReachableFrom(spawn);
                foreach (Vector2Int c in nav.Cells)
                {
                    if (nav.IsWalkable(c)) Assert.IsTrue(reach.Contains(c), $"{room.Id}: walkable cell {c} is cut off from the spawn");
                }

                foreach (Vector2Int c in PropFootprint.HardCells(room))
                {
                    Assert.IsTrue(nav.IsWalkable(c), $"{room.Id}: required cell {c} is inside a prop");
                }

                foreach (BuildingLayout.ContainerSpec container in room.Containers)
                {
                    Vector2Int at = GridNav.CellOf(BuildingLayout.ContainerApproach(room, container));
                    Assert.IsTrue(nav.IsWalkable(at) && reach.Contains(at), $"{room.Id}: container {container.Id} has no free approach cell");
                }
            }
        }
    }
}
