using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class SeatedFatherRulesTests
    {
        [Test]
        public void RetreatDuringWarningAvoidsTheAttack()
        {
            var rules = new SeatedFatherRules(2.5f);
            rules.Tick(0f, true, true);
            rules.Tick(2.4f, true, true);
            Assert.That(rules.State, Is.EqualTo(FatherState.Warning));
            rules.Tick(0.2f, false, false);
            Assert.That(rules.State, Is.EqualTo(FatherState.Returning));
            rules.Returned();
            Assert.That(rules.State, Is.EqualTo(FatherState.Sitting));
        }

        [Test]
        public void AttackHasWarningAndRiseThenEndsWithoutAnUnlimitedChase()
        {
            var rules = new SeatedFatherRules(2.5f);
            rules.Tick(0f, true, true);
            rules.Tick(2.5f, true, true);
            Assert.That(rules.State, Is.EqualTo(FatherState.Rising));
            rules.Tick(0.7f, true, true);
            Assert.That(rules.State, Is.EqualTo(FatherState.Lunging));
            rules.Tick(0.65f, false, false);
            Assert.That(rules.State, Is.EqualTo(FatherState.Returning));
        }

        [Test]
        public void CatchIsConsumedAndResetIsIdempotent()
        {
            var rules = new SeatedFatherRules(2.5f);
            rules.Catch();
            rules.Tick(20f, true, true);
            Assert.That(rules.State, Is.EqualTo(FatherState.Caught));
            rules.Reset();
            rules.Reset();
            Assert.That(rules.State, Is.EqualTo(FatherState.Sitting));
            Assert.That(rules.Elapsed, Is.Zero);
            rules.Tick(10f, false, false);
            Assert.That(rules.State, Is.EqualTo(FatherState.Sitting));
        }
    }
}
