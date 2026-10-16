using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Prep;

namespace SixtyOne.Tests
{
    public class PrepCountdownTests
    {
        private PrepCountdown countdown;
        private int windowEvents;
        private int zeroEvents;
        private List<int> ticks;

        [SetUp]
        public void SetUp()
        {
            countdown = new PrepCountdown();
            windowEvents = 0;
            zeroEvents = 0;
            ticks = new List<int>();
            countdown.PlacementWindowReached += () => windowEvents++;
            countdown.Zero += () => zeroEvents++;
            countdown.SecondTick += s => ticks.Add(s);
        }

        private void Run(float seconds, float step)
        {
            for (float t = 0f; t < seconds - 0.0001f; t += step) countdown.Tick(step);
        }

        [Test]
        public void PlacementWindowFiresOnceAtThreshold()
        {
            countdown.Start(61f, 10f);
            Run(50.5f, 0.25f);
            Assert.AreEqual(0, windowEvents, "10.5s left: not yet");
            Assert.IsFalse(countdown.InPlacementWindow);
            Run(1f, 0.25f);
            Assert.AreEqual(1, windowEvents);
            Assert.IsTrue(countdown.InPlacementWindow);
            Run(5f, 0.25f);
            Assert.AreEqual(1, windowEvents, "fires only once");
        }

        [Test]
        public void LastTenSecondsTickOnceEachThenZero()
        {
            countdown.Start(61f, 10f, tickFrom: 10);
            Run(61f, 0.1f);
            CollectionAssert.AreEqual(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, ticks);
            Assert.AreEqual(1, zeroEvents);
            Assert.IsFalse(countdown.Running);
            countdown.Tick(1f);
            Assert.AreEqual(1, zeroEvents, "stopped countdown does nothing");
        }

        [Test]
        public void BigDeltaCrossesSeveralSecondsAndThresholds()
        {
            countdown.Start(12f, 10f, tickFrom: 10);
            countdown.Tick(4.5f); // 12 -> 7.5: crosses the window and seconds 10, 9, 8
            Assert.AreEqual(1, windowEvents);
            CollectionAssert.AreEqual(new[] { 10, 9, 8 }, ticks);
        }

        [Test]
        public void JumpToSkipsEventsForTheSkippedRange()
        {
            countdown.Start(61f, 10f);
            countdown.JumpTo(10f);
            Assert.AreEqual(0, windowEvents, "jumping into the window does not re-fire it");
            Assert.IsTrue(countdown.InPlacementWindow);
            countdown.Tick(0.5f);
            Assert.AreEqual(0, windowEvents);
            Assert.AreEqual(0, ticks.Count, "no whole second crossed between 10 and 9.5");
            countdown.Tick(0.6f);
            CollectionAssert.AreEqual(new[] { 9 }, ticks);
        }

        [Test]
        public void ZeroPrepStartsInsideTheWindow()
        {
            countdown.Start(5f, 10f);
            Assert.IsTrue(countdown.InPlacementWindow);
            countdown.Tick(1f);
            Assert.AreEqual(0, windowEvents);
        }
    }
}
