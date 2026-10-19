using NUnit.Framework;
using SixtyOne.Cinematic;
using UnityEngine;

namespace SixtyOne.Tests
{
    /// <summary>The Parlaklık slider: the middle keeps BrightnessConfig's look, the ends add ± its user range in EV.</summary>
    public class BrightnessTests
    {
        [Test]
        public void MiddleOfTheSliderIsTheConfiguredLook()
        {
            Assert.AreEqual(0f, BrightnessConfig.UserOffset(0.5f, 0.6f), 1e-5f);
        }

        [Test]
        public void EndsAddTheUserRange()
        {
            Assert.AreEqual(-0.6f, BrightnessConfig.UserOffset(0f, 0.6f), 1e-5f);
            Assert.AreEqual(0.6f, BrightnessConfig.UserOffset(1f, 0.6f), 1e-5f);
        }

        [Test]
        public void OutOfRangeValuesClamp()
        {
            Assert.AreEqual(0.6f, BrightnessConfig.UserOffset(3f, 0.6f), 1e-5f);
            Assert.AreEqual(-0.6f, BrightnessConfig.UserOffset(-1f, 0.6f), 1e-5f);
        }

        [Test]
        public void DefaultsMakeTheGameBrighterButKeepTheVignette()
        {
            var config = ScriptableObject.CreateInstance<BrightnessConfig>();
            Assert.That(config.ExposureBoost, Is.InRange(0.3f, 0.4f));
            Assert.That(config.VignetteScale, Is.InRange(0.6f, 0.8f));
            Assert.AreEqual(config.ExposureBoost, config.ExposureFor(0.5f), 1e-5f);
            Object.DestroyImmediate(config);
        }
    }
}
