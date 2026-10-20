#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class ApproachingPaintingAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator PaintingAdvancesOnlyAfterLookingAwayAndResetsToNormal()
        {
            var anomaly = (ApproachingPaintingAnomaly)Select("IR2-13");
            Move(new Vector3(2.5f, 0.03f, 2f));
            Vector3 target = new Vector3(2.5f, 2.25f, 4.87f);
            camera.LookAt(target);
            yield return new WaitForSeconds(0.2f);
            Assert.That(anomaly.StageIndex, Is.Zero);
            yield return new WaitForSeconds(0.6f);
            Assert.That(anomaly.StageIndex, Is.Zero);
            for (int stage = 1; stage <= 2; stage++)
            {
                camera.rotation = Quaternion.LookRotation(Vector3.back);
                yield return new WaitForSeconds(0.5f);
                Assert.That(anomaly.StageIndex, Is.EqualTo(stage - 1));
                camera.LookAt(target);
                yield return new WaitForSeconds(0.1f);
                Assert.That(anomaly.StageIndex, Is.EqualTo(stage));
            }
            preview.ResetPreview();
            anomaly.ResetAnomaly();
            Assert.That(anomaly.StageIndex, Is.EqualTo(-1));
            Assert.That(anomaly.IsActive, Is.False);
        }
    }
}
#endif
