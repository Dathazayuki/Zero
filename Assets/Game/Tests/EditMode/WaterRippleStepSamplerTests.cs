using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Utilities;

namespace Zero.Tests.EditMode
{
    public sealed class WaterRippleStepSamplerTests
    {
        [Test]
        public void StationaryAndVerticalMotionNeverEmit()
        {
            var sampler = new WaterRippleStepSampler();
            Assert.That(sampler.Sample(Vector3.zero, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.up, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.up, true, 0.65f), Is.False);
        }

        [Test]
        public void HorizontalDistanceAccumulatesAcrossFrames()
        {
            var sampler = new WaterRippleStepSampler();
            sampler.Reset(Vector3.zero);
            Assert.That(sampler.Sample(Vector3.forward * 0.3f, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.forward * 0.7f, true, 0.65f), Is.True);
            Assert.That(sampler.Sample(Vector3.forward * 0.7f, true, 0.65f), Is.False);
        }

        [Test]
        public void DryMovementAndTeleportDoNotProduceFalseSteps()
        {
            var sampler = new WaterRippleStepSampler();
            sampler.Reset(Vector3.zero);
            Assert.That(sampler.Sample(Vector3.forward * 0.4f, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.forward, false, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.forward * 1.3f, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.forward * 10f, true, 0.65f), Is.False);
            Assert.That(sampler.Sample(Vector3.forward * 10.2f, true, 0.65f), Is.False);
        }

        [Test]
        public void ResetIsIdempotent()
        {
            var sampler = new WaterRippleStepSampler();
            sampler.Reset(Vector3.zero);
            sampler.Sample(Vector3.forward * 0.4f, true, 0.65f);
            sampler.Reset(Vector3.zero);
            sampler.Reset(Vector3.zero);
            Assert.That(sampler.Sample(Vector3.forward * 0.4f, true, 0.65f), Is.False);
        }
    }
}
