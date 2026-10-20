using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class DeskLampFlickerAnomalyTests
    {
        private GameObject root;

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        [UnityTest]
        public IEnumerator ResetAndDisableRestoreBothEnabledStateAndIntensity()
        {
            root = new GameObject("FlickeringDeskLampLifecycle");
            Light lamp = root.AddComponent<Light>();
            lamp.intensity = 1.7f;
            lamp.enabled = false;
            var anomaly = root.AddComponent<DeskLampFlickerAnomaly>();
            anomaly.Configure(lamp);
            anomaly.Activate();
            bool sawLit = false;
            bool sawDark = false;
            float end = Time.time + 0.8f;
            while (Time.time < end)
            {
                sawLit |= lamp.enabled;
                sawDark |= !lamp.enabled;
                yield return null;
            }
            Assert.That(sawLit && sawDark, Is.True);
            anomaly.ResetAnomaly();
            anomaly.ResetAnomaly();
            Assert.That(lamp.enabled, Is.False);
            Assert.That(lamp.intensity, Is.EqualTo(1.7f));
            anomaly.Activate();
            yield return new WaitForSeconds(0.2f);
            anomaly.enabled = false;
            Assert.That(lamp.enabled, Is.False);
            Assert.That(anomaly.IsActive, Is.False);
        }
    }
}
