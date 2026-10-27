using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenButcherChaseRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenButcherChaseRulesTests
    {
        [Test]
        public void WaitsForMiddleTriggerThenFacesAndLiftsBeforeDeath()
        {
            var rules = new KitchenButcherChaseRules(0.35f, 1.2f);
            rules.Tick(5f, false, false); Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.Catch(), Is.False);
            rules.Tick(0f, true, true); Assert.That(rules.State, Is.EqualTo(Phase.Chasing));
            Assert.That(rules.Catch(), Is.True); Assert.That(rules.Catch(), Is.False);
            rules.Tick(0.2f, true, false); Assert.That(rules.State, Is.EqualTo(Phase.Facing));
            rules.Tick(0.2f, false, false); Assert.That(rules.State, Is.EqualTo(Phase.Lifting));
            rules.Tick(0.6f, false, false); Assert.That(rules.LiftProgress, Is.EqualTo(0.5f).Within(0.001f));
            rules.Tick(0.61f, false, false); Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Tick(10f, true, true); Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Reset(); rules.Reset(); Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
        }

        [Test]
        public void LeavingRoomEndsChaseAndResetDuringLiftIsSafe()
        {
            var rules = new KitchenButcherChaseRules(0.35f, 1.2f);
            rules.Tick(0f, true, true); rules.Tick(0f, false, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Escaped)); Assert.That(rules.Catch(), Is.False);
            rules.Tick(5f, true, true); Assert.That(rules.State, Is.EqualTo(Phase.Escaped));
            rules.Reset(); rules.Tick(0f, true, true); rules.Catch(); rules.Tick(1f, false, true);
            rules.Reset(); Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.LiftProgress, Is.Zero);
        }
    }
}
