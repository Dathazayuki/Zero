using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveCabinetCrushRulesTests
    {
        [Test]
        public void WarningPrecedesOneStrikeAndAPlayerCanDodgeAfterTriggeringIt()
        {
            var rules = new ArchiveCabinetCrushRules(0.9f, 0.55f);
            rules.Tick(5f, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Waiting));
            rules.Tick(0f, true);
            rules.Tick(0.8f, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Warning));
            rules.Tick(0.11f, false);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Lunging));
            rules.Tick(0.6f, false);
            rules.Tick(10f, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Spent));
        }

        [Test]
        public void CaptureAndResetConsumeFurtherAdvanceAndRearmTheEncounter()
        {
            var rules = new ArchiveCabinetCrushRules(1f, 0.5f);
            rules.Catch();
            rules.Tick(100f, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Caught));
            rules.Reset(); rules.Reset();
            rules.Tick(0f, true);
            Assert.That(rules.State, Is.EqualTo(ArchiveCabinetCrushRules.Phase.Warning));
        }
    }
}
