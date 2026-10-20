#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class TelevisionMotherAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator LeavingTheRoomDuringWarningCancelsMotherAndRestoresTheTv()
        {
            var anomaly = (TelevisionMotherAnomaly)Select("IR2-18");
            Assert.That(anomaly.IsConfigured, Is.True);
            Move(new Vector3(3.4f, 0.03f, -4.8f));
            yield return null;
            Assert.That(anomaly.IsActive, Is.False);
            Assert.That(anomaly.GetComponent<TelevisionFeed>().IsPlaying, Is.False);
            Assert.That(anomaly.GetComponentInChildren<NavMeshAgent>(true).enabled, Is.False);
            anomaly.ResetAnomaly();
        }
    }
}
#endif
