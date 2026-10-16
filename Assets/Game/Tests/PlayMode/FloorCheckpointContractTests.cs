using NUnit.Framework;
using UnityEngine;
using Zero.EnvironmentLoop;

namespace Zero.Tests.PlayMode
{
    public sealed class FloorCheckpointContractTests
    {
        [Test]
        public void RestoringLogicalFloorDoesNotPretendACommitOccurred()
        {
            var root = new GameObject("FloorCheckpointContract");
            try
            {
                var progress = root.AddComponent<FloorProgressManager>();
                progress.Initialize();
                Assert.IsTrue(progress.RestoreLogicalFloor(-5));
                Assert.AreEqual(-5, progress.LogicalFloorIndex);
                Assert.AreEqual(0, progress.CommitCount);
                Assert.IsFalse(progress.HasTravelDirection);
                Assert.IsFalse(progress.RestoreLogicalFloor(100001));
                Assert.AreEqual(-5, progress.LogicalFloorIndex);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
