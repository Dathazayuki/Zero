using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveHeadRulesTests
    {
        [Test]
        public void WatchingAtTheEndOfGrowthCommitsLungeEvenAfterRetreat()
        {
            var rules = Growing();
            rules.Tick(2.5f, false, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Lunging));
            rules.Tick(5f, false, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Lunging));
        }

        [Test]
        public void LookingAwayDuringGrowthAvoidsTheCommittedAttack()
        {
            var rules = Growing();
            rules.Tick(2.4f, true, true);
            rules.Tick(0.11f, false, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Escaped));
            rules.Tick(10f, true, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Escaped));
        }

        [Test]
        public void DuplicateCatchAndResetLeaveNoStaleProgress()
        {
            var rules = Growing();
            rules.Catch(); rules.Catch();
            rules.Tick(10f, true, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Caught));
            rules.Reset(); rules.Reset();
            Assert.That(rules.Elapsed, Is.Zero);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Waiting));
            rules.Tick(10f, false, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Waiting));
        }

        private static ArchiveHeadRules Growing()
        {
            var rules = new ArchiveHeadRules(1.2f, 0.7f, 2.5f);
            rules.Tick(0f, true, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Rattling));
            rules.Tick(1.21f, true, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Falling));
            rules.Tick(0.71f, true, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveHeadRules.Phase.Growing));
            return rules;
        }
    }
}
