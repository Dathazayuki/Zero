using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBlinkRulesTests
    {
        [Test]
        public void OnlyStageStartsDarknessAndStepsOnlyOccurAtDarkEntry()
        {
            var rules = new CabaretBlinkRules(.2f, 1f);
            Assert.That(rules.Tick(10f, false, false), Is.False);
            Assert.That(rules.Tick(0f, true, false), Is.True);
            Assert.That(rules.Tick(.1f, false, false), Is.False);
            Assert.That(rules.IsDark, Is.True);
            Assert.That(rules.Tick(.11f, false, false), Is.False);
            Assert.That(rules.IsDark, Is.False);
            Assert.That(rules.Tick(1.1f, false, false), Is.True);
        }

        [Test]
        public void EscapeStopsAllFutureStepsAndResetReturnsToWaiting()
        {
            var rules = new CabaretBlinkRules(.2f, 1f);
            rules.Tick(0f, true, false);
            rules.Tick(.1f, false, true);
            Assert.That(rules.State, Is.EqualTo(CabaretBlinkRules.Phase.Escaped));
            Assert.That(rules.Tick(10f, true, false), Is.False);
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(CabaretBlinkRules.Phase.Waiting));
        }
    }
}
