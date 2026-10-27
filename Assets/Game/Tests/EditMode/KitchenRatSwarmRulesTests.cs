using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenRatSwarmRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenRatSwarmRulesTests
    {
        [Test]
        public void WarningRemainsActiveUntilFullLeadInBeforeRatsRun()
        {
            var rules = new KitchenRatSwarmRules();
            rules.Tick(0f, true, false, false);
            rules.Tick(0.79f, false, false, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            rules.Tick(0.02f, false, false, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Running));
            rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
        }
        [Test]
        public void RatFacesHeadFirstDownwardsDuringVerticalDrop()
        {
            Quaternion fall = KitchenRatSwarmActor.MovementRotation(Vector3.down, Vector3.right);
            Assert.That(Vector3.Dot(fall * Vector3.forward, Vector3.down), Is.GreaterThan(0.999f));
            var diagonal = new Vector3(-1f, -2f, 0f).normalized;
            Quaternion dive = KitchenRatSwarmActor.MovementRotation(diagonal, Vector3.right);
            Assert.That(Vector3.Dot(dive * Vector3.forward, diagonal), Is.GreaterThan(0.999f));
        }
        [Test]
        public void HoodDropStartsAtMouthLandsOnStoveAndJumpEndsOnFloor()
        {
            var mouth = new Vector3(3.4f, 2.3f, 1.3f);
            var stove = new Vector3(3.35f, 1.4f, 1.3f);
            var floor = new Vector3(2.2f, 0.015f, 1.1f);
            Assert.That(KitchenRatSwarmActor.DropPosition(mouth, stove, 0f), Is.EqualTo(mouth));
            Assert.That(Vector3.Distance(KitchenRatSwarmActor.DropPosition(mouth, stove, 1f), stove), Is.LessThan(0.00001f));
            Assert.That(Vector3.Distance(KitchenRatSwarmActor.DropPosition(stove, floor, 1f), floor), Is.LessThan(0.00001f));
            Assert.That(KitchenRatSwarmActor.DropPosition(stove, floor, 0.5f).y, Is.GreaterThan((stove.y + floor.y) * 0.5f));
        }
        [Test]
        public void TouchStartsOneCollapseThenTramplingBeforeDeathAndResetIsRepeatable()
        {
            var rules = new KitchenRatSwarmRules();
            rules.Tick(10f, false, true, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            rules.Tick(0f, true, false, false); rules.Tick(0.8f, false, false, false);
            Assert.That(rules.State, Is.EqualTo(Phase.Running));
            rules.Tick(0f, false, true, true);
            Assert.That(rules.State, Is.EqualTo(Phase.Falling));
            rules.Tick(0.65f, false, true, true);
            Assert.That(rules.State, Is.EqualTo(Phase.Trampling));
            rules.Tick(1.5f, false, true, true);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
        }
        [Test]
        public void SegmentContactCatchesFastCrossingButNotRatsHighAbovePlayer()
        {
            Assert.That(KitchenRatSwarmActor.Touches(Vector3.left * 2f, Vector3.right * 2f, Vector3.zero, 0.5f), Is.True);
            Assert.That(KitchenRatSwarmActor.Touches(Vector3.up * 2f, Vector3.up * 3f, Vector3.zero, 0.5f), Is.False);
        }
    }
}
