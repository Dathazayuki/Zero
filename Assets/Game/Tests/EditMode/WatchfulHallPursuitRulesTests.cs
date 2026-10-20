using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.Editor.CodePolicy;

namespace Zero.Tests.EditMode
{
    public sealed class WatchfulHallPursuitRulesTests
    {
        [TestCase(0f, 75f, 15f, 0f)]
        [TestCase(5f, 75f, 15f, 75f)]
        [TestCase(15f, 75f, 15f, -75f)]
        [TestCase(20f, 75f, 15f, 0f)]
        [TestCase(100f, 0f, 15f, 0f)]
        [TestCase(100f, 75f, 0f, 0f)]
        public void WatcherNeckSweepReversesAtLimits(float seconds, float arc, float speed, float expected)
        {
            Assert.That(WatchfulHallPursuitRules.WatcherScanYaw(seconds, arc, speed), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void WatcherConeRejectsPlayerOutsideLightDespiteMatchingHorizontalBearing()
        {
            Vector3 origin = new Vector3(0f, 8.5f, 0f);
            Vector3 forward = Quaternion.Euler(50f, 0f, 0f) * Vector3.forward;
            Vector3 inside = new Vector3(0f, 0.05f, 10f);
            Vector3 outside = new Vector3(0f, 0.05f, 60f);
            Assert.IsTrue(WatchfulHallPursuitRules.IsInsideViewCone(origin, forward, inside, 65f, 70f));
            Assert.IsFalse(WatchfulHallPursuitRules.IsInsideViewCone(origin, forward, outside, 65f, 70f));
            Assert.AreEqual(0f, Vector3.Angle(Vector3.forward, Vector3.ProjectOnPlane(outside - origin, Vector3.up)));
        }

        [TestCase(0f, 0f, 10f, true)]
        [TestCase(0f, 0f, -10f, false)]
        [TestCase(20f, 0f, 10f, false)]
        [TestCase(0f, 0f, 66f, false)]
        [TestCase(0f, 20f, 10f, false)]
        public void WatcherConeChecksBothAnglesAndRange(float x, float y, float z, bool expected)
        {
            Assert.AreEqual(expected, WatchfulHallPursuitRules.IsInsideViewCone(
                Vector3.zero, Vector3.forward, new Vector3(x, y, z), 65f, 88f));
        }

        [Test]
        public void WatcherReportInvestigatesButDoesNotGrantSightOrOverwriteDirectChase()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Report(Vector3.right);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, rules.State);
            rules.Observe(Vector3.forward);
            rules.Report(Vector3.left, true);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, rules.State);
            Assert.AreEqual(Vector3.forward, rules.LastKnownPosition);
        }

        [Test]
        public void RadioReportImmediatelyRetargetsAChaseThatHasLostDirectSight()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Observe(Vector3.forward);
            rules.Tick(false, false, 0.2f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, rules.State);
            rules.Report(Vector3.right, false);
            Assert.AreEqual(Vector3.right, rules.LastKnownPosition);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, rules.State);
        }

        [Test]
        public void LosingSightKeepsLastPositionSearchesAndReturnsToPatrol()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Observe(Vector3.forward);
            rules.Tick(false, false, 0.81f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, rules.State);
            Assert.AreEqual(Vector3.forward, rules.LastKnownPosition);
            rules.Tick(false, true, 0.1f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Searching, rules.State);
            rules.Tick(false, false, 8f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Patrolling, rules.State);
        }

        [Test]
        public void SeeingPlayerAgainInterruptsSearchAndResetClearsKnowledge()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Report(Vector3.right);
            rules.Tick(false, true, 0.1f);
            rules.Observe(Vector3.back);
            rules.Tick(true, false, 20f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, rules.State);
            rules.Reset();
            rules.Reset();
            Assert.AreEqual(Vector3.zero, rules.LastKnownPosition);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Patrolling, rules.State);
        }

        [Test]
        public void UnreachableInvestigationTimesOutInsteadOfCampingForever()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Report(Vector3.right);
            rules.Tick(false, false, 21f);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Patrolling, rules.State);
        }

        [TestCase("WatchfulHallPursuitRules")]
        [TestCase("WatchfulHallHidingSpots")]
        [TestCase("WatchfulHallWatcher")]
        [TestCase("WatchfulHallPatrol")]
        [TestCase("WatchfulHallThreatDirector")]
        [TestCase("WatchfulHallSession")]
        public void NewAIHasNoPolicyDebt(string name)
        {
            string path = "Assets/Game/Scripts/Flow/Campaign/" + name + ".cs";
            Assert.IsFalse(CodePolicyScanner.AnalyzeFile(System.IO.Path.GetFullPath(path), path).HasDebt, name);
        }
    }
}
