using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBartenderRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBartenderRulesTests
    {
        [Test]
        public void ServiceWaitsForApproachAndCannotRestartWhileServing()
        {
            var rules = new CabaretBartenderRules(2f, 1f, 3f, 1f, 2f);
            rules.Tick(100f);
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.TryStart(), Is.True);
            rules.Tick(.5f);
            Assert.That(rules.TryStart(), Is.False);
            Assert.That(rules.Elapsed, Is.EqualTo(.5f));
        }

        [Test]
        public void ServicePreservesTimeAcrossPhasesAndFinishesExactlyOnce()
        {
            var rules = new CabaretBartenderRules(2f, 1f, 3f, 1f, 2f);
            rules.TryStart();
            rules.Tick(3.75f);
            Assert.That(rules.State, Is.EqualTo(Phase.Pouring));
            Assert.That(rules.Progress, Is.EqualTo(.25f).Within(.0001f));
            rules.Tick(20f);
            Assert.That(rules.State, Is.EqualTo(Phase.Finished));
            Assert.That(rules.TryStart(), Is.False);
            rules.Tick(20f);
            Assert.That(rules.State, Is.EqualTo(Phase.Finished));
        }

        [TestCase(.5f)]
        [TestCase(2.5f)]
        [TestCase(4f)]
        [TestCase(6.5f)]
        [TestCase(8f)]
        public void ResetDuringAnyStageAllowsOneFreshService(float seconds)
        {
            var rules = new CabaretBartenderRules(2f, 1f, 3f, 1f, 2f);
            rules.TryStart(); rules.Tick(seconds);
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.Elapsed, Is.Zero);
            Assert.That(rules.TryStart(), Is.True);
            Assert.That(rules.Progress, Is.Zero);
        }

        [Test]
        public void PausedTimeCannotAdvanceTheBottleOrPour()
        {
            var rules = new CabaretBartenderRules(2f, 1f, 3f, 1f, 2f);
            rules.TryStart(); rules.Tick(.5f);
            rules.Tick(0f); rules.Tick(-2f);
            Assert.That(rules.Elapsed, Is.EqualTo(.5f));
            Assert.That(rules.State, Is.EqualTo(Phase.Fetching));
        }
    }
}
