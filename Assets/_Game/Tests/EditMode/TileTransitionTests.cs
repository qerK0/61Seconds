using NUnit.Framework;
using SixtyOne.Placement;

namespace SixtyOne.Tests
{
    /// <summary>The tile transition schedule: wave order, layer order, totals from TransitionConfig, easing.</summary>
    public class TileTransitionTests
    {
        private TestData data;
        private TransitionConfig config;

        [SetUp]
        public void SetUp()
        {
            data = new TestData();
            config = data.Make<TransitionConfig>();
            config.Configure(0.35f, 0.4f, 0.65f, 1.2f, 1f, 2.5f, 0.12f, 0.05f, 0.1f, 0.1f, 0.15f, 0.25f, null, null, null);
        }

        [TearDown]
        public void TearDown() => data.Cleanup();

        [Test]
        public void DepthRunsFromTheFarCornerToTheCamera()
        {
            Assert.AreEqual(0f, TileTransitionPlan.Depth01(3f, 0f, 3f), 1e-5f, "highest local y = far corner");
            Assert.AreEqual(1f, TileTransitionPlan.Depth01(0f, 0f, 3f), 1e-5f, "lowest = nearest");
            Assert.AreEqual(0.5f, TileTransitionPlan.Depth01(1.5f, 0f, 3f), 1e-5f);
            Assert.AreEqual(0f, TileTransitionPlan.Depth01(2f, 2f, 2f), "flat room");
        }

        [Test]
        public void OutWaveStartsFarAndEveryPieceFinishesAtOutDuration()
        {
            TransitionTiming t = config.Timing(false);
            float far = TileTransitionPlan.OutDelay(0f, t);
            float mid = TileTransitionPlan.OutDelay(0.5f, t);
            float near = TileTransitionPlan.OutDelay(1f, t);
            Assert.AreEqual(0f, far, 1e-5f);
            Assert.Less(far, mid);
            Assert.Less(mid, near);
            Assert.AreEqual(0.35f, near + TileTransitionPlan.OutPieceDuration(t), 1e-4f, "last piece ends at outDuration");
            Assert.AreEqual(0.35f, TileTransitionPlan.OutTotal(t), 1e-4f);
        }

        [Test]
        public void InDropsFloorThenWallsThenPropsAndLandsAtInDuration()
        {
            TransitionTiming t = config.Timing(false);
            float floor = TileTransitionPlan.InDelay(TransitionLayer.Floor, 0f, t);
            float walls = TileTransitionPlan.InDelay(TransitionLayer.Walls, 0f, t);
            float props = TileTransitionPlan.InDelay(TransitionLayer.Props, 0f, t);
            Assert.Less(floor, walls, "walls arrive slightly after the floor");
            Assert.Less(walls, props, "props last");
            Assert.Less(TileTransitionPlan.InDelay(TransitionLayer.Floor, 0f, t), TileTransitionPlan.InDelay(TransitionLayer.Floor, 1f, t), "wave inside a layer");

            float lastLanding = TileTransitionPlan.InDelay(TransitionLayer.Props, 1f, t) + TileTransitionPlan.InPieceDuration(t);
            Assert.AreEqual(0.4f, lastLanding, 1e-4f, "everything has landed at inDuration");
            Assert.AreEqual(0.4f + 0.15f, TileTransitionPlan.InTotal(t), 1e-4f, "then the family pops in");
            Assert.AreEqual(0.35f + 0.55f, TileTransitionPlan.Total(t), 1e-4f);
        }

        [Test]
        public void DescendUsesTheLongerHigherDrop()
        {
            TransitionTiming lateral = config.Timing(false);
            TransitionTiming descend = config.Timing(true);
            Assert.AreEqual(0.65f, TileTransitionPlan.InDelay(TransitionLayer.Props, 1f, descend) + TileTransitionPlan.InPieceDuration(descend), 1e-4f);
            Assert.Greater(descend.Drop, lateral.Drop);
            Assert.AreEqual(lateral.OutDuration, descend.OutDuration, 1e-5f, "same rise");
        }

        [Test]
        public void ScheduleSurvivesExtremeConfigs()
        {
            config.Configure(0.05f, 0.05f, 0.05f, 1f, 1f, 1f, 5f, 5f, 5f, 0f, 0f, 0.1f, null, null, null);
            TransitionTiming t = config.Timing(false);
            Assert.Greater(TileTransitionPlan.OutPieceDuration(t), 0f, "a huge stagger never leaves zero-length pieces");
            Assert.Greater(TileTransitionPlan.InPieceDuration(t), 0f);
            Assert.IsTrue(TileTransitionPlan.OutDelay(1f, t) + TileTransitionPlan.OutPieceDuration(t) <= 0.05f + 1e-4f, "still fits the phase");
        }

        [Test]
        public void EasingStartsHighLandsAtZeroAndSettlesWithABounce()
        {
            Assert.AreEqual(1f, TileTransitionPlan.DropOffset01(0f, 0.1f), 1e-4f);
            Assert.AreEqual(0f, TileTransitionPlan.DropOffset01(1f, 0.1f), 1e-4f);
            float lowest = 1f;
            for (int i = 0; i <= 100; i++) lowest = System.Math.Min(lowest, TileTransitionPlan.DropOffset01(i / 100f, 0.1f));
            Assert.Less(lowest, -0.05f, "dips below the floor before settling");
            Assert.Greater(lowest, -0.15f, "about 10%");
            Assert.IsTrue(TileTransitionPlan.DropOffset01(0.5f, 0f) >= 0f, "no bounce = no dip");

            Assert.AreEqual(0f, TileTransitionPlan.EaseIn(0f), 1e-5f);
            Assert.AreEqual(0.25f, TileTransitionPlan.EaseIn(0.5f), 1e-5f, "ease-in");
            Assert.AreEqual(0.6f, TileTransitionPlan.PopScale(0f), 1e-4f);
            Assert.AreEqual(1f, TileTransitionPlan.PopScale(1f), 1e-4f);
            Assert.AreEqual(0.5f, TileTransitionPlan.Progress(0.2f, 0.1f, 0.2f), 1e-5f);
            Assert.AreEqual(0f, TileTransitionPlan.Progress(0.05f, 0.1f, 0.2f), 1e-5f, "not started yet");
        }
    }
}
