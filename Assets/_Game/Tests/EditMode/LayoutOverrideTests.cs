using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Editor;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEngine;
using PlacedObject = SixtyOne.Editor.RoomLayoutOverrides.PlacedObject;

namespace SixtyOne.Tests
{
    /// <summary>Layout Editor overrides: apply (move / remove / add), the blocked-cell check, and save diffs.</summary>
    public class LayoutOverrideTests
    {
        private RoomLayoutOverrides overrides;

        [SetUp]
        public void SetUp() => overrides = ScriptableObject.CreateInstance<RoomLayoutOverrides>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(overrides);

        private static BuildingLayout.RoomSpec Room(List<BuildingLayout.RoomSpec> rooms, string id) => rooms.Find(r => r.Id == id);

        [Test]
        public void PropIdsAreUniquePerRoom()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                var ids = new HashSet<string>();
                foreach (BuildingLayout.PropSpec p in room.Props) Assert.IsTrue(ids.Add(p.Id), $"{room.Id}: duplicate prop id {p.Id}");
            }
        }

        [Test]
        public void ApplyMovesRemovesAndAdds()
        {
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            BuildingLayout.RoomSpec salon = Room(rooms, BuildingLevelsGenerator.Flat11Salon);
            BuildingLayout.PropSpec chair = salon.Props.Find(p => p.Name == "Chair");
            BuildingLayout.PropSpec table = salon.Props.Find(p => p.Name == "Table");
            overrides.SetRoom(salon.Id, new List<PlacedObject>
            {
                new PlacedObject { id = chair.Id, kind = LayoutKind.Prop, propName = "Chair", gx = 4.5f, gy = 1.5f, flipX = true },
                new PlacedObject { id = table.Id, kind = LayoutKind.Prop, removed = true },
                new PlacedObject { id = "food_x", kind = LayoutKind.Pickup, itemId = "food", gx = 3f, gy = 3f, borrowed = true },
                new PlacedObject { id = "drawer_x", kind = LayoutKind.Container, containerType = "drawer", gx = 1.5f, gy = 4.5f, contents = new List<string> { "water", "food" } }
            });

            LayoutOverrides.Apply(rooms, overrides);

            Assert.AreEqual(4.5f, chair.Gx);
            Assert.IsTrue(chair.FlipX);
            Assert.IsNull(salon.Props.Find(p => p.Id == table.Id));
            BuildingLayout.ItemSpec food = salon.Items.Find(i => i.Id == "food_x");
            Assert.IsNotNull(food);
            Assert.IsTrue(food.Borrowed);
            BuildingLayout.ContainerSpec drawer = salon.Containers.Find(c => c.Id == "drawer_x");
            Assert.AreEqual(new[] { "water", "food" }, drawer.Items);
            Assert.AreEqual("container.drawer", drawer.NameKey);
        }

        [Test]
        public void FurnitureOnADoorApproachIsCarvedOut()
        {
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            BuildingLayout.RoomSpec salon = Room(rooms, BuildingLevelsGenerator.Flat11Salon);
            Vector2 door = BuildingLayout.EdgeInside(salon, salon.Doors[0].Edge);
            Vector2Int cell = GridNav.CellOf(door);
            overrides.SetRoom(salon.Id, new List<PlacedObject>
            {
                new PlacedObject { id = "wardrobe_block", kind = LayoutKind.Prop, propName = "Wardrobe", gx = cell.x + 0.2f, gy = cell.y + 0.2f, blocksNav = true }
            });

            LayoutOverrides.Apply(rooms, overrides);

            CollectionAssert.IsEmpty(LayoutOverrides.Check(salon));
            CollectionAssert.DoesNotContain(salon.Blocked, cell);
        }

        [Test]
        public void CodeLayoutHasNoProblems()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms()) CollectionAssert.IsEmpty(LayoutOverrides.Check(room), room.Id);
        }

        [Test]
        public void UnchangedRoomDiffsToNothing()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                var placed = new List<PlacedObject>(LayoutOverrides.CodeObjects(room));
                CollectionAssert.IsEmpty(LayoutOverrides.Diff(room, placed), room.Id);
            }
        }

        [Test]
        public void DiffKeepsMovesAddsAndRemovals()
        {
            BuildingLayout.RoomSpec room = Room(BuildingLayout.Rooms(), BuildingLevelsGenerator.Flat11Salon);
            var placed = new List<PlacedObject>(LayoutOverrides.CodeObjects(room));
            PlacedObject moved = placed.Find(o => o.kind == LayoutKind.Prop);
            moved.gx += 1f;
            PlacedObject removed = placed.Find(o => o.kind == LayoutKind.Container);
            placed.Remove(removed);
            placed.Add(new PlacedObject { id = "chair_new", kind = LayoutKind.Prop, propName = "Chair", gx = 1f, gy = 1f });

            List<PlacedObject> diff = LayoutOverrides.Diff(room, placed);

            Assert.AreEqual(3, diff.Count);
            Assert.IsTrue(diff.Exists(o => o.id == moved.id && !o.removed));
            Assert.IsTrue(diff.Exists(o => o.id == removed.id && o.removed));
            Assert.IsTrue(diff.Exists(o => o.id == "chair_new"));
        }
    }
}
