using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenButcherChaseAnomalyTests
    {
        [Test]
        public void MissingDependenciesDoNotLockPlayerAndResetIsIdempotent()
        {
            var go = new GameObject("UnconfiguredKitchenChase");
            try
            {
                var anomaly = go.AddComponent<KitchenButcherChaseAnomaly>();
                Assert.That(anomaly.IsConfigured, Is.False);
                LogAssert.Expect(LogType.Error, "KITCHEN_CHASE_CONFIGURATION_INVALID");
                anomaly.Activate(); anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
