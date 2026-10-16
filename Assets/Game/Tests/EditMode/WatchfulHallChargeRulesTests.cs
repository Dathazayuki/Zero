using NUnit.Framework;
using Zero.Editor.CodePolicy;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class WatchfulHallChargeRulesTests
    {
        [TestCase("WatchfulHallChargeRules")]
        [TestCase("WatchfulHallChargePad")]
        [TestCase("WatchfulHallExitGate")]
        [TestCase("WatchfulHallSession")]
        public void NewObjectiveCodeDoesNotAddPolicyDebt(string className)
        {
            string path = "Assets/Game/Scripts/Flow/Campaign/" + className + ".cs";
            CodePolicyMetrics metrics = CodePolicyScanner.AnalyzeFile(System.IO.Path.GetFullPath(path), path);
            Assert.IsFalse(metrics.HasDebt, className + " must stay within the unchanged code-policy limits.");
        }

        [Test]
        public void ChargingFeedbackOnlyTargetsSettledPadAndStopsOnPauseOrCompletion()
        {
            var rules = new WatchfulHallChargeRules();
            rules.Tick(0, true, 1.4f);
            Assert.IsFalse(rules.IsChargingPad(0));
            rules.Tick(0, true, 0.2f);
            Assert.IsTrue(rules.IsChargingPad(0));
            Assert.IsFalse(rules.IsChargingPad(1));
            rules.Pause();
            Assert.IsFalse(rules.IsChargingPad(0));
            rules.Tick(0, true, 16.5f);
            Assert.IsFalse(rules.IsChargingPad(0));
        }

        [Test]
        public void SettlingIsNotPartOfFifteenSecondCharge()
        {
            var rules = new WatchfulHallChargeRules();
            rules.Tick(0, true, 1.5f);
            Assert.AreEqual(0f, rules.Progress(0));
            rules.Tick(0, true, 14.9f);
            Assert.IsFalse(rules.IsCharged(0));
            rules.Tick(0, true, 0.11f);
            Assert.IsTrue(rules.IsCharged(0));
            Assert.AreEqual(15f, rules.Progress(0));
        }

        [TestCase(-1, true)]
        [TestCase(0, false)]
        public void LeavingOrMovingPreservesChargeButRequiresSettlingAgain(int pad, bool stationary)
        {
            var rules = new WatchfulHallChargeRules();
            rules.Tick(0, true, 6.5f);
            rules.Tick(pad, stationary, 2f);
            Assert.AreEqual(5f, rules.Progress(0));
            rules.Tick(0, true, 1.5f);
            Assert.AreEqual(5f, rules.Progress(0));
            rules.Tick(0, true, 10f);
            Assert.IsTrue(rules.IsCharged(0));
        }

        [Test]
        public void EnterExitSpamNeverCharges()
        {
            var rules = new WatchfulHallChargeRules();
            for (int i = 0; i < 30; i++)
            {
                rules.Tick(0, true, 1.4f);
                rules.Tick(-1, true, 0.1f);
            }
            Assert.AreEqual(0f, rules.Progress(0));
        }

        [Test]
        public void SwitchingPadsDoesNotTransferSettlingOrProgress()
        {
            var rules = new WatchfulHallChargeRules();
            rules.Tick(0, true, 2.5f);
            rules.Tick(1, true, 1f);
            Assert.AreEqual(0f, rules.Progress(1));
            rules.Tick(0, true, 1f);
            Assert.AreEqual(1f, rules.Progress(0));
        }

        [Test]
        public void FourDistinctPadsUnlockAndCompletionIsConsumedOnce()
        {
            var rules = new WatchfulHallChargeRules();
            Assert.IsFalse(rules.Complete());
            for (int i = 3; i > 0; i--) rules.Tick(i, true, 16.5f);
            Assert.IsFalse(rules.IsUnlocked);
            rules.Tick(1, true, 100f);
            Assert.IsFalse(rules.IsUnlocked);
            rules.Tick(0, true, 16.5f);
            Assert.IsTrue(rules.IsUnlocked);
            Assert.IsTrue(rules.Complete());
            Assert.IsFalse(rules.Complete());
            rules.Reset();
            rules.Reset();
            Assert.IsFalse(rules.IsUnlocked);
            Assert.IsFalse(rules.IsCompleted);
            for (int i = 0; i < 4; i++) Assert.AreEqual(0f, rules.Progress(i));
        }

        [Test]
        public void PauseRetainsProgressAndDiscardsSettling()
        {
            var rules = new WatchfulHallChargeRules();
            rules.Tick(0, true, 4f);
            rules.Pause();
            rules.Tick(0, true, 1.5f);
            Assert.AreEqual(2.5f, rules.Progress(0));
            rules.Tick(0, true, -100f);
            rules.Tick(0, true, float.NaN);
            Assert.AreEqual(2.5f, rules.Progress(0));
        }
    }
}
