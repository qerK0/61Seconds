using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Editor;
using SixtyOne.Nodes;
using SixtyOne.World;
using UnityEditor;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>
    /// Descent rule: apart from the F4→F3 stairs and the F2 stairs to the street (same floor folder), every way down
    /// needs the bedsheet rope and the child harness, and is never silently allowed. Walks the generated node
    /// assets (run SixtyOne > Generate Building Levels first) and the BuildingLayout doors.
    /// </summary>
    public class DescentRuleTests
    {
        /// <summary>Choices allowed to go down a floor without gear (node id + label key): the normal stairs.</summary>
        private static readonly HashSet<string> Stairs = new HashSet<string> { BuildingLevelsGenerator.F4CorridorNode + ".c_down" };

        /// <summary>Door links allowed to go down a floor (from zone → to zone): the F4→F3 stairs.</summary>
        private static readonly HashSet<string> StairDoors = new HashSet<string> { BuildingLevelsGenerator.F4Corridor + ">" + BuildingLevelsGenerator.F3Corridor };

        private static Dictionary<SceneNode, int> LoadNodeFloors()
        {
            var floors = new Dictionary<SceneNode, int>();
            foreach (string guid in AssetDatabase.FindAssets("t:SceneNode", new[] { BuildingLevelsGenerator.NodesDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var node = AssetDatabase.LoadAssetAtPath<SceneNode>(path);
                if (node == null) continue;
                if (path.Contains("/Floor4/")) floors[node] = 4;
                else if (path.Contains("/Floor3/")) floors[node] = 3;
                else if (path.Contains("/Floor2/")) floors[node] = 2;
            }

            return floors;
        }

        private static bool Requires(Choice choice, string itemId)
        {
            foreach (Condition condition in choice.Conditions)
            {
                if (condition != null && condition.Type == ConditionType.HasItem && condition.Item != null && condition.Item.Id == itemId) return true;
            }

            return false;
        }

        [Test]
        public void EveryNonStairDescentNeedsRopeAndHarness()
        {
            Dictionary<SceneNode, int> floors = LoadNodeFloors();
            Assert.Greater(floors.Count, 0, "No node assets: run SixtyOne > Generate Building Levels.");

            int descents = 0;
            foreach (KeyValuePair<SceneNode, int> entry in floors)
            {
                foreach (Choice choice in entry.Key.Choices)
                {
                    if (choice == null) continue;
                    foreach (Outcome outcome in choice.Outcomes)
                    {
                        if (outcome == null || outcome.Type != OutcomeType.GoToNode || outcome.TargetNode == null) continue;
                        if (!floors.TryGetValue(outcome.TargetNode, out int targetFloor) || targetFloor >= entry.Value) continue;
                        if (Stairs.Contains(choice.LabelKey.Key)) continue;

                        descents++;
                        string where = $"{entry.Key.Id} / {choice.LabelKey.Key}";
                        Assert.IsTrue(Requires(choice, BuildingLevelsGenerator.DescentRopeId), where + " goes down without requiring the bedsheet rope.");
                        Assert.IsTrue(Requires(choice, BuildingLevelsGenerator.DescentFamilyItemId), where + " goes down without requiring the child harness.");
                        Assert.That(choice.FailBehaviour, Is.EqualTo(FailBehaviour.ShowLockedFatal).Or.EqualTo(FailBehaviour.Hide),
                            where + " must be ShowLockedFatal or hidden without the gear.");
                    }
                }
            }

            Assert.GreaterOrEqual(descents, 2, "Expected the Flat 9 cables and the Flat 10 collapsed floor as descents.");
        }

        [Test]
        public void DoorsNeverSkipAFloorExceptTheStairs()
        {
            Dictionary<SceneNode, int> floors = LoadNodeFloors();
            Assert.Greater(floors.Count, 0, "No node assets: run SixtyOne > Generate Building Levels.");

            // Zone → floor from the nodes' rooms (a node's room is its zone's entry room).
            var zoneFloor = new Dictionary<string, int>();
            foreach (KeyValuePair<SceneNode, int> entry in floors)
            {
                if (!string.IsNullOrEmpty(entry.Key.RoomId)) zoneFloor[entry.Key.RoomId] = entry.Value;
            }

            var zoneOf = new Dictionary<string, string>();
            List<BuildingLayout.RoomSpec> rooms = BuildingLayout.Rooms();
            foreach (BuildingLayout.RoomSpec room in rooms) zoneOf[room.Id] = string.IsNullOrEmpty(room.Zone) ? room.Id : room.Zone;

            foreach (BuildingLayout.RoomSpec room in rooms)
            {
                foreach (BuildingLayout.DoorSpec door in room.Doors)
                {
                    string from = zoneOf[room.Id];
                    Assert.IsTrue(zoneOf.TryGetValue(door.TargetRoom, out string to), $"{room.Id}: door to unknown room {door.TargetRoom}.");
                    if (!zoneFloor.TryGetValue(from, out int a) || !zoneFloor.TryGetValue(to, out int b) || b >= a) continue;
                    Assert.IsTrue(StairDoors.Contains(from + ">" + to), $"Door {room.Id} → {door.TargetRoom} skips a floor outside the stairs.");
                }
            }
        }

        [Test]
        public void BalconyRopeNeedsRopeAndHarness()
        {
            var rope = ScriptableObject.CreateInstance<Item>();
            rope.name = BuildingLevelsGenerator.DescentRopeId;
            var harness = ScriptableObject.CreateInstance<Item>();
            harness.name = BuildingLevelsGenerator.DescentFamilyItemId;
            try
            {
                var state = new GameState();
                state.Stamina = 100f;
                Assert.IsFalse(RopeDescentInteractable.CanDescend(state, rope, harness), "No gear.");
                StateOps.TryAddItem(state, rope.Id, false);
                Assert.IsFalse(RopeDescentInteractable.CanDescend(state, rope, harness), "Rope without the harness.");
                StateOps.TryAddItem(state, harness.Id, false);
                Assert.IsTrue(RopeDescentInteractable.CanDescend(state, rope, harness), "Rope and harness.");
            }
            finally
            {
                Object.DestroyImmediate(rope);
                Object.DestroyImmediate(harness);
            }
        }
    }
}
