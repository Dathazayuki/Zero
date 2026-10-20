using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class FamilyRugRulesTests
    {
        [Test]
        public void LeavingRugDiscardsWarningProgress()
        {
            var rules = new FamilyRugRules(3f);
            rules.Tick(2.9f, true);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Warning));
            rules.Tick(0.1f, false);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Waiting));
            rules.Tick(1f, true);
            Assert.That(rules.Progress, Is.EqualTo(1f / 3f).Within(0.001f));
        }

        [Test]
        public void FullWarningWaitsForTheAcceptedDeathCallback()
        {
            var rules = new FamilyRugRules(3f);
            rules.Tick(3f, true);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Grabbing));
            rules.Tick(0.6f, true);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Grabbing));
            rules.Catch();
            rules.Tick(6f, true);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Caught));
        }

        [Test]
        public void CatchCannotBeCancelledByLeavingAndResetCanRepeat()
        {
            var rules = new FamilyRugRules(3f);
            rules.Catch();
            rules.Tick(100f, false);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Caught));
            rules.Reset();
            rules.Reset();
            Assert.That(rules.Progress, Is.Zero);
            Assert.That(rules.Phase, Is.EqualTo(FamilyRugRules.PhaseState.Waiting));
        }
    }
}
