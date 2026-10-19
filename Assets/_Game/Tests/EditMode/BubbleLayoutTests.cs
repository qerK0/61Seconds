using NUnit.Framework;
using SixtyOne.UI;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Speech bubble placement: above-right by preference, flips left, stays in the safe area, docks off-screen.</summary>
    public class BubbleLayoutTests
    {
        // 1920x1080 canvas: below the panic meter (y 916) and left of the bag column (x 1560).
        private static readonly Rect Safe = Rect.MinMaxRect(16f, 16f, 1560f, 900f);
        private static readonly Vector2 Offset = new Vector2(22f, 18f);

        [Test]
        public void PrefersAboveRightOfTheHead()
        {
            BubblePlacement p = BubbleLayout.Place(new Vector2(600f, 400f), new Vector2(380f, 120f), 120f, Safe, Offset);
            Assert.IsFalse(p.OnLeft);
            Assert.AreEqual(622f, p.BlockMin.x, 0.01f);
            Assert.AreEqual(418f, p.BlockMin.y, 0.01f, "bubble bottom just above the head");
            Assert.Less(p.TailAngle, 0f, "tail points down at the head");
            Assert.IsFalse(p.Docked);
        }

        [Test]
        public void FlipsLeftNearTheBagColumn()
        {
            BubblePlacement p = BubbleLayout.Place(new Vector2(1400f, 400f), new Vector2(380f, 120f), 120f, Safe, Offset);
            Assert.IsTrue(p.OnLeft);
            Assert.LessOrEqual(p.BlockMin.x + 380f, Safe.xMax);
        }

        [Test]
        public void NeverCoversTheTimerOrTheBag()
        {
            // Head right under the HUD: the block is pushed down, inside the safe area.
            BubblePlacement p = BubbleLayout.Place(new Vector2(900f, 880f), new Vector2(400f, 260f), 120f, Safe, Offset);
            var block = new Rect(p.BlockMin, new Vector2(400f, 260f));
            Assert.LessOrEqual(block.yMax, Safe.yMax + 0.01f);
            Assert.LessOrEqual(block.xMax, Safe.xMax + 0.01f);
            Assert.GreaterOrEqual(block.xMin, Safe.xMin - 0.01f);
            Assert.GreaterOrEqual(block.yMin, Safe.yMin - 0.01f);
        }

        [Test]
        public void OffScreenSpeakerDocksWithTheTailTowardThem()
        {
            BubblePlacement p = BubbleLayout.Place(new Vector2(-300f, 500f), new Vector2(380f, 120f), 120f, Safe, Offset);
            Assert.IsTrue(p.Docked);
            Assert.AreEqual(Safe.xMin, p.BlockMin.x, 0.01f, "docked to the left edge");
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(p.TailAngle, 0f)), 90f, "tail points left, at the speaker");
        }
    }
}
