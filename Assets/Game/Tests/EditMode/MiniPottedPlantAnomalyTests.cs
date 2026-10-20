using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class MiniPottedPlantAnomalyTests
    {
        [Test]
        public void RepeatedActivationAndResetPreserveOriginalScale()
        {
            var root = new GameObject("PlantTest");
            try
            {
                var plant = new GameObject("Plant").transform;
                plant.SetParent(root.transform);
                plant.localScale = new Vector3(1.2f, 2f, 3f);
                var anomaly = root.AddComponent<MiniPottedPlantAnomaly>();
                anomaly.Configure(plant);
                anomaly.Activate();
                anomaly.CaptureNormalState();
                anomaly.Activate();
                Assert.That(plant.localScale, Is.EqualTo(new Vector3(1.2f, 2f, 3f) * 0.6f));
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                Assert.That(plant.localScale, Is.EqualTo(new Vector3(1.2f, 2f, 3f)));
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ReconfigureRestoresOldTargetAndCapturesNewTarget()
        {
            var root = new GameObject("PlantRebindTest");
            try
            {
                var first = new GameObject("First").transform;
                var second = new GameObject("Second").transform;
                first.SetParent(root.transform);
                second.SetParent(root.transform);
                second.localScale = Vector3.one * 2f;
                var anomaly = root.AddComponent<MiniPottedPlantAnomaly>();
                anomaly.Configure(first);
                anomaly.Activate();
                anomaly.Configure(second);
                Assert.That(first.localScale, Is.EqualTo(Vector3.one));
                Assert.That(anomaly.IsActive, Is.False);
                anomaly.Activate();
                Assert.That(second.localScale, Is.EqualTo(Vector3.one * 1.2f));
                anomaly.enabled = false;
                Assert.That(second.localScale, Is.EqualTo(Vector3.one * 2f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NullTargetFailsSafe()
        {
            var root = new GameObject("NullPlantTest");
            try
            {
                var anomaly = root.AddComponent<MiniPottedPlantAnomaly>();
                anomaly.Configure(null);
                Assert.DoesNotThrow(() => anomaly.Activate());
                Assert.DoesNotThrow(() => anomaly.ResetAnomaly());
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
