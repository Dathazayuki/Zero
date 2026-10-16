using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class DarumaRulesTests
    {
        private static DarumaRules NewRules() => new DarumaRules(120f, 3f, 2.5f, 0.8f, 0.06f);

        [Test]
        public void WaitingDoesNotConsumeTime()
        {
            var rules = NewRules();
            rules.Tick(50f);
            Assert.AreEqual(120f, rules.Remaining);
            Assert.IsFalse(rules.Complete());
        }

        [Test]
        public void MovingAndTurningAllowMovementButWatchingCatchesIt()
        {
            var rules = NewRules();
            rules.Start();
            Assert.IsFalse(rules.ObserveMovement(2f));
            rules.Tick(3f);
            Assert.AreEqual(DarumaRules.Phase.TurningToWatch, rules.State);
            Assert.IsFalse(rules.ObserveMovement(2f));
            rules.Tick(0.8f);
            Assert.IsFalse(rules.ObserveMovement(0.03f));
            Assert.IsTrue(rules.ObserveMovement(0.07f));
            Assert.IsFalse(rules.Catch());
            Assert.IsFalse(rules.Complete());
        }

        [Test]
        public void WatchingEndsAndCycleAllowsRunningAgain()
        {
            var rules = NewRules();
            rules.Start();
            rules.Tick(3f);
            rules.Tick(0.8f);
            rules.Tick(2.5f);
            Assert.AreEqual(DarumaRules.Phase.TurningAway, rules.State);
            rules.Tick(0.8f);
            Assert.AreEqual(DarumaRules.Phase.Moving, rules.State);
        }

        [Test]
        public void TimeoutAndResetAreSafe()
        {
            var rules = NewRules();
            rules.Start();
            rules.Tick(120f);
            Assert.AreEqual(DarumaRules.Phase.Caught, rules.State);
            rules.Reset();
            rules.Reset();
            Assert.AreEqual(DarumaRules.Phase.Waiting, rules.State);
            Assert.AreEqual(120f, rules.Remaining);
        }

        [Test]
        public void CompletionIsConsumedOnce()
        {
            var rules = NewRules();
            rules.Start();
            Assert.IsTrue(rules.Complete());
            Assert.IsFalse(rules.Complete());
            rules.Tick(200f);
            Assert.AreEqual(DarumaRules.Phase.Completed, rules.State);
        }

        private static DarumaRules VariableRules(int seed, float pressure = 0.25f)
        {
            var rules = new DarumaRules(10000f, 3f, 2.5f, 0.8f, 0.06f);
            rules.ConfigureRandomTiming(seed, 0.65f, 1.45f, pressure);
            rules.Start();
            return rules;
        }

        [Test]
        public void SeedReproducesVariableRhythm()
        {
            var first = VariableRules(721);
            var second = VariableRules(721);
            for (int i = 0; i < 40; i++)
            {
                Assert.AreEqual(first.State, second.State);
                Assert.AreEqual(first.PhaseDuration, second.PhaseDuration);
                first.Tick(first.PhaseDuration + 0.001f, 0.5f);
                second.Tick(second.PhaseDuration + 0.001f, 0.5f);
            }
        }

        [Test]
        public void EveryPhaseUsesBoundedRandomDuration()
        {
            var rules = VariableRules(721, 0f);
            for (int i = 0; i < 80; i++)
            {
                float baseline = rules.State == DarumaRules.Phase.Moving ? 3f :
                    rules.State == DarumaRules.Phase.Watching ? 2.5f : 0.8f;
                Assert.That(rules.PhaseDuration, Is.InRange(baseline * 0.65f, baseline * 1.45f));
                rules.Tick(rules.PhaseDuration + 0.001f);
            }
        }

        [Test]
        public void ProgressDoesNotRerollCurrentPhase()
        {
            var rules = VariableRules(5);
            float original = rules.PhaseDuration;
            rules.Tick(0.01f, 0f);
            rules.Tick(0.01f, 1f);
            Assert.AreEqual(original, rules.PhaseDuration);
            Assert.AreEqual(DarumaRules.Phase.Moving, rules.State);
        }

        [Test]
        public void NearFinishShortensMovementAndTurnsButExtendsWatching()
        {
            var beginning = VariableRules(8);
            var ending = VariableRules(8);
            for (int i = 0; i < 12; i++)
            {
                beginning.Tick(beginning.PhaseDuration + 0.001f, 0f);
                ending.Tick(ending.PhaseDuration + 0.001f, 1f);
                if (ending.State == DarumaRules.Phase.Watching)
                    Assert.That(ending.PhaseDuration, Is.GreaterThan(beginning.PhaseDuration));
                else
                    Assert.That(ending.PhaseDuration, Is.LessThan(beginning.PhaseDuration));
            }
        }

        [Test]
        public void FastestTurnStillGivesWarningAndDoesNotCatchEarly()
        {
            var rules = new DarumaRules(120f, 0.1f, 0.1f, 0.1f, 0.06f);
            rules.ConfigureRandomTiming(1, 0.1f, 0.1f, 0.5f);
            rules.Start();
            Assert.That(rules.PhaseDuration, Is.GreaterThanOrEqualTo(0.7f));
            rules.Tick(rules.PhaseDuration, 1f);
            Assert.AreEqual(DarumaRules.Phase.TurningToWatch, rules.State);
            Assert.That(rules.PhaseDuration, Is.EqualTo(0.45f));
            rules.Tick(0.2f, 1f);
            Assert.That(rules.TurnFraction, Is.InRange(0.4f, 0.5f));
            Assert.IsFalse(rules.ObserveMovement(1f));
            rules.Tick(0.25f, 1f);
            Assert.AreEqual(DarumaRules.Phase.Watching, rules.State);
            Assert.IsTrue(rules.ObserveMovement(1f));
        }

        [Test]
        public void ResetClearsPressureAndDoesNotReplayMemorizedRhythm()
        {
            var rules = VariableRules(9);
            float original = rules.PhaseDuration;
            rules.Tick(rules.PhaseDuration, 1f);
            rules.Reset();
            rules.Reset();
            Assert.AreEqual(DarumaRules.Phase.Waiting, rules.State);
            Assert.AreEqual(10000f, rules.Remaining);
            rules.Start();
            Assert.That(rules.PhaseDuration, Is.InRange(1.95f, 4.35f));
            Assert.AreNotEqual(original, rules.PhaseDuration);
        }

        [Test]
        public void InvalidRangeAndPressureAreClamped()
        {
            var rules = NewRules();
            rules.ConfigureRandomTiming(9, -2f, -3f, 5f);
            rules.Start();
            Assert.That(rules.PhaseDuration, Is.EqualTo(0.7f));
            rules.Tick(0.7f, 10f);
            Assert.That(rules.PhaseDuration, Is.EqualTo(0.45f));
        }

        [Test]
        public void DuplicateResetDoesNotConsumeRandomDraws()
        {
            var first = VariableRules(21);
            var second = VariableRules(21);
            first.Reset();
            second.Reset();
            second.Reset();
            first.Start();
            second.Start();
            Assert.AreEqual(first.PhaseDuration, second.PhaseDuration);
        }
    }
}
