using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class SinkOverflowProgressTests
    {
        [Test]
        public void FillCompletesBeforeSpillAndDuplicateStartDoesNotRestartIt()
        {
            var progress = new SinkOverflowProgress(10f, 8f);
            progress.Advance(100f);
            Assert.That(progress.Fill, Is.Zero);
            progress.Start(); progress.Advance(5f); progress.Start();
            Assert.That(progress.Fill, Is.EqualTo(0.5f));
            Assert.That(progress.IsOverflowing, Is.False);
            progress.Advance(9f);
            Assert.That(progress.Fill, Is.EqualTo(1f));
            Assert.That(progress.Spill, Is.EqualTo(0.5f));
            progress.Advance(100f);
            Assert.That(progress.Spill, Is.EqualTo(1f));
            progress.Reset(); progress.Reset();
            Assert.That(progress.HasStarted, Is.False);
            Assert.That(progress.IsOverflowing, Is.False);
            Assert.That(progress.Spill, Is.Zero);
        }
    }
}
