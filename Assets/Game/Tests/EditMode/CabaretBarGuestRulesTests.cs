using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretBarGuestRules.Phase;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBarGuestRulesTests
    {
        [Test]
        public void MustSeeGuestThenLookAwayContinuouslyBeforeReturning()
        {
            var rules = new CabaretBarGuestRules();
            Assert.That(rules.Advance(10, false), Is.False); Assert.That(rules.State, Is.EqualTo(Phase.Unseen));
            rules.Advance(0, true); rules.Advance(.2f, false); rules.Advance(0, true);
            rules.Advance(.2f, false); Assert.That(rules.State, Is.EqualTo(Phase.Watching));
            rules.Advance(.11f, false); Assert.That(rules.State, Is.EqualTo(Phase.Armed));
            Assert.That(rules.Advance(1, false), Is.False);
            Assert.That(rules.Advance(0, true), Is.True);
            Assert.That(rules.Advance(100, true), Is.False);
        }

        [Test]
        public void ResetCancelsArmedAndVanishedState()
        {
            var rules = new CabaretBarGuestRules(); rules.Advance(0, true); rules.Advance(.31f, false);
            rules.Reset(); rules.Reset(); Assert.That(rules.State, Is.EqualTo(Phase.Unseen));
            rules.Advance(0, true); rules.Advance(.31f, false); rules.Advance(0, true); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(Phase.Unseen));
        }
    }
}
