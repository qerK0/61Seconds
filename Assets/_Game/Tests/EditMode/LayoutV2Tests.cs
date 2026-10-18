using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SixtyOne.Editor;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Layout Editor v2: scaled nav footprints, door moves, stairwell doors and the cut items.</summary>
    public sealed class LayoutV2Tests
    {
        [Test]
        public void ScaleOneBlocksOnlyItsOwnCell()
        {
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(2, 2) }, BuildingLayout.Footprint(2.5f, 2.5f, 1f).ToArray());
        }

        [Test]
        public void ScaleTwoBlocksTwoByTwo()
        {
            Assert.AreEqual(4, BuildingLayout.Footprint(2.5f, 2.5f, 2f).Count());
            Assert.AreEqual(4, BuildingLayout.Footprint(2.0f, 2.0f, 2f).Count());
        }

        [Test]
        public void MovingATwoWayDoorMovesTheArrivalOnTheOtherSide()
        {
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            BuildingLayout.RoomSpec corridor = rooms.Find(r => r.Id == BuildingLevelsGenerator.F4Corridor);
            BuildingLayout.DoorSpec door = corridor.Doors.Find(d => d.TargetRoom == BuildingLevelsGenerator.Flat12);
            BuildingLayout.MoveDoor(rooms, corridor, door, "L5");

            BuildingLayout.RoomSpec flat12 = rooms.Find(r => r.Id == BuildingLevelsGenerator.Flat12);
            BuildingLayout.DoorSpec back = flat12.Doors.Find(d => d.TargetRoom == corridor.Id);
            Assert.AreEqual(BuildingLayout.EdgeInside(corridor, "L5"), back.Arrival);
            Assert.Contains("L5", corridor.Gaps);
            Assert.IsFalse(corridor.Gaps.Contains("L4"));
        }

        [Test]
        public void DoorOverrideRoundTripsAndOldEntriesLoadAsScaleOne()
        {
            var o = new RoomLayoutOverrides.PlacedObject { objectScale = 0f };
            Assert.AreEqual(1f, o.Scale1);

            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            var asset = ScriptableObject.CreateInstance<RoomLayoutOverrides>();
            asset.SetRoom(BuildingLevelsGenerator.F4Corridor, new List<RoomLayoutOverrides.PlacedObject>
            {
                new RoomLayoutOverrides.PlacedObject { id = "door_flat12", kind = LayoutKind.Door, doorEdge = "L5", doorArt = "Door" }
            });
            LayoutOverrides.Apply(rooms, asset);
            Object.DestroyImmediate(asset);
            Assert.AreEqual("L5", rooms.Find(r => r.Id == BuildingLevelsGenerator.F4Corridor).Doors.Find(d => d.Id == "door_flat12").Edge);
        }

        [Test]
        public void EveryWayDownIsAStairwellDoorOnAWall()
        {
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            foreach (BuildingLayout.RoomSpec room in rooms)
            {
                Assert.IsFalse(room.Props.Exists(p => p.Name == "StairsDown"), room.Id);
                foreach (BuildingLayout.DoorSpec d in room.Doors) Assert.IsTrue(BuildingLayout.IsWallEdge(d.Edge), room.Id + " " + d.Edge);
            }

            Assert.AreEqual(BuildingLayout.DoorArtKind.Stairwell, rooms.Find(r => r.Id == BuildingLevelsGenerator.F4Corridor).Doors.Find(d => d.TargetRoom == BuildingLevelsGenerator.F3Corridor).Art);
            Assert.IsTrue(rooms.Find(r => r.Id == BuildingLevelsGenerator.F2Stairs).Hotspots.Exists(h => h.Art == BuildingLayout.StairwellArt && h.Edge != null));
        }

        [Test]
        public void CutItemsAreGone()
        {
            foreach (string id in new[] { "gold", "laptop", "console" }) CollectionAssert.DoesNotContain(BuildingLevelsGenerator.ItemIds, id);
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                Assert.IsFalse(room.Containers.Exists(c => c.NameKey == "container.medicine" || c.NameKey == "container.jewelry"), room.Id);
            }

            BuildingLayout.RoomSpec kitchen = BuildingLayout.Rooms().Find(r => r.Id == BuildingLevelsGenerator.Flat11Kitchen);
            Assert.IsTrue(kitchen.Containers.Exists(c => c.NameKey == "container.kitchen_cabinet" && c.Items.Contains("first_aid")));
        }
    }
}
