using NUnit.Framework;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveDrawerSequenceTests
    {
        [Test]
        public void ProximityAndIntervalGateTheNextDrawerWithoutReopeningPreviousDrawers()
        {
            var sequence = new ArchiveDrawerSequence(3, 0.8f, 1f);
            sequence.Advance(4f, false);
            Assert.That(sequence.NextIndex, Is.Zero);
            sequence.Advance(0.01f, true);
            Assert.That(sequence.NextIndex, Is.EqualTo(1));
            sequence.Advance(0.5f, true);
            Assert.That(sequence.NextIndex, Is.EqualTo(1));
            Assert.That(sequence.Progress(0), Is.InRange(0.62f, 0.63f));
            sequence.Advance(2f, false);
            Assert.That(sequence.Progress(0), Is.EqualTo(1f));
            Assert.That(sequence.NextIndex, Is.EqualTo(1));
            sequence.Advance(0.01f, true);
            Assert.That(sequence.NextIndex, Is.EqualTo(2));
            Assert.That(sequence.Progress(1), Is.Zero);
        }

        [Test]
        public void ResetDuringOpeningRemovesAllPendingProgressAndCanRepeat()
        {
            var sequence = new ArchiveDrawerSequence(2, 0.8f, 1f);
            sequence.Advance(0.1f, true);
            sequence.Advance(0.4f, false);
            sequence.Reset();
            sequence.Reset();
            Assert.That(sequence.NextIndex, Is.Zero);
            Assert.That(sequence.Progress(0), Is.Zero);
            Assert.That(sequence.HasStarted(0), Is.False);
            sequence.Advance(0.1f, true);
            Assert.That(sequence.NextIndex, Is.EqualTo(1));
            Assert.That(sequence.Progress(0), Is.Zero);
        }

        [Test]
        public void CompletionConsumesFurtherRequests()
        {
            var sequence = new ArchiveDrawerSequence(1, 0.8f, 1f);
            sequence.Advance(0.1f, true);
            sequence.Advance(10f, true);
            sequence.Advance(10f, true);
            Assert.That(sequence.NextIndex, Is.EqualTo(1));
            Assert.That(sequence.Progress(0), Is.EqualTo(1f));
        }
    }
}
