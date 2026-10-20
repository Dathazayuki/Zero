using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class ReverseClockAnomalyTests
    {
        [Test]
        public void ResetRestoresOriginalHandsAndBothAuthoredRotations()
        {
            var root = new GameObject("ReverseClockTest");
            try
            {
                var original = new GameObject("OriginalHands");
                original.transform.SetParent(root.transform);
                var hour = new GameObject("Hour").transform;
                hour.SetParent(root.transform);
                var minute = new GameObject("Minute").transform;
                minute.SetParent(root.transform);
                hour.localRotation = Quaternion.Euler(0f, 0f, 15f);
                minute.localRotation = Quaternion.Euler(0f, 0f, 70f);
                hour.gameObject.SetActive(false);
                minute.gameObject.SetActive(false);
                var anomaly = root.AddComponent<ReverseClockAnomaly>();
                anomaly.Configure(original, hour, minute);
                anomaly.Activate();
                Assert.That(original.activeSelf, Is.False);
                Assert.That(hour.gameObject.activeSelf && minute.gameObject.activeSelf, Is.True);
                hour.localRotation = minute.localRotation = Quaternion.Euler(0f, 0f, 140f);
                anomaly.CaptureNormalState();
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                Assert.That(original.activeSelf, Is.True);
                Assert.That(hour.gameObject.activeSelf || minute.gameObject.activeSelf, Is.False);
                Assert.That(Quaternion.Angle(hour.localRotation, Quaternion.Euler(0f, 0f, 15f)), Is.LessThan(0.01f));
                Assert.That(Quaternion.Angle(minute.localRotation, Quaternion.Euler(0f, 0f, 70f)), Is.LessThan(0.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
