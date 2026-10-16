using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class CampaignCheckpointTests
    {
        [Test]
        public void VersionTwoRoundTripKeepsStageAndHistoryButNotFloorProgress()
        {
            var snapshot = new CampaignCheckpoint("test", "stage-3",
                CampaignCheckpoint.LocationKind.MinigameEntrance, new[] { "a", "b", "c" });
            string json = JsonUtility.ToJson(CheckpointData.ForCampaign(snapshot));
            Assert.IsTrue(CheckpointData.TryDeserialize(json, out CheckpointData data));
            Assert.AreEqual("stage-3", data.StageId);
            Assert.AreEqual("test", data.CampaignId);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, data.ToCampaignCheckpoint().EncounteredIds);
            Assert.AreEqual(0, data.LogicalFloorIndex);
            Assert.AreEqual(0, data.DisplayedFloorNumber);
        }

        [TestCase(1, 5, 8, CheckpointKind.Stairway)]
        [TestCase(2, 0, 0, CheckpointKind.Backroom)]
        public void LegacySaveMigratesToFirstStageEntrance(int kind, int floor, int number, CheckpointKind expected)
        {
            string json = "{\"signature\":\"ZERO_CHECKPOINT\",\"version\":1,\"kind\":" + kind +
                ",\"logicalFloorIndex\":" + floor + ",\"displayedFloorNumber\":" + number + "}";
            Assert.IsTrue(CheckpointData.TryDeserialize(json, out CheckpointData data));
            Assert.AreEqual(2, data.Version);
            Assert.AreEqual(CampaignCheckpoint.FirstStageId, data.StageId);
            Assert.AreEqual(expected, data.Kind);
            Assert.AreEqual(0, data.DisplayedFloorNumber);
        }

        [TestCase("{}")]
        [TestCase("{broken")]
        [TestCase("{\"version\":99}")]
        public void InvalidOrFutureSaveIsRejected(string json)
        {
            Assert.IsFalse(CheckpointData.TryDeserialize(json, out _));
        }
    }
}
