using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretCurtainCallRulesTests
    {
        [Test]
        public void FieldWaitsForApproachAndBowThenContactIsConsumedOnce()
        {
            var rules = new CabaretCurtainCallRules(2f, .3f, 18f);
            rules.Tick(100f, false, true, 0f, false);
            Assert.That(rules.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Waiting));
            rules.Tick(0f, true, false, 4f, false);
            rules.Tick(10f, true, false, 4f, false);
            Assert.That(rules.Radius, Is.Zero);
            rules.Tick(.1f, true, true, 4f, false);
            Assert.That(rules.Radius, Is.EqualTo(.5f).Within(.001f));
            rules.Tick(1f, false, true, 1f, false);
            Assert.That(rules.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Caught));
            float radius = rules.Radius;
            rules.Tick(100f, true, true, 100f, true);
            Assert.That(rules.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Caught));
            Assert.That(rules.Radius, Is.EqualTo(radius));
        }

        [Test]
        public void LeavingRoomStopsExpansionAndResetAllowsAnotherAttempt()
        {
            var rules = new CabaretCurtainCallRules(2f, .3f, 18f);
            rules.Tick(0f, true, false, 4f, false);
            rules.Tick(0f, false, false, 4f, true);
            rules.Tick(100f, true, true, 0f, false);
            Assert.That(rules.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Escaped));
            rules.Reset(); rules.Reset();
            rules.Tick(0f, true, false, 4f, false);
            Assert.That(rules.State, Is.EqualTo(CabaretCurtainCallRules.Phase.Bowing));
        }

        [Test]
        public void FieldTouchesCapsuleSurfaceBeforeReachingPlayerPivot()
        {
            Vector3 a = new Vector3(3f, .3f, 0f), b = new Vector3(3f, 1.5f, 0f);
            float distance = CabaretCurtainCallRules.DistanceToCapsule(Vector3.up, a, b, .3f);
            Assert.That(distance, Is.EqualTo(2.7f).Within(.001f));
        }
    }
}
