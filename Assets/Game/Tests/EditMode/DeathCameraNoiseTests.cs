using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class DeathCameraNoiseTests
    {
        [Test]
        public void ZeroAmplitudeHasNoCameraOffset()
        {
            Assert.AreEqual(Vector3.zero, DeathCameraNoise.Sample(12f, 2f, 0f));
        }

        [Test]
        public void SamplePreservesAuthoredPerlinShapeAndIsDeterministic()
        {
            float time = 4.3f;
            var expected = new Vector3(Mathf.PerlinNoise(time, 0.17f) - 0.5f,
                Mathf.PerlinNoise(0.43f, time) - 0.5f, Mathf.PerlinNoise(time, 0.79f) - 0.5f) * 0.07f;
            Assert.AreEqual(expected, DeathCameraNoise.Sample(time, 0f, 0.07f));
            Assert.AreEqual(expected, DeathCameraNoise.Sample(time, 0f, 0.07f));
        }
    }
}
