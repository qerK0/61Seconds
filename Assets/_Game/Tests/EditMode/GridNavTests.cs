using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Grid A*: around walls, unreachable targets, no diagonal squeezes, smoothing.</summary>
    public class GridNavTests
    {
        private static Vector2Int C(int x, int y) => new Vector2Int(x, y);

        [Test]
        public void PathGoesAroundAWallOfBlockedCells()
        {
            var nav = new GridNav(6, 6);
            // Wall at x = 2 from y = 0 to 4; the gap is at y = 5.
            for (int y = 0; y <= 4; y++) nav.SetBlocked(C(2, y), true);

            List<Vector2Int> path = nav.FindPath(C(0, 0), C(4, 0));
            Assert.IsNotNull(path);
            Assert.AreEqual(C(0, 0), path[0]);
            Assert.AreEqual(C(4, 0), path[path.Count - 1]);
            foreach (Vector2Int c in path) Assert.IsFalse(nav.IsBlocked(c), "path crosses the wall at " + c);
            Assert.IsTrue(path.Contains(C(2, 5)), "the only gap");
            for (int i = 1; i < path.Count; i++) Assert.IsTrue(nav.CanStep(path[i - 1], path[i]));
        }

        [Test]
        public void SmoothedPlanNeverCrossesBlockedCells()
        {
            var nav = new GridNav(6, 6);
            for (int y = 0; y <= 4; y++) nav.SetBlocked(C(2, y), true);

            Vector2 from = new Vector2(0.5f, 0.5f);
            List<Vector2> plan = nav.Plan(from, new Vector2(4.5f, 0.5f));
            Assert.Greater(plan.Count, 1, "it has to turn around the wall");
            Assert.AreEqual(new Vector2(4.5f, 0.5f), plan[plan.Count - 1]);
            Vector2 previous = from;
            foreach (Vector2 p in plan)
            {
                Assert.IsTrue(nav.LineClear(previous, p, 0f), $"segment {previous} -> {p} crosses a blocked cell");
                previous = p;
            }
        }

        [Test]
        public void OpenFloorIsOneStraightSegment()
        {
            var nav = new GridNav(5, 5);
            List<Vector2> plan = nav.Plan(new Vector2(0.5f, 0.5f), new Vector2(4.2f, 3.1f));
            Assert.AreEqual(1, plan.Count, "a clear straight line cuts every corner");
        }

        [Test]
        public void UnreachableTargetGoesToNearestReachableFreeCell()
        {
            var nav = new GridNav(5, 3);
            // Wall the right part off completely at x = 3.
            for (int y = 0; y < 3; y++) nav.SetBlocked(C(3, y), true);

            Assert.IsNull(nav.FindPath(C(0, 1), C(4, 1)));
            Assert.AreEqual(C(2, 1), nav.NearestReachable(C(0, 1), C(4, 1)));
            List<Vector2> plan = nav.Plan(new Vector2(0.5f, 1.5f), new Vector2(4.5f, 1.5f));
            Assert.AreEqual(GridNav.Center(C(2, 1)), plan[plan.Count - 1]);
        }

        [Test]
        public void ClickOnABlockedCellGoesToTheNearestFreeCell()
        {
            var nav = new GridNav(5, 5);
            nav.SetBlocked(C(3, 3), true);
            List<Vector2> plan = nav.Plan(new Vector2(0.5f, 0.5f), new Vector2(3.5f, 3.5f));
            Vector2Int end = GridNav.CellOf(plan[plan.Count - 1]);
            Assert.IsFalse(nav.IsBlocked(end));
            Assert.LessOrEqual((GridNav.Center(end) - new Vector2(3.5f, 3.5f)).magnitude, 1.5f, "a neighbour of the clicked cell");
        }

        [Test]
        public void NoDiagonalSqueezeBetweenTwoBlockedCorners()
        {
            var nav = new GridNav(2, 2);
            nav.SetBlocked(C(1, 0), true);
            nav.SetBlocked(C(0, 1), true);

            Assert.IsFalse(nav.CanStep(C(0, 0), C(1, 1)));
            Assert.IsNull(nav.FindPath(C(0, 0), C(1, 1)), "the only way is the diagonal squeeze");
            Assert.IsFalse(nav.LineClear(new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f)), "the smoother must not cut it either");
        }

        [Test]
        public void DiagonalStepNeedsBothSideCellsFree()
        {
            var nav = new GridNav(3, 3);
            nav.SetBlocked(C(1, 0), true);
            Assert.IsFalse(nav.CanStep(C(0, 0), C(1, 1)), "one blocked side cell is enough to forbid corner cutting");
            List<Vector2Int> path = nav.FindPath(C(0, 0), C(1, 1));
            Assert.AreEqual(new List<Vector2Int> { C(0, 0), C(0, 1), C(1, 1) }, path);
        }

        [Test]
        public void ApproachCellUnderDebrisUsesNearestFreeNeighbour()
        {
            var nav = new GridNav(4, 4);
            nav.SetBlocked(C(2, 2), true);
            Vector2Int free = nav.NearestFree(C(2, 2));
            Assert.IsTrue(nav.IsWalkable(free));
            Assert.LessOrEqual(Mathf.Abs(free.x - 2) + Mathf.Abs(free.y - 2), 1);
            Assert.AreEqual(C(1, 1), nav.NearestFree(C(1, 1)), "a free cell is its own approach");
        }

        [Test]
        public void ExtraCellsAreWalkable()
        {
            var nav = new GridNav(3, 2, new[] { C(3, 0) });
            Assert.IsTrue(nav.IsWalkable(C(3, 0)));
            Assert.IsFalse(nav.IsWalkable(C(3, 1)));
            Assert.IsNotNull(nav.FindPath(C(0, 1), C(3, 0)));
        }

        [Test]
        public void LeavingABlockedStartCellIsAllowed()
        {
            var nav = new GridNav(3, 3);
            nav.SetBlocked(C(0, 0), true);
            Assert.IsNotNull(nav.FindPath(C(0, 0), C(2, 2)), "debris fell where someone stands");
        }
    }
}
