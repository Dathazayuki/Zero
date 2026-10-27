using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretAudienceRulesTests
    {
        [Test]
        public void MustEnterStageFaceForwardThenTurnAndOnlyRevealOnce()
        {
            var rules=new CabaretAudienceRules();
            Assert.That(rules.Advance(false,true,true),Is.False);
            Assert.That(rules.Advance(true,false,true),Is.False);
            Assert.That(rules.Advance(true,false,true),Is.False);
            Assert.That(rules.Advance(true,true,false),Is.False);
            Assert.That(rules.Advance(true,false,false),Is.False);
            Assert.That(rules.Advance(true,false,true),Is.True);
            Assert.That(rules.Advance(true,false,true),Is.False);
            rules.Reset(); rules.Reset();
            Assert.That(rules.HasRevealed,Is.False);
            Assert.That(rules.Advance(false,false,true),Is.False);
        }
    }
}
