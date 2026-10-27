using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBlackHoleRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBlackHoleRulesTests
    {
        private static CabaretBlackHoleRules Create() => new CabaretBlackHoleRules(2.26f, 1.1f, 1.2f, 2f, 1.15f);

        private static void Snap(CabaretBlackHoleRules rules)
        {
            rules.Tick(0, true, true, 0);
            rules.Tick(0, false, true, 2.26f);
            rules.Tick(0, false, true, 1.1f);
        }

        [Test]
        public void RequiresApproachThenStandAndActualSnapTime()
        {
            var rules = Create(); rules.Tick(100, false, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            rules.Tick(0, true, true, 0);
            rules.Tick(10, true, true, 2.2f);
            Assert.That(rules.State, Is.EqualTo(Phase.Rising));
            rules.Tick(0, false, true, 2.26f);
            Assert.That(rules.State, Is.EqualTo(Phase.Snapping));
            rules.Tick(10, true, true, 1.09f);
            Assert.That(rules.State, Is.EqualTo(Phase.Snapping));
            rules.Tick(0, false, true, 1.1f);
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            Assert.That(rules.Elapsed, Is.Zero);
        }

        [Test]
        public void StayingInWarningDoesNotKillUntilItOpensThenFallsBeforeDeath()
        {
            var rules = Create(); Snap(rules);
            rules.Tick(1.19f, false, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            rules.Tick(.02f, false, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Falling));
            rules.Tick(1.14f, false, false, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Falling));
            rules.Tick(.02f, false, false, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Tick(100, true, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
        }

        [Test]
        public void DodgeSurvivesButReturningWhileOpenFalls()
        {
            var rules = Create(); Snap(rules); rules.Tick(1.2f, false, false, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Open));
            rules.Tick(.5f, false, false, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Open));
            rules.Tick(0, false, true, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Falling));
        }

        [Test]
        public void ClosedHoleCannotCatchAndResetCancelsEveryPhase()
        {
            var rules = Create(); Snap(rules); rules.Tick(1.2f, false, false, 0);
            rules.Tick(2, true, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Finished));
            rules.Tick(100, true, true, 100);
            Assert.That(rules.State, Is.EqualTo(Phase.Finished));
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting)); Assert.That(rules.Elapsed, Is.Zero);
            Snap(rules); rules.Cancel();
            Assert.That(rules.State, Is.EqualTo(Phase.Finished));
            rules.Reset(); Snap(rules); rules.Tick(2, false, true, 0); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting)); Assert.That(rules.FallProgress, Is.Zero);
        }
    }
}
