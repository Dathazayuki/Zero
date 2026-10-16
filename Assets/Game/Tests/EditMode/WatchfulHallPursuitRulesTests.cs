using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.Editor.CodePolicy;

namespace Zero.Tests.EditMode
{
    public sealed class WatchfulHallPursuitRulesTests
    {
        [Test]
        public void WatcherReportInvestigatesButDoesNotGrantSightOrOverwriteDirectChase()
        {
            var rules = new WatchfulHallPursuitRules();
            rules.Report(Vector3.right);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Investigating, rules.State);
            rules.Observe(Vector3.forward);
            rules.Report(Vector3.left);
            Assert.AreEqual(WatchfulHallPursuitRules.Phase.Chasing, rules.State);
            Assert.AreEqual(Vector3.forward, rules.LastKnownPosition);
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
