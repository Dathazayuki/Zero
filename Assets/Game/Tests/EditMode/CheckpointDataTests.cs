using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class CheckpointDataTests
    {
        [Test]
        public void StairwayRejectsInvalidFloorAndNumber()
        {
            Assert.IsTrue(CheckpointData.ForStairway(-2, 8).IsValid());
            Assert.IsFalse(CheckpointData.ForStairway(0, 9).IsValid());
            Assert.IsFalse(CheckpointData.ForStairway(100001, 0).IsValid());
        }

        [Test]
        public void BackroomCheckpointIsValidWithoutFloorProgress()
        {
            Assert.IsTrue(CheckpointData.ForBackroom().IsValid());
        }

        [TestCase("logicalFloorIndex")]
        [TestCase("displayedFloorNumber")]
        [TestCase("version")]
        [TestCase("signature")]
        [TestCase("kind")]
        public void MissingRequiredFieldRejectsCheckpoint(string field)
        {
            string json = UnityEngine.JsonUtility.ToJson(CheckpointData.ForStairway(3, 4));
            json = System.Text.RegularExpressions.Regex.Replace(json,
                "\\\"" + field + "\\\":(?:\\\"[^\\\"]*\\\"|-?\\d+),?", "");
            json = json.Replace(",}", "}");
            Assert.IsFalse(CheckpointData.TryDeserialize(json, out _));
        }

        [Test]
        public void BackroomRoundTripsRequiredZeroFloorFields()
        {
            string json = UnityEngine.JsonUtility.ToJson(CheckpointData.ForBackroom());
            Assert.IsTrue(CheckpointData.TryDeserialize(json, out CheckpointData data));
            Assert.AreEqual(CheckpointKind.Backroom, data.Kind);
        }
    }
}
