using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class TelevisionMotherRulesTests
    {
        [Test]
        public void MotherKeepsRunningWhenNearInsteadOfEnteringALunge()
        {
            var rules = new TelevisionMotherRules(3f, 1.4f, 20f);
            rules.Tick(2.9f, true, true);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Warning));
            rules.Tick(0.2f, true, true);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Emerging));
            rules.Tick(1.5f, true, true);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Chasing));
            rules.Tick(0.1f, true, true);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Chasing));
            rules.Tick(0.9f, true, false);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Chasing));
        }

        [Test]
        public void LeavingRoomOrChaseTimeoutEndsTheThreat()
        {
            var rules = new TelevisionMotherRules(3f, 1f, 2f);
            rules.Tick(0.1f, false, false);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Finished));
            rules.Reset();
            rules.Tick(3f, true, false);
            rules.Tick(1f, true, false);
            rules.Tick(2.1f, true, false);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Finished));
        }

        [Test]
        public void CatchBlocksFurtherTransitionsAndResetCanRepeat()
        {
            var rules = new TelevisionMotherRules(3f, 1f, 20f);
            rules.Catch();
            rules.Tick(50f, false, false);
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Caught));
            rules.Reset();
            rules.Reset();
            Assert.That(rules.Phase, Is.EqualTo(TelevisionMotherRules.PhaseState.Warning));
            Assert.That(rules.Elapsed, Is.Zero);
        }
    }
}
