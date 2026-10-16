using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class EncounterDiversityHistoryTests
    {
        [Test]
        public void RestorePreservesCoverageAndLastTwoAcrossAttemptReset()
        {
            var history = new EncounterDiversityHistory();
            history.Record("a"); history.Record("b"); history.Record("c");
            var restored = new EncounterDiversityHistory();
            restored.Restore(history.Capture());
            Assert.IsTrue(restored.HasEncountered("a"));
            Assert.IsFalse(restored.IsRecent("a"));
            Assert.IsTrue(restored.IsRecent("b"));
            Assert.IsTrue(restored.IsRecent("c"));
            restored.Restore(null);
            Assert.IsFalse(restored.HasEncountered("a"));
        }

        [Test]
        public void RepeatedEncountersDoNotGrowSaveWithoutBound()
        {
            var history = new EncounterDiversityHistory();
            for (int i = 0; i < 2000; i++) history.Record("id-" + i % 24);
            Assert.LessOrEqual(history.Capture().Length, 256);
            Assert.IsTrue(history.IsRecent("id-7"));
        }
    }
}
