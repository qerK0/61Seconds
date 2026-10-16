using NUnit.Framework;
using SixtyOne.Cinematic;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>Pure blend of CinematicProfile + layered state (no rendering).</summary>
    public class CinematicMixerTests
    {
        private CinematicProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<CinematicProfile>();
            profile.ApplyActDefaults(1);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(profile);

        [Test]
        public void RestStateIsTheBaseLook()
        {
            CinematicFrame f = CinematicMixer.Mix(profile, new CinematicState());
            Assert.AreEqual(profile.PostExposure, f.PostExposure, 0.0001f);
            Assert.AreEqual(profile.Saturation, f.Saturation, 0.0001f);
            Assert.AreEqual(profile.Vignette, f.Vignette, 0.0001f);
            Assert.AreEqual(0f, f.Zoom, 0.0001f);
            Assert.AreEqual(0f, f.Chromatic, 0.0001f);
        }

        [Test]
        public void PrepTensionReachesItsTargetsAtZero()
        {
            CinematicFrame f = CinematicMixer.Mix(profile, new CinematicState { Prep = 1f });
            Assert.AreEqual(profile.PrepSaturationEnd, f.Saturation, 0.0001f);
            Assert.AreEqual(profile.PrepVignetteEnd, f.Vignette, 0.0001f);
            Assert.AreEqual(profile.PrepZoomPercent * 0.01f, f.Zoom, 0.0001f);
        }

        [Test]
        public void HeartbeatAddsVignette()
        {
            float rest = CinematicMixer.Mix(profile, new CinematicState { Prep = 1f }).Vignette;
            float beat = CinematicMixer.Mix(profile, new CinematicState { Prep = 1f, Heartbeat = 1f }).Vignette;
            Assert.AreEqual(profile.HeartbeatVignette, beat - rest, 0.0001f);
        }

        [Test]
        public void PostQuakeIsDarkerThanPrep()
        {
            CinematicFrame prep = CinematicMixer.Mix(profile, new CinematicState());
            CinematicFrame post = CinematicMixer.Mix(profile, new CinematicState { PostQuake = 1f });
            Assert.Less(post.PostExposure, prep.PostExposure);
            Assert.AreEqual(0f, post.Zoom, 0.0001f, "push-in released after the quake");
        }

        [Test]
        public void DeathIsNearBlackAndWhite()
        {
            CinematicFrame f = CinematicMixer.Mix(profile, new CinematicState { Prep = 0.5f, Death = 1f });
            Assert.AreEqual(profile.DeathSaturation, f.Saturation, 0.0001f);
            Assert.AreEqual(profile.DeathContrast, f.Contrast, 0.0001f);
            Assert.AreEqual(profile.DeathVignette, f.Vignette, 0.0001f);
        }

        [Test]
        public void RewindSpikesAberrationAndGrain()
        {
            CinematicFrame f = CinematicMixer.Mix(profile, new CinematicState { Rewind = 1f });
            Assert.AreEqual(profile.RewindChromatic, f.Chromatic, 0.0001f);
            Assert.AreEqual(profile.RewindGrain, f.Grain, 0.0001f);
        }

        [Test]
        public void ReduceEffectsDisablesFlashAberrationAndWobble()
        {
            var state = new CinematicState { Flash = 1f, Chromatic = 0.8f, LensDistortion = 0.3f, Rewind = 1f, ReduceEffects = true };
            CinematicFrame f = CinematicMixer.Mix(profile, state);
            Assert.AreEqual(profile.PostExposure, f.PostExposure, 0.0001f, "no white flash");
            Assert.AreEqual(0f, f.Chromatic, 0.0001f);
            Assert.AreEqual(0f, f.LensDistortion, 0.0001f);
        }

        [Test]
        public void WinBlendsWarmerWithLessVignette()
        {
            CinematicFrame f = CinematicMixer.Mix(profile, new CinematicState { PostQuake = 1f, Win = 1f });
            Assert.AreEqual(profile.WinVignette, f.Vignette, 0.0001f);
            Assert.AreEqual(profile.WinSaturation, f.Saturation, 0.0001f);
        }

        [Test]
        public void EachActIsDarkerAndHarsher()
        {
            var acts = new CinematicProfile[3];
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    acts[i] = ScriptableObject.CreateInstance<CinematicProfile>();
                    acts[i].ApplyActDefaults(i + 1);
                }

                for (int i = 1; i < 3; i++)
                {
                    Assert.Less(acts[i].PostExposure, acts[i - 1].PostExposure, "darker");
                    Assert.Less(acts[i].Saturation, acts[i - 1].Saturation, "more desaturated");
                    Assert.Greater(acts[i].Contrast, acts[i - 1].Contrast, "harsher");
                    Assert.Greater(acts[i].Vignette, acts[i - 1].Vignette);
                    Assert.Greater(acts[i].RotationalShakeDegrees, acts[i - 1].RotationalShakeDegrees);
                }

                Assert.AreEqual(0f, acts[0].DutchTiltDegrees, 0.0001f);
                Assert.AreEqual(0f, acts[1].DutchTiltDegrees, 0.0001f);
                Assert.That(acts[2].DutchTiltDegrees, Is.InRange(2f, 3f), "act 3 dutch tilt 2-3 degrees");
            }
            finally
            {
                foreach (CinematicProfile p in acts) if (p != null) Object.DestroyImmediate(p);
            }
        }

        [Test]
        public void ApproachMovesAtTheGivenRate()
        {
            Assert.AreEqual(0.5f, CinematicMixer.Approach(0f, 1f, 0.5f, 0.25f), 0.0001f);
            Assert.AreEqual(1f, CinematicMixer.Approach(0.9f, 1f, 0.5f, 0.25f), 0.0001f, "clamps at target");
            Assert.AreEqual(0f, CinematicMixer.Approach(1f, 0f, 0f, 0.01f), 0.0001f, "0 seconds = instant");
        }
    }
}
