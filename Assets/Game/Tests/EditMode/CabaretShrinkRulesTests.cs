using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretShrinkRulesTests
    {
        [TestCase(0f, .4f)]
        [TestCase(5f, .7f)]
        [TestCase(10f, 1f)]
        [TestCase(50f, 1f)]
        [TestCase(-1f, .4f)]
        public void StageAndEntranceRespectAuthoredSizeLimits(float distance, float expected)
            => Assert.That(CabaretShrinkRules.Evaluate(distance, 10f, .4f), Is.EqualTo(expected).Within(.0001f));

        [Test]
        public void RetracingRouteRestoresSameSizeWithoutAccumulatingShrink()
        {
            float far = CabaretShrinkRules.Evaluate(8f, 10f, .4f);
            float near = CabaretShrinkRules.Evaluate(2f, 10f, .4f);
            Assert.That(near, Is.LessThan(far));
            Assert.That(CabaretShrinkRules.Evaluate(8f, 10f, .4f), Is.EqualTo(far));
        }
    }
}
