using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Core;
using SixtyOne.Editor;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Debris never blocks progress, only grows, is seeded; cracks only increase.</summary>
    public class DebrisTests
    {
        private static readonly float[] Densities = { 0f, 0.06f, 0.10f, 0.15f, 0.20f };
        private static readonly float[] CrackChances = { 0f, 0.15f, 0.35f, 0.55f, 0.7f };
        private const float DepthBonus = 0.03f;

        private static HashSet<Vector2Int> Plan(BuildingLayout.RoomSpec room, int quakes, int seed)
        {
            return DebrisPlanner.Plan(BuildingLayout.NavFor(room), BuildingLayout.RequiredCells(room), room.Id, quakes, seed, Densities, DepthBonus, room.Depth);
        }

        [Test]
        public void DebrisNeverDisconnectsRequiredCells_Fuzz200SeedsEveryRoom()
        {
            int placedTotal = 0;
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                GridNav baseNav = BuildingLayout.NavFor(room);
                var required = new HashSet<Vector2Int>();
                foreach (Vector2Int c in BuildingLayout.RequiredCells(room)) required.Add(baseNav.NearestFloor(c));
                List<List<Vector2Int>> groups = DebrisPlanner.ConnectedGroups(baseNav, required);

                for (int seed = 0; seed < 200; seed++)
                {
                    HashSet<Vector2Int> debris = Plan(room, 4, seed);
                    placedTotal += debris.Count;
                    foreach (Vector2Int r in required) Assert.IsFalse(debris.Contains(r), $"{room.Id} seed {seed}: debris on required cell {r}");

                    GridNav nav = baseNav.Clone();
                    nav.SetBlocked(debris);
                    Assert.IsTrue(DebrisPlanner.StillConnected(nav, groups), $"{room.Id} seed {seed}: debris cut a required path");
                    foreach (List<Vector2Int> group in groups)
                    {
                        for (int i = 1; i < group.Count; i++)
                        {
                            Assert.IsNotNull(nav.FindPath(group[0], group[i]), $"{room.Id} seed {seed}: no path {group[0]} -> {group[i]}");
                        }
                    }
                }
            }

            Assert.Greater(placedTotal, 0, "the fuzz actually placed debris");
        }

        [Test]
        public void EveryRoomHasItsRequiredCellsConnectedBeforeDebris()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                GridNav nav = BuildingLayout.NavFor(room);
                var required = new List<Vector2Int>();
                foreach (Vector2Int c in BuildingLayout.RequiredCells(room)) required.Add(nav.NearestFloor(c));
                Assert.AreEqual(1, DebrisPlanner.ConnectedGroups(nav, required).Count, room.Id + ": doors, spawns and items must all be reachable");
            }
        }

        [Test]
        public void DensityIsMonotonic()
        {
            for (int depth = 0; depth <= 3; depth++)
            {
                float previous = -1f;
                for (int quake = 0; quake <= 6; quake++)
                {
                    float d = DebrisPlanner.Density(Densities, quake, DepthBonus, depth);
                    Assert.GreaterOrEqual(d, previous, $"depth {depth} quake {quake}");
                    previous = d;
                }
            }

            Assert.AreEqual(0f, DebrisPlanner.Density(Densities, 0, DepthBonus, 3), "no debris before the quake, on any floor");
            Assert.Greater(DebrisPlanner.Density(Densities, 2, DepthBonus, 2), DebrisPlanner.Density(Densities, 2, DepthBonus, 0), "lower floors get more");
        }

        [Test]
        public void DebrisOnlyGrowsWithEveryQuake()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                for (int seed = 0; seed < 20; seed++)
                {
                    HashSet<Vector2Int> previous = Plan(room, 0, seed);
                    Assert.AreEqual(0, previous.Count);
                    for (int quake = 1; quake <= 4; quake++)
                    {
                        HashSet<Vector2Int> next = Plan(room, quake, seed);
                        Assert.IsTrue(next.IsSupersetOf(previous), $"{room.Id} seed {seed}: quake {quake} lost debris");
                        previous = next;
                    }
                }
            }
        }

        [Test]
        public void SameSeedGivesTheSameLayout()
        {
            foreach (BuildingLayout.RoomSpec room in BuildingLayout.Rooms())
            {
                HashSet<Vector2Int> a = Plan(room, 3, 61);
                HashSet<Vector2Int> b = Plan(room, 3, 61);
                Assert.IsTrue(a.SetEquals(b), room.Id);
            }

            Assert.AreEqual(DebrisPlanner.Seed("flat7", 3, 61), DebrisPlanner.Seed("flat7", 3, 61));
            Assert.AreNotEqual(DebrisPlanner.Seed("flat7", 3, 61), DebrisPlanner.Seed("flat7", 2, 61));
        }

        [Test]
        public void CracksOnlyIncrease()
        {
            for (int depth = 0; depth <= 3; depth++)
            {
                var cracked = new HashSet<string>();
                for (int quake = 0; quake <= 4; quake++)
                {
                    float chance = CrackRules.Chance(CrackChances, quake, 0.05f, depth);
                    var now = new HashSet<string>();
                    for (int seg = 0; seg < 40; seg++)
                    {
                        string key = "L" + seg + "_r0";
                        if (CrackRules.IsCracked("f3_corridor", key, 61, chance)) now.Add(key);
                    }

                    Assert.IsTrue(now.IsSupersetOf(cracked), $"depth {depth} quake {quake}: a crack disappeared");
                    cracked = now;
                }
            }

            Assert.AreEqual(0f, CrackRules.Chance(CrackChances, 0, 0.05f, 0), "about 0% in the pre-quake flat");
            Assert.AreEqual(0.15f, CrackRules.Chance(CrackChances, 1, 0.05f, 0), 1e-5f);
            Assert.AreEqual(0.55f, CrackRules.Chance(CrackChances, 3, 0.05f, 0), 1e-5f);
        }

        [Test]
        public void QuakeCountFollowsTheQuakesAndRewindsWithSnapshots()
        {
            Assert.AreEqual(1, QuakeRules.CountAfter(0, 1));
            Assert.AreEqual(3, QuakeRules.CountAfter(1, 3), "falling through the floor skips ahead");
            Assert.AreEqual(3, QuakeRules.CountAfter(3, 2), "never lower");
            Assert.AreEqual(4, QuakeRules.CountAfter(3, 0));

            var session = new GameSession();
            session.State.QuakeCount = 1;
            session.TakeSnapshot("f3.aftershock");
            session.State.QuakeCount = 2;
            session.Die(new DeathInfo());
            session.Rewind();
            Assert.AreEqual(1, session.State.QuakeCount, "the rewound quake takes its debris back");
        }
    }
}
