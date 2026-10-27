using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenRatSwarmAnomalyTests
    {
        [Test]
        public void MissingReferencesAndRepeatedResetDoNotLockOrThrow()
        {
            var root = new GameObject("UnconfiguredSwarm");
            try
            {
                var anomaly = root.AddComponent<KitchenRatSwarmAnomaly>();
                anomaly.Activate(); anomaly.SendMessage("LateUpdate");
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
