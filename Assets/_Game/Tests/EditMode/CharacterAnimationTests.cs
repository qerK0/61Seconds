using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Run-frame ordering and the code animator's state rules.</summary>
    public class CharacterAnimationTests
    {
        [Test]
        public void CrouchBeatsRunBeatsIdle()
        {
            Vector2 running = new Vector2(1f, 0f);
            Assert.AreEqual(CharacterAnimState.Crouch, CharacterAnimator.StateFor(true, running, true));
            Assert.AreEqual(CharacterAnimState.Crouch, CharacterAnimator.StateFor(false, Vector2.zero, true));
            Assert.AreEqual(CharacterAnimState.Run, CharacterAnimator.StateFor(true, running, false));
            Assert.AreEqual(CharacterAnimState.Idle, CharacterAnimator.StateFor(false, Vector2.zero, false));
        }

        [Test]
        public void CrouchPlaysOnceThenHoldsTheLastFrame()
        {
            const int frames = 10;
            float clock = 0f;
            for (int i = 0; i < 60; i++) clock = CharacterAnimator.CrouchClock(clock, 0.05f, true, 14f, 18f, frames);
            Assert.AreEqual(frames - 1, CharacterAnimator.CrouchFrameIndex(clock, frames), "held on the last frame");
            Assert.AreEqual(frames - 1, CharacterAnimator.CrouchFrameIndex(clock + 100f, frames), "never past it");

            // Standing up plays it backwards (18 fps) down to frame 0 and stops there.
            float half = CharacterAnimator.CrouchClock(clock, 0.25f, false, 14f, 18f, frames);
            Assert.AreEqual(4.5f, half, 0.001f);
            for (int i = 0; i < 20; i++) half = CharacterAnimator.CrouchClock(half, 0.05f, false, 14f, 18f, frames);
            Assert.AreEqual(0f, half);
            Assert.AreEqual(0, CharacterAnimator.CrouchFrameIndex(half, frames));
            Assert.AreEqual(0f, CharacterAnimator.CrouchClock(3f, 1f, true, 14f, 18f, 0), "no frames = no crouch");
        }

        [Test]
        public void FramesSortNumericallyByTheNumberInParentheses()
        {
            var paths = new List<string>
            {
                "Assets/_Game/Art/char/RUN/mehmetrun (10).png",
                "Assets/_Game/Art/char/RUN/mehmetrun (2).png",
                "Assets/_Game/Art/char/RUN/mehmetrun (1).png",
                "Assets/_Game/Art/char/RUN/mehmetrun (11).png",
                "Assets/_Game/Art/char/RUN/mehmetrun (9).png"
            };
            FrameOrder.Sort(paths);

            var numbers = new List<int>();
            foreach (string p in paths) numbers.Add(FrameOrder.NumberOf(p));
            CollectionAssert.AreEqual(new[] { 1, 2, 9, 10, 11 }, numbers, "(10) must come after (9), not after (1)");
            Assert.AreEqual(7, FrameOrder.NumberOf("cocukrun (7)"));
            Assert.AreEqual(0, FrameOrder.NumberOf("annerun.png"), "no number = 0");
        }

        [Test]
        public void RunsOnlyWhileMovingWithARealVelocity()
        {
            Assert.AreEqual(CharacterAnimState.Run, CharacterAnimator.StateFor(true, new Vector2(1.2f, 0.3f)));
            Assert.AreEqual(CharacterAnimState.Idle, CharacterAnimator.StateFor(false, new Vector2(1.2f, 0.3f)), "stopped motor = idle");
            Assert.AreEqual(CharacterAnimState.Idle, CharacterAnimator.StateFor(true, new Vector2(0.001f, 0f)), "creeping = idle (no foot slide)");
            Assert.AreEqual(CharacterAnimState.Idle, CharacterAnimator.StateFor(false, Vector2.zero));
        }

        [Test]
        public void RunFramesLoopAndFlipFollowsHorizontalDirection()
        {
            Assert.AreEqual(0, CharacterAnimator.RunFrameIndex(0f, 11));
            Assert.AreEqual(10, CharacterAnimator.RunFrameIndex(10.9f, 11));
            Assert.AreEqual(0, CharacterAnimator.RunFrameIndex(11f, 11), "loops");
            Assert.AreEqual(3, CharacterAnimator.RunFrameIndex(12f, 9), "child has 9 frames");
            Assert.AreEqual(0, CharacterAnimator.RunFrameIndex(5f, 0), "no frames is safe");

            Assert.IsTrue(CharacterAnimator.FlipFor(-1f, true, false), "art faces right: running left flips");
            Assert.IsFalse(CharacterAnimator.FlipFor(1f, true, true));
            Assert.IsTrue(CharacterAnimator.FlipFor(0.001f, true, true), "tiny velocity keeps the facing");
        }

        [Test]
        public void BreathingIsFasterWithPanic()
        {
            float calm = CharacterAnimator.BreathRate(1.6f, 0f, 2.2f);
            float panicked = CharacterAnimator.BreathRate(1.6f, 1f, 2.2f);
            Assert.AreEqual(Mathf.PI * 2f / 1.6f, calm, 1e-4f, "one breath per period when calm");
            Assert.AreEqual(calm * 2.2f, panicked, 1e-4f);
        }
    }
}
