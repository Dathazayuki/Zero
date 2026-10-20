using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveCabinetCrushAnomalyTests
    {
        [Test]
        public void ResetAndDisableRestoreBothCabinetsEvenDuringAManualPreview()
        {
            var root = new GameObject("CrushLifecycle");
            try
            {
                GameObject moving = GameObject.CreatePrimitive(PrimitiveType.Cube);
                moving.transform.SetParent(root.transform);
                moving.transform.position = Vector3.left * 2f;
                GameObject warning = GameObject.CreatePrimitive(PrimitiveType.Cube);
                warning.transform.SetParent(root.transform);
                warning.transform.position = Vector3.right * 2f;
                GameObject player = new GameObject("Player");
                player.transform.SetParent(root.transform);
                player.AddComponent<CharacterController>();
                var camera = player.AddComponent<Camera>();
                var death = player.AddComponent<PlayerDeathController>();
                var view = root.AddComponent<AnomalyDeathPresentation>();
                var anomaly = root.AddComponent<ArchiveCabinetCrushAnomaly>();
                anomaly.Configure(moving.GetComponent<BoxCollider>(), warning.transform, camera, death, view);
                anomaly.Activate();
                moving.transform.position = Vector3.one;
                warning.transform.position = Vector3.zero;
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(moving.transform.position, Is.EqualTo(Vector3.left * 2f));
                Assert.That(warning.transform.position, Is.EqualTo(Vector3.right * 2f));
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
