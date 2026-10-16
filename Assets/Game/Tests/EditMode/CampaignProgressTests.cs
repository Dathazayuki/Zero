using System;
using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class CampaignProgressTests
    {
        private static CampaignProgress Create() => new CampaignProgress("test",
            new[] { "one", "two", "three", "four", "five" });

        [Test]
        public void FiveRoomsAndFiveFakeSessionsReachEndingExactlyOnce()
        {
            var progress = Create();
            for (int stage = 0; stage < 5; stage++)
            {
                Assert.AreEqual(stage, progress.StageIndex);
                Assert.IsFalse(progress.TryCompleteMinigame());
                Assert.IsTrue(progress.TryCompleteInfinityRoom());
                Assert.IsFalse(progress.TryCompleteInfinityRoom());
                var session = new FakeSession(progress);
                Assert.IsTrue(session.Win());
                Assert.IsFalse(session.Win());
                if (stage < 4)
                {
                    Assert.AreEqual(stage, progress.StageIndex, "Completion alone must not commit a scene load.");
                    Assert.IsTrue(progress.TryEnterNextStage());
                    Assert.IsFalse(progress.TryEnterNextStage());
                }
            }
            Assert.AreEqual(CampaignProgress.PhaseKind.FinalEnding, progress.Phase);
            Assert.IsFalse(progress.TryEnterNextStage());
            Assert.IsTrue(progress.TryChooseEnding(EndingIdentity.CurtainCall));
            Assert.IsFalse(progress.TryChooseEnding(EndingIdentity.TheCast));
            Assert.IsFalse(progress.TryChooseEnding(EndingIdentity.CurtainCall));
            progress.ReleaseEnding(EndingIdentity.TheCast);
            Assert.AreEqual(EndingIdentity.CurtainCall, progress.SelectedEnding);
            progress.ReleaseEnding(EndingIdentity.CurtainCall);
            Assert.IsTrue(progress.TryChooseEnding(EndingIdentity.AnotherShift));
        }

        [Test]
        public void RestoreStartsOnlySelectedStageAtAnEntrance()
        {
            var progress = Create();
            Assert.IsFalse(progress.TryChooseEnding(EndingIdentity.TheCast));
            progress.Restore(new CampaignCheckpoint("test", "three", CampaignCheckpoint.LocationKind.InfinityRoomStart));
            Assert.AreEqual(2, progress.StageIndex);
            Assert.AreEqual(CampaignProgress.PhaseKind.InfinityRoom, progress.Phase);
            Assert.IsTrue(progress.TryCompleteInfinityRoom());
        }

        [Test]
        public void InvalidRestoreDoesNotMutateProgress()
        {
            var progress = Create();
            Assert.Throws<ArgumentException>(() => progress.Restore(new CampaignCheckpoint("test", "missing",
                CampaignCheckpoint.LocationKind.MinigameEntrance)));
            Assert.AreEqual(0, progress.StageIndex);
        }

        [Test]
        public void DuplicateStageIdsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new CampaignProgress("test", new[] { "a", "b", "a", "d", "e" }));
        }

        private sealed class FakeSession
        {
            private readonly CampaignProgress progress;
            public FakeSession(CampaignProgress state) { progress = state; }
            public bool Win() => progress.TryCompleteMinigame();
        }
    }
}
