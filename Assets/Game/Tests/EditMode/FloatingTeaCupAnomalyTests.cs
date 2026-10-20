using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class FloatingTeaCupAnomalyTests
    {
        [Test]
        public void ResetAndDisableRestoreTheOriginalCupWithoutRecapturingItsAnimatedPose()
        {
            var root = new GameObject("TeaTest");
            try
            {
                var cup = new GameObject("Cup").transform;
                cup.SetParent(root.transform);
                cup.localPosition = new Vector3(1f, 2f, 3f);
                var anomaly = root.AddComponent<FloatingTeaCupAnomaly>();
                anomaly.Configure(cup, null);
                anomaly.Activate();
                cup.localPosition += Vector3.up;
                cup.localRotation = Quaternion.Euler(0f, 0f, 18f);
                anomaly.CaptureNormalState();
                anomaly.enabled = false;
                anomaly.ResetAnomaly();
                Assert.That(cup.localPosition, Is.EqualTo(new Vector3(1f, 2f, 3f)));
                Assert.That(Quaternion.Angle(cup.localRotation, Quaternion.identity), Is.LessThan(0.01f));
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
