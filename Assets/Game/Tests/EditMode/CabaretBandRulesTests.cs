using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBandRulesTests
    {
        [Test]
        public void OnlyCentreStartsMusicAndLeavingCentreDoesNotStopCountdown()
        {
            var rules = new CabaretBandRules(18f);
            rules.Tick(20f, false, false);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.Waiting));
            rules.Tick(0f, true, false);
            rules.Tick(9f, false, false);
            Assert.That(rules.Progress, Is.EqualTo(.5f));
            rules.Tick(9f, false, false);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.DeathRequested));
        }

        [Test]
        public void EscapeIsTerminalAndCannotBeRetriggered()
        {
            var rules = new CabaretBandRules(18f);
            rules.Tick(0f, true, false);
            rules.Tick(12f, false, false);
            rules.Tick(.1f, false, true);
            rules.Tick(100f, true, false);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.Escaped));
        }

        [Test]
        public void DeathIsConsumedOnceAndResetIsIdempotent()
        {
            var rules = new CabaretBandRules(1f);
            rules.Tick(0f, true, false);
            rules.Tick(2f, true, false);
            rules.Tick(1f, false, true);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.DeathRequested));
            rules.Reset(); rules.Reset();
            Assert.That(rules.Progress, Is.Zero);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.Waiting));
            rules.Tick(0f, true, true);
            Assert.That(rules.State, Is.EqualTo(CabaretBandRules.Phase.Waiting));
        }
    }
}
