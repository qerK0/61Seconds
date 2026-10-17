using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Doors / hotspots mapped onto node choices (RoomLinkRules), the floor clamp and the InRoom condition.</summary>
    public class NavigationTests
    {
        private TestData data;
        private GameState state;
        private SceneNode corridor;
        private SceneNode hall;
        private SceneNode flat12;
        private Item wrench;

        // Rooms: hall + corridor are their own zones; flat12 + flat12_living share the flat12 zone.
        private static string ZoneOf(string room) => room == "flat12_living" ? "flat12" : room;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            state = new GameState();
            wrench = data.Item("wrench");
            corridor = Node("corridor", "f4_corridor");
            hall = Node("hall", "flat11_hall");
            flat12 = Node("flat12", "flat12");

            corridor.AddChoice(new Choice("corridor.c_home").Move(3f).Then(Outcome.GoTo(hall)));
            corridor.AddChoice(new Choice("corridor.c_flat12").Move(2f).ShowWhen(Condition.FlagTrue("KnowsFlat12")).Then(Outcome.GoTo(flat12)));
            corridor.AddChoice(new Choice("corridor.c_talk"));
            hall.AddChoice(new Choice("hall.c_exit").Move(3f)
                .Require(Condition.HasItem(wrench))
                .WhenLocked(FailBehaviour.ShowLocked, "hall.c_exit.locked")
                .Then(Outcome.GoTo(corridor)));
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        private SceneNode Node(string id, string room)
        {
            SceneNode node = data.Make<SceneNode>();
            node.Configure(id, id + ".title", id + ".body", room, 1);
            return node;
        }

        [Test]
        public void DoorPlaysTheMoveChoiceIntoItsZone()
        {
            LinkDecision d = RoomLinkRules.DecideDoor(corridor, state, "f4_corridor", "flat11_hall", ZoneOf);
            Assert.AreEqual(LinkAction.Choose, d.Action);
            Assert.AreEqual(0, d.ChoiceIndex);
        }

        [Test]
        public void DoorIntoAnyRoomOfTheZoneMatchesTheZoneEntryNode()
        {
            state.Flags.Set("KnowsFlat12", true);
            LinkDecision d = RoomLinkRules.DecideDoor(corridor, state, "f4_corridor", "flat12_living", ZoneOf);
            Assert.AreEqual(LinkAction.Choose, d.Action);
            Assert.AreEqual(1, d.ChoiceIndex);
        }

        [Test]
        public void HiddenMoveMakesTheDoorUnavailable()
        {
            LinkDecision d = RoomLinkRules.DecideDoor(corridor, state, "f4_corridor", "flat12", ZoneOf);
            Assert.AreEqual(LinkAction.Unavailable, d.Action);
        }

        [Test]
        public void LockedMoveShowsItsReason()
        {
            LinkDecision d = RoomLinkRules.DecideDoor(hall, state, "flat11_hall", "f4_corridor", ZoneOf);
            Assert.AreEqual(LinkAction.Locked, d.Action);
            Assert.AreEqual("hall.c_exit.locked", d.ReasonKey);

            state.Inventory.TryAdd("wrench");
            Assert.AreEqual(LinkAction.Choose, RoomLinkRules.DecideDoor(hall, state, "flat11_hall", "f4_corridor", ZoneOf).Action);
        }

        [Test]
        public void DoorInsideTheSameFlatIsALocalSwitch()
        {
            LinkDecision d = RoomLinkRules.DecideDoor(flat12, state, "flat12", "flat12_living", ZoneOf);
            Assert.AreEqual(LinkAction.LocalSwitch, d.Action);
        }

        [Test]
        public void HotspotPlaysTheChoiceWithItsLabelKey()
        {
            Assert.AreEqual(LinkAction.Choose, RoomLinkRules.DecideHotspot(corridor, state, "corridor.c_talk").Action);
            Assert.AreEqual(LinkAction.Unavailable, RoomLinkRules.DecideHotspot(corridor, state, "corridor.c_missing").Action);
        }

        [Test]
        public void DoorsAndHotspotsTakeTheirChoicesOutOfThePanel()
        {
            Choice home = corridor.Choices[0];
            Choice talk = corridor.Choices[2];
            Assert.IsTrue(RoomLinkRules.Handles(home, "f4_corridor", "flat11_hall", null, ZoneOf));
            Assert.IsFalse(RoomLinkRules.Handles(talk, "f4_corridor", "flat11_hall", null, ZoneOf));
            Assert.IsTrue(RoomLinkRules.Handles(talk, "f4_corridor", null, "corridor.c_talk", ZoneOf));
            // A door to the zone you are already in never takes over a move.
            Assert.IsFalse(RoomLinkRules.Handles(home, "flat11_hall", "flat11_hall", null, ZoneOf));
        }

        [Test]
        public void InRoomConditionReadsTheShownRoom()
        {
            state.CurrentRoomId = "flat9";
            Assert.IsTrue(ConditionEvaluator.Evaluate(Condition.InRoom("flat9"), state));
            state.CurrentRoomId = "flat9_kitchen";
            Assert.IsFalse(ConditionEvaluator.Evaluate(Condition.InRoom("flat9"), state));
        }

        [Test]
        public void FloorClampKeepsClicksInsideTheDiamond()
        {
            var floor = new FloorArea(4, 4);
            Vector2 inside = floor.ClampGrid(new Vector2(2f, 2f), 0.25f);
            Assert.AreEqual(new Vector2(2f, 2f), inside);

            Vector2 outside = floor.ClampGrid(new Vector2(9f, -3f), 0.25f);
            Assert.AreEqual(3.75f, outside.x, 1e-4f);
            Assert.AreEqual(0.25f, outside.y, 1e-4f);

            Vector3 world = floor.ClampLocal(IsoMath.GridToWorld(-5f, 2f), 0.25f);
            Vector2 back = IsoMath.WorldToGrid(world);
            Assert.AreEqual(0.25f, back.x, 1e-3f);
            Assert.AreEqual(2f, back.y, 1e-3f);
        }

        [Test]
        public void FloorClampReachesExtraCells()
        {
            var floor = new FloorArea(4, 4, new List<Vector2Int> { new Vector2Int(4, 0) });
            Vector2 balcony = floor.ClampGrid(new Vector2(4.6f, 0.5f), 0.2f);
            Assert.AreEqual(4.6f, balcony.x, 1e-4f);
            Assert.IsTrue(floor.ContainsGrid(balcony));
            Assert.IsFalse(floor.ContainsGrid(new Vector2(4.5f, 3f)));
        }
    }
}
