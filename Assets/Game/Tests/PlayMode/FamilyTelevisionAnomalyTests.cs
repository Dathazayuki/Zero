#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class FamilyTelevisionAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator FamilyFeedStopsAndReleasesItsOutputOnReset()
        {
            var anomaly = (FamilyTelevisionAnomaly)Select("IR2-12");
            Assert.That(anomaly.Feed.IsPlaying, Is.True);
            yield return new WaitForSeconds(0.2f);
            preview.ResetPreview();
            anomaly.ResetAnomaly();
            Assert.That(anomaly.Feed.IsPlaying, Is.False);
            Assert.That(anomaly.IsActive, Is.False);
        }
    }
}
#endif
