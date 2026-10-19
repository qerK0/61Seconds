using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Placement;
using SixtyOne.Text;

namespace SixtyOne.Tests
{
    public class PlacementResolverTests
    {
        private TestData data;
        private PlacementLayout layout;
        private Hazard wardrobe;
        private Hazard window;
        private Hazard openRoom;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAll();
            data = new TestData();
            wardrobe = data.Hazard("wardrobe", HazardEffect.Death, HazardEffect.Death, HazardEffect.Death);
            window = data.Hazard("window", HazardEffect.Death, HazardEffect.Injured, HazardEffect.Trapped, ActMask.Act2 | ActMask.Act3);
            openRoom = data.Hazard("open_room", HazardEffect.Death, HazardEffect.Injured, HazardEffect.Trapped);

            // table(2) - wall - doorway(no hazard here, for adjacency) ; table - wardrobe ; window isolated
            layout = new PlacementLayout { UnplacedHazard = openRoom }
                .Add(new AnchorInfo("table", AnchorTag.Safe, 2, null, new[] { "wall", "wardrobe" }))
                .Add(new AnchorInfo("wall", AnchorTag.Safe, 1, null, new[] { "table" }))
                .Add(new AnchorInfo("wardrobe", AnchorTag.Trap, 1, wardrobe))
                .Add(new AnchorInfo("window", AnchorTag.Trap, 1, window))
                .Add(new AnchorInfo("corner", AnchorTag.Safe, 1));
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        private static Dictionary<MemberType, string> Place(string player, string wife, string child)
        {
            return new Dictionary<MemberType, string>
            {
                { MemberType.Player, player },
                { MemberType.Wife, wife },
                { MemberType.Child, child }
            };
        }

        [Test]
        public void SafePlacementHasNoFatalResult()
        {
            var results = PlacementResolver.Resolve(layout, Place("wall", "table", "table"), 1);
            Assert.AreEqual(3, results.Count);
            Assert.IsNull(PlacementResolver.FirstFatal(results));
            Assert.IsTrue(results.TrueForAll(r => r.Effect == HazardEffect.None));
        }

        [Test]
        public void TrapOutcomesPerMemberType()
        {
            var results = PlacementResolver.Resolve(layout, Place("wardrobe", "table", "table"), 1);
            Assert.AreEqual(HazardEffect.Death, results[0].Effect);
            Assert.AreSame(wardrobe, results[0].Hazard);
            Assert.AreEqual(MemberType.Player, PlacementResolver.FirstFatal(results).Member);

            results = PlacementResolver.Resolve(layout, Place("table", "window", "table"), 2);
            Assert.AreEqual(HazardEffect.Injured, results[1].Effect, "wife is injured by the window");
            Assert.IsNull(PlacementResolver.FirstFatal(results));
        }

        [Test]
        public void ChildIsTrappedNeverKilled()
        {
            // Wardrobe is configured to kill the child; it must turn into Trapped.
            var results = PlacementResolver.Resolve(layout, Place("table", "table", "wardrobe"), 1);
            Assert.AreEqual(HazardEffect.Trapped, results[2].Effect);
            Assert.IsTrue(results[2].IsFatal);
        }

        [Test]
        public void ActBitmaskDisablesHazard()
        {
            // Window only fires in acts 2 and 3.
            var results = PlacementResolver.Resolve(layout, Place("window", "table", "table"), 1);
            Assert.AreEqual(HazardEffect.None, results[0].Effect);
            results = PlacementResolver.Resolve(layout, Place("window", "table", "table"), 3);
            Assert.AreEqual(HazardEffect.Death, results[0].Effect);
        }

        [Test]
        public void ChildAloneIsTrapped()
        {
            var results = PlacementResolver.Resolve(layout, Place("table", "table", "corner"), 1);
            Assert.AreEqual(HazardEffect.Trapped, results[2].Effect);
            Assert.IsTrue(results[2].ChildAlone);
        }

        [Test]
        public void ChildAdjacentToAdultIsSupported()
        {
            var results = PlacementResolver.Resolve(layout, Place("table", "table", "wall"), 1);
            Assert.AreEqual(HazardEffect.None, results[2].Effect);
            Assert.IsTrue(layout.AreAdjacent("wall", "table"));
            Assert.IsTrue(layout.AreAdjacent("table", "wall"), "adjacency is symmetric");
        }

        [Test]
        public void UnplacedMembersGetUnplacedHazard()
        {
            var results = PlacementResolver.Resolve(layout, Place(null, "table", "table"), 1);
            Assert.AreEqual(HazardEffect.Death, results[0].Effect);
            Assert.AreSame(openRoom, results[0].Hazard);
        }

        [Test]
        public void FirstFatalFollowsPlayerWifeChildOrder()
        {
            var results = PlacementResolver.Resolve(layout, Place("table", "wardrobe", "corner"), 1);
            Assert.AreEqual(MemberType.Wife, PlacementResolver.FirstFatal(results).Member);
        }

        [Test]
        public void CapacityIsRespected()
        {
            var placements = Place(null, "table", "table");
            Assert.IsFalse(PlacementRules.CanPlace(layout, placements, MemberType.Player, "table", out string reason));
            Assert.AreEqual(UiKeys.PlacementNoRoom, reason);
            Assert.IsTrue(PlacementRules.CanPlace(layout, placements, MemberType.Wife, "table", out _), "moving within the same anchor does not count twice");
            Assert.IsTrue(PlacementRules.CanPlace(layout, placements, MemberType.Player, "wall", out _));
        }

        [Test]
        public void ChildPlacementNeedsAdultNearby()
        {
            var placements = Place(null, "table", null);
            Assert.IsTrue(PlacementRules.CanPlace(layout, placements, MemberType.Child, "wall", out _));
            Assert.IsFalse(PlacementRules.CanPlace(layout, placements, MemberType.Child, "corner", out string reason));
            Assert.AreEqual(UiKeys.PlacementChildAlone, reason);
        }

        [Test]
        public void PlayerIsPlacedLast()
        {
            Assert.IsFalse(PlacementRules.CanSelect(MemberType.Player, Place(null, "table", null)));
            Assert.IsTrue(PlacementRules.CanSelect(MemberType.Wife, Place(null, null, null)));
            Assert.IsTrue(PlacementRules.CanSelect(MemberType.Player, Place(null, "table", "table")));
        }

        [Test]
        public void UnknownAnchorIsInvalid()
        {
            Assert.IsFalse(PlacementRules.CanPlace(layout, Place(null, null, null), MemberType.Wife, "nope", out string reason));
            Assert.AreEqual(UiKeys.PlacementInvalid, reason);
        }
    }
}
