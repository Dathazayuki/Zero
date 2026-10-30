using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretRitualRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretRitualRulesTests
    {
        [Test]
        public void EntryRunsBlackoutThenRisingBeforeTracking()
        {
            var rules = NewRules();
            rules.Tick(20f, false, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            rules.Tick(0f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Blackout));
            rules.Tick(1f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Rising));
            rules.Tick(1f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Active));
            Assert.That(rules.StabilizedMask, Is.Zero);
        }

        [Test]
        public void LightIsHarmlessUntilRedStrikeArrivesAndMissRestartsCycle()
        {
            var rules = ActiveRules();
            AdvanceTracking(rules);
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            rules.Tick(1.25f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Attacking));
            rules.Tick(.24f, true, true, 7);
            Assert.That(rules.StabilizedMask, Is.Zero);
            rules.Tick(.02f, true, false, 0);
            rules.Tick(.35f, true, false, 0);
            Assert.That(rules.State, Is.EqualTo(Phase.Recovery));
            rules.Tick(.8f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Active));
        }

        [Test]
        public void HitsAreIdempotentAndEveryEyeIncreasesDifficulty()
        {
            var rules = ActiveRules();
            Strike(rules, 1);
            Assert.That(rules.StabilizedMask, Is.EqualTo(1));
            Assert.That(rules.Difficulty, Is.EqualTo(1.4f));
            rules.Tick(.1f, true, false, 1);
            Assert.That(rules.DefeatedCount, Is.EqualTo(1));
            rules.Tick(1f, true, false, 0); rules.Tick(.8f, true, false, 0);
            AdvanceTracking(rules);
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            rules.Tick(1.1f, true, false, 0); rules.Tick(.3f, true, false, 2);
            Assert.That(rules.DefeatedCount, Is.EqualTo(2));
            Assert.That(rules.Difficulty, Is.EqualTo(1.8f));
        }

        [Test]
        public void ThreeHitsReleaseHostAndResetClearsAllProgress()
        {
            var rules = ActiveRules();
            Strike(rules, 7);
            rules.Tick(.31f, true, false, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Releasing));
            rules.Tick(2.5f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Safe));
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Waiting));
            Assert.That(rules.Difficulty, Is.EqualTo(1f));
        }

        [Test]
        public void PlayerInsideStrikeDiesEvenWhenTargetIsAlsoHitAndCannotAdvanceAgain()
        {
            var rules = ActiveRules();
            AdvanceTracking(rules); rules.Tick(1.25f, true, false, 0);
            rules.Tick(.3f, true, true, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            rules.Tick(99f, true, false, 7);
            Assert.That(rules.State, Is.EqualTo(Phase.Caught));
            Assert.That(rules.StabilizedMask, Is.Zero);
        }

        [TestCase(Phase.Active, 0, false)]
        [TestCase(Phase.Active, 1, true)]
        [TestCase(Phase.Active, 2, true)]
        [TestCase(Phase.Active, 4, true)]
        [TestCase(Phase.Active, 3, true)]
        [TestCase(Phase.Warning, 1, false)]
        [TestCase(Phase.Attacking, 1, false)]
        [TestCase(Phase.Recovery, 1, false)]
        [TestCase(Phase.Releasing, 7, false)]
        public void EyesFleeOnlyAfterFirstHitDuringTracking(Phase phase, int mask, bool expected)
            => Assert.That(CabaretRitualRules.CanEyesFlee(phase, mask), Is.EqualTo(expected));

        [TestCase(0f, 6.25f)]
        [TestCase(8f, 8.5f)]
        [TestCase(9f, 9f)]
        [TestCase(100f, 9f)]
        public void DistantEyeDoesNotRequireAnEvenFurtherDestination(float distanceSquared, float expected)
            => Assert.That(CabaretRitualRules.MinimumEscapeDistanceSquared(distanceSquared), Is.EqualTo(expected));

        [TestCase(0, 1f)]
        [TestCase(1, 1f)]
        [TestCase(2, 1f)]
        [TestCase(4, 1f)]
        [TestCase(3, 1.5f)]
        [TestCase(5, 1.5f)]
        [TestCase(6, 1.5f)]
        public void OnlyLastSurvivingEyeGetsSpeedBoost(int defeatedMask, float expected)
            => Assert.That(CabaretRitualRules.EyeSpeedMultiplier(defeatedMask), Is.EqualTo(expected));

        [Test]
        public void TrackingDurationVariesWithinConfiguredRangeAndCanBeSeeded()
        {
            var rules = NewRules();
            rules.SetRandomSeed(1234);
            EnterTracking(rules);
            var durations = new float[6];
            for (int i = 0; i < durations.Length; i++)
            {
                durations[i] = AdvanceTracking(rules);
                if (i < durations.Length - 1) AdvanceToNextTracking(rules);
            }
            Assert.That(durations, Has.All.GreaterThanOrEqualTo(4.5f));
            Assert.That(durations, Has.All.LessThanOrEqualTo(7.55f));
            Assert.That(HasDifferentValues(durations), Is.True);
        }

        private static bool HasDifferentValues(float[] values)
        {
            for (int i = 1; i < values.Length; i++)
                if (values[i] != values[0]) return true;
            return false;
        }

        private static void AdvanceToNextTracking(CabaretRitualRules rules)
        {
            rules.Tick(1.25f, true, false, 0);
            rules.Tick(.6f, true, false, 0);
            rules.Tick(.8f, true, false, 0);
        }

        private static float AdvanceTracking(CabaretRitualRules rules)
        {
            float elapsed = 0f;
            while (rules.State == Phase.Active && elapsed < 8f)
            { rules.Tick(.05f, true, false, 0); elapsed += .05f; }
            Assert.That(rules.State, Is.EqualTo(Phase.Warning));
            return elapsed;
        }

        private static void Strike(CabaretRitualRules rules, int mask)
        {
            AdvanceTracking(rules); rules.Tick(1.25f, true, false, 0);
            rules.Tick(.3f, true, false, mask);
        }

        private static CabaretRitualRules NewRules()
        {
            var rules = new CabaretRitualRules(1f, 1f, 6f, 1.25f, 2.5f);
            rules.SetTrackingVariation(1.5f);
            return rules;
        }
        private static CabaretRitualRules ActiveRules()
        {
            var rules = NewRules();
            EnterTracking(rules);
            return rules;
        }

        private static void EnterTracking(CabaretRitualRules rules)
        {
            rules.Tick(0f, true, false, 0); rules.Tick(1f, true, false, 0); rules.Tick(1f, true, false, 0);
        }
    }
}
