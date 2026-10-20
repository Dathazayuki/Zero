using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class PhoneEyeAnomalyTests
    {
        [Test]
        public void ResetRestoresScreenVisibilityAndThePreviousPropertyBlock()
        {
            var root = new GameObject("PhoneTest");
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.transform.SetParent(root.transform);
            try
            {
                Renderer renderer = screen.GetComponent<Renderer>();
                renderer.enabled = false;
                var original = new MaterialPropertyBlock();
                Color tint = new Color(0.31f, 0.42f, 0.53f);
                original.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(original);
                var anomaly = root.AddComponent<PhoneEyeAnomaly>();
                anomaly.Configure(renderer, root.transform);
                anomaly.Activate();
                Assert.That(renderer.enabled, Is.True);
                anomaly.CaptureNormalState();
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                renderer.GetPropertyBlock(original);
                Assert.That(Vector4.Distance(original.GetColor("_BaseColor"), tint), Is.LessThan(0.0001f));
                Assert.That(renderer.enabled, Is.False);
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void MissingObserverFailsSafe()
        {
            var root = new GameObject("PhoneTest");
            try
            {
                var anomaly = root.AddComponent<PhoneEyeAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False);
                Assert.DoesNotThrow(() => anomaly.Activate());
                Assert.DoesNotThrow(() => anomaly.ResetAnomaly());
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
