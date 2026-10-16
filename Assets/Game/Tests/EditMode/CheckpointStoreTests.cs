using System;
using System.IO;
using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class CheckpointStoreTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "ZeroCheckpointTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void StairwayLegacyAdapterStoresOnlyStageEntrance()
        {
            var store = new CheckpointStore(directory);
            Assert.IsTrue(store.Save(CheckpointData.ForStairway(-4, 7)));
            Assert.IsTrue(store.TryLoad(out CheckpointData loaded));
            Assert.AreEqual(CheckpointKind.Stairway, loaded.Kind);
            Assert.AreEqual(0, loaded.LogicalFloorIndex);
            Assert.AreEqual(0, loaded.DisplayedFloorNumber);
            Assert.AreEqual(CampaignCheckpoint.FirstStageId, loaded.StageId);
        }

        [Test]
        public void CorruptPrimaryRecoversPreviousCheckpointFromBackup()
        {
            var store = new CheckpointStore(directory);
            Assert.IsTrue(store.Save(StageCheckpoint("stage-2")));
            Assert.IsTrue(store.Save(CheckpointData.ForBackroom()));
            File.WriteAllText(Path.Combine(directory, "zero-checkpoint.json"), "{}");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                "CHECKPOINT_BACKUP_RECOVERED");
            Assert.IsTrue(store.TryLoad(out CheckpointData loaded));
            Assert.AreEqual(CheckpointKind.Stairway, loaded.Kind);
            Assert.AreEqual("stage-2", loaded.StageId);
            Assert.AreEqual(0, loaded.DisplayedFloorNumber);
        }

        [Test]
        public void ClearRemovesPrimaryAndBackup()
        {
            var store = new CheckpointStore(directory);
            Assert.IsTrue(store.Save(CheckpointData.ForStairway(1, 1)));
            Assert.IsTrue(store.Save(CheckpointData.ForBackroom()));
            Assert.IsTrue(store.Clear());
            Assert.IsFalse(store.TryLoad(out _));
        }

        [Test]
        public void FutureSaveVersionDoesNotLoad()
        {
            var store = new CheckpointStore(directory);
            Assert.IsTrue(store.Save(CheckpointData.ForStairway(2, 4)));
            string path = Path.Combine(directory, "zero-checkpoint.json");
            string json = File.ReadAllText(path).Replace("\"version\":" + CheckpointData.CurrentVersion, "\"version\":999");
            StringAssert.Contains("\"version\":999", json);
            File.WriteAllText(path, json);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "CHECKPOINT_LOAD_INVALID: primary and backup failed validation");
            Assert.IsFalse(store.TryLoad(out _));
        }

        [Test]
        public void SavingAfterCorruptionPreservesLastValidBackup()
        {
            var store = new CheckpointStore(directory);
            Assert.IsTrue(store.Save(StageCheckpoint("stage-2")));
            Assert.IsTrue(store.Save(StageCheckpoint("stage-3")));
            string path = Path.Combine(directory, "zero-checkpoint.json");
            File.WriteAllText(path, "{}");
            Assert.IsTrue(store.Save(CheckpointData.ForBackroom()));
            File.WriteAllText(path, "{}");
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
                "CHECKPOINT_BACKUP_RECOVERED");
            Assert.IsTrue(store.TryLoad(out CheckpointData data));
            Assert.AreEqual("stage-2", data.StageId);
        }

        private static CheckpointData StageCheckpoint(string stage) => CheckpointData.ForCampaign(
            new CampaignCheckpoint(CampaignCheckpoint.DefaultCampaignId, stage, CampaignCheckpoint.LocationKind.InfinityRoomStart));
    }
}
