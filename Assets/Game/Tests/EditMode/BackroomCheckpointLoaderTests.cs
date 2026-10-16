using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class BackroomCheckpointLoaderTests
    {
        [Test]
        public void MissingPlayerRejectsWithoutStartingSceneLoad()
        {
            var loader = new BackroomCheckpointLoader("BackRoomScene");
            IEnumerator routine = loader.Restore(null);
            LogAssert.Expect(LogType.Error,
                "CHECKPOINT_BACKROOM_CONFIG_INVALID: scene=BackRoomScene");
            Assert.IsFalse(routine.MoveNext());
            Assert.IsFalse(loader.Succeeded);
        }
    }
}
