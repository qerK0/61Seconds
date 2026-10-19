using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Editor;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>The street finale: the way to the sign always exists, façade drops never take the only path.</summary>
    public class StreetTests
    {
        private static readonly float[] Densities = { 0f, 0.06f, 0.10f, 0.15f, 0.20f };

        private static BuildingLayout.RoomSpec StreetSpec() => BuildingLayout.Rooms().Find(r => r.Id == BuildingLevelsGenerator.Street);

        [Test]
        public void StreetMatchesTheLayout()
        {
            BuildingLayout.RoomSpec street = StreetSpec();
            Assert.IsNotNull(street);
            Assert.AreEqual(20, street.Width);
            Assert.AreEqual(6, street.Height);
            Assert.IsTrue(street.FollowCamera);
            Assert.AreEqual(StreetLayout.DoorCell, GridNav.CellOf(street.PlayerSpawn), "enter from the building door at the right end");
            Assert.Less(StreetLayout.SignCell.x, StreetLayout.DoorCell.x, "the exit is on the far left");
        }

        [Test]
        public void PathFromDoorToSignExistsFor200Seeds()
        {
            BuildingLayout.RoomSpec street = StreetSpec();
            GridNav baseNav = BuildingLayout.NavFor(street);
            List<Vector2Int> required = BuildingLayout.RequiredCells(street);
            for (int seed = 0; seed < 200; seed++)
            {
                HashSet<Vector2Int> debris = DebrisPlanner.Plan(baseNav, required, street.Id, 4, seed, Densities, 0.03f, street.Depth);
                GridNav nav = baseNav.Clone();
                nav.SetBlocked(debris);
                Assert.IsNotNull(nav.FindPath(StreetLayout.DoorCell, StreetLayout.SignCell), $"seed {seed}: debris cut the street");

                // The façade drops land on top of that debris and still never cut it.
                List<Vector2Int> drops = StreetLayout.PlanFacadeDrops(nav, seed, 4, required);
                var rubble = new List<Vector2Int>();
                foreach (Vector2Int origin in drops) rubble.AddRange(StreetLayout.DropCells(origin));
                List<Vector2Int> safe = DebrisPlanner.SafeExtras(baseNav, required, debris, rubble);
                nav.SetBlocked(safe);
                Assert.IsNotNull(nav.FindPath(StreetLayout.DoorCell, StreetLayout.SignCell), $"seed {seed}: façade rubble cut the street");
            }
        }

        [Test]
        public void FacadeDropsNeverIncludeTheOnlyPathCell()
        {
            GridNav nav = StreetLayout.Nav();
            // Cells whose loss alone would disconnect the door from the sign (the gap in the power line and its sides).
            var chokepoints = new List<Vector2Int>();
            foreach (Vector2Int c in nav.Cells)
            {
                if (!nav.IsWalkable(c)) continue;
                GridNav probe = nav.Clone();
                probe.SetBlocked(c, true);
                if (c != StreetLayout.DoorCell && c != StreetLayout.SignCell && !probe.Connected(StreetLayout.DoorCell, StreetLayout.SignCell)) chokepoints.Add(c);
            }

            Assert.Contains(new Vector2Int(StreetLayout.LineX, StreetLayout.GapY), chokepoints, "the gap in the power line is the only way through");

            for (int seed = 0; seed < 200; seed++)
            {
                List<Vector2Int> drops = StreetLayout.PlanFacadeDrops(nav, seed, 4);
                Assert.GreaterOrEqual(drops.Count, 3, $"seed {seed}: 3-4 façade drops");
                GridNav after = nav.Clone();
                foreach (Vector2Int origin in drops)
                {
                    foreach (Vector2Int cell in StreetLayout.DropCells(origin))
                    {
                        Assert.IsFalse(chokepoints.Contains(cell), $"seed {seed}: drop on the only path cell {cell}");
                        Assert.GreaterOrEqual(cell.y, StreetLayout.Height - 2, "drops land next to the façades");
                        after.SetBlocked(cell, true);
                    }
                }

                Assert.IsTrue(after.Connected(StreetLayout.DoorCell, StreetLayout.SignCell), $"seed {seed}");
            }
        }

        [Test]
        public void OpenMiddleIsAwayFromFacadesAndTheLine()
        {
            Assert.IsTrue(StreetLayout.InOpen(new Vector2Int(5, 2)));
            Assert.IsTrue(StreetLayout.InOpen(new Vector2Int(15, 0)));
            Assert.IsFalse(StreetLayout.InOpen(new Vector2Int(5, StreetLayout.Height - 1)), "right under the façades");
            Assert.IsFalse(StreetLayout.InOpen(new Vector2Int(5, StreetLayout.Height - 2)), "within 1 cell of the façades");
            Assert.IsFalse(StreetLayout.InOpen(new Vector2Int(StreetLayout.LineX + 1, 1)), "next to the power line");
            foreach (Vector2Int spark in StreetLayout.SparkCells) Assert.IsFalse(StreetLayout.InOpen(spark));
        }

        [Test]
        public void CollapseOnlyKillsNearTheBuildingDoor()
        {
            Assert.IsTrue(StreetLayout.CollapseKills(StreetLayout.DoorCell));
            Assert.IsTrue(StreetLayout.CollapseKills(StreetLayout.DoorCell + new Vector2Int(-5, 0)));
            Assert.IsFalse(StreetLayout.CollapseKills(StreetLayout.DoorCell + new Vector2Int(-6, 0)));
            Assert.IsFalse(StreetLayout.CollapseKills(StreetLayout.SignCell));
        }

        [Test]
        public void PowerLineHasExactlyOneGap()
        {
            var line = new HashSet<Vector2Int>(StreetLayout.PowerLine);
            int gaps = 0;
            for (int y = 0; y < StreetLayout.Height; y++) if (!line.Contains(new Vector2Int(StreetLayout.LineX, y))) gaps++;
            Assert.AreEqual(1, gaps);
            foreach (Vector2Int spark in StreetLayout.SparkCells) Assert.IsFalse(line.Contains(spark), "sparks are walkable (and deadly)");
        }
    }
}
