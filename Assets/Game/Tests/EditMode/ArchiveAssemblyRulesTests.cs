using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveAssemblyRulesTests
    {
        [Test]
        public void EveryAssemblyStagePrecedesTheChaseAndTheChaseDoesNotTimeOut()
        {
            var rules = new ArchiveAssemblyRules(1f, 5f, 2f);
            rules.Tick(1f);
            Assert.That(rules.State, Is.EqualTo(ArchiveAssemblyRules.Phase.CrawlingTogether));
            rules.Tick(5f);
            Assert.That(rules.State, Is.EqualTo(ArchiveAssemblyRules.Phase.Assembling));
            rules.Tick(1f);
            Assert.That(rules.Progress, Is.EqualTo(0.5f));
            rules.Tick(1f);
            rules.Tick(1000f);
            Assert.That(rules.State, Is.EqualTo(ArchiveAssemblyRules.Phase.Chasing));
        }

        [Test]
        public void CaptureStopsProgressAndResetRestartsFromScatteredParts()
        {
            var rules = new ArchiveAssemblyRules(1f, 5f, 2f);
            rules.Tick(1f);
            rules.Catch(); rules.Tick(100f);
            Assert.That(rules.State, Is.EqualTo(ArchiveAssemblyRules.Phase.Caught));
            rules.Reset(); rules.Reset();
            Assert.That(rules.State, Is.EqualTo(ArchiveAssemblyRules.Phase.Scattered));
            Assert.That(rules.Progress, Is.Zero);
        }
    }
}
