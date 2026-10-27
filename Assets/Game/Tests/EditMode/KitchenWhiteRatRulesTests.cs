using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenWhiteRatRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenWhiteRatRulesTests
    {
        [Test]
        public void RatExitsBeyondFallenHeadInsteadOfWalkingBackIntoTorso()
        {
            var hips = new UnityEngine.Vector3(0f, 0.3f, 0f);
            var head = new UnityEngine.Vector3(0f, 0.2f, -0.8f);
            var outward = KitchenWhiteRatActor.ExitDirection(head, hips, UnityEngine.Vector3.forward);
            Assert.That(UnityEngine.Vector3.Dot(outward, head - hips), Is.GreaterThan(0f));
            Assert.That(outward.y, Is.Zero);
        }
        [Test]
        public void DeathMustFinishBeforeHatAndRatAndOnlyNearbyIdleTriggersAttack()
        {
            var rules = new KitchenWhiteRatRules();
            rules.Tick(1f, false, false, true);
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            rules.Tick(0f, true, false, true);
            rules.Tick(20f, false, false, true);
            Assert.That(rules.State, Is.EqualTo(Phase.Falling));
            rules.Tick(0f, false, true, false);
            rules.Tick(1f, false, true, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Walking));
            rules.Tick(2f, false, true, false);
            rules.Tick(20f, false, true, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Idle));
            rules.Tick(0f, false, true, true);
            rules.Tick(0.45f, false, true, false);
            rules.Tick(1.2f, false, true, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.Elapsed, Is.Zero);
        }
    }
}
