using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class EndlessPrinterAnomalyTests
    {
        [UnityTest]
        public IEnumerator PaperRemainsAfterFeedingAndResetAndDisableClearIt()
        {
            var root = new GameObject("PrinterTest");
            try
            {
                var surface = new GameObject("Paper");
                surface.transform.SetParent(root.transform);
                var filter = surface.AddComponent<MeshFilter>();
                var end = new GameObject("End");
                end.transform.SetParent(root.transform);
                end.transform.localPosition = Vector3.forward * 0.02f;
                var anomaly = root.AddComponent<EndlessPrinterAnomaly>();
                anomaly.Configure(filter, new[] { root.transform, end.transform });
                anomaly.Activate();
                Mesh mesh = filter.sharedMesh;
                yield return new WaitForSeconds(0.25f);
                Assert.That(mesh.bounds.size.z, Is.EqualTo(0.02f).Within(0.001f));
                yield return new WaitForSeconds(0.1f);
                Assert.That(surface.activeSelf, Is.True);
                Assert.That(mesh.bounds.size.z, Is.EqualTo(0.02f).Within(0.001f));
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                Assert.That(surface.activeSelf, Is.False);
                anomaly.Activate();
                Assert.That(filter.sharedMesh, Is.SameAs(mesh));
                Assert.That(mesh.bounds.size.z, Is.LessThan(0.001f));
                anomaly.enabled = false;
                Assert.That(surface.activeSelf, Is.False);
            }
            finally { Object.Destroy(root); }
            yield return null;
        }
    }
}
