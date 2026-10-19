using NUnit.Framework;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Prep;

namespace SixtyOne.Tests
{
    /// <summary>A room transition never reduces the prep countdown or the building timer beyond the door's configured cost.</summary>
    public class TransitionTimerTests
    {
        private const float DoorCost = 1f;
        private const float TransitionSeconds = 1.15f;
        private const float FrameSeconds = 1f / 60f;

        [SetUp]
        public void SetUp() => NavigationEvents.ClearAll();

        [TearDown]
        public void TearDown() => NavigationEvents.ClearAll();

        [Test]
        public void TimersPauseOnlyBetweenStartAndComplete()
        {
            Assert.IsFalse(NavigationEvents.TimersPaused);
            Assert.AreEqual(0.5f, NavigationEvents.TimerDelta(0.5f));
            NavigationEvents.RaiseTransitionStart("a", "b");
            Assert.IsTrue(NavigationEvents.TimersPaused);
            Assert.AreEqual(0f, NavigationEvents.TimerDelta(0.5f));
            NavigationEvents.RaiseTransitionComplete("b");
            Assert.IsFalse(NavigationEvents.TimersPaused);
        }

        [Test]
        public void PrepCountdownOnlyPaysTheDoorCost()
        {
            var timer = new PrepTimer();
            timer.Configure(60f, 10);
            float prep = timer.Advance(60f, DoorCost);
            prep = RunTransition(prep, timer.Advance);
            Assert.AreEqual(60f - DoorCost, prep, 1e-4f);
        }

        [Test]
        public void BuildingTimerOnlyPaysTheDoorCost()
        {
            var timer = new BuildingTimer();
            timer.Configure(480f, 60f, 30f);
            float building = timer.Advance(480f, DoorCost);
            building = RunTransition(building, timer.Advance);
            Assert.AreEqual(480f - DoorCost, building, 1e-4f);
        }

        [Test]
        public void TimersRunAgainAfterTheTransition()
        {
            var timer = new BuildingTimer();
            timer.Configure(480f, 60f, 30f);
            float building = RunTransition(480f, timer.Advance);
            building = timer.Advance(building, NavigationEvents.TimerDelta(1f));
            Assert.AreEqual(479f, building, 1e-4f);
        }

        // Ticks frames through a whole transition the way PrepPhase / SurvivalController do.
        private static float RunTransition(float remaining, System.Func<float, float, float> advance)
        {
            NavigationEvents.RaiseTransitionStart("from", "to");
            for (float t = 0f; t < TransitionSeconds; t += FrameSeconds)
            {
                remaining = advance(remaining, NavigationEvents.TimerDelta(FrameSeconds));
            }

            NavigationEvents.RaiseTransitionComplete("to");
            return remaining;
        }
    }
}
