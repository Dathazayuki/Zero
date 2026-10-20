using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class ArchiveBodyDrawersAnomalyTests
    {
        private GameObject root;

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator ResetDuringOpeningRestoresTheTrayAndHidesItsContents()
        {
            ArchiveBodyDrawersAnomaly anomaly = Create(out Transform drawer, out GameObject part);
            Vector3 baseline = drawer.localPosition;
            anomaly.Activate();
            yield return null;
            yield return new WaitForSeconds(0.25f);
            Assert.That(drawer.localPosition.x, Is.LessThan(baseline.x));
            Assert.That(part.activeSelf, Is.True);
            anomaly.ResetAnomaly();
            anomaly.ResetAnomaly();
            Assert.That(drawer.localPosition, Is.EqualTo(baseline));
            Assert.That(part.activeSelf, Is.False);
            Assert.That(anomaly.OpenedCount, Is.Zero);
            yield return null;
            Assert.That(drawer.localPosition, Is.EqualTo(baseline));
        }

        [UnityTest]
        public IEnumerator DisableCleansUpAndReactivationStartsAFreshSequence()
        {
            ArchiveBodyDrawersAnomaly anomaly = Create(out Transform drawer, out GameObject part);
            anomaly.Activate();
            yield return null;
            yield return new WaitForSeconds(0.2f);
            anomaly.enabled = false;
            Assert.That(drawer.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(part.activeSelf, Is.False);
            Assert.That(anomaly.IsActive, Is.False);
            anomaly.enabled = true;
            anomaly.Activate();
            yield return null;
            yield return null;
            Assert.That(anomaly.OpenedCount, Is.EqualTo(1));
        }

        private ArchiveBodyDrawersAnomaly Create(out Transform drawer, out GameObject part)
        {
            root = new GameObject("DrawerLifecycle");
            drawer = new GameObject("Drawer").transform;
            drawer.SetParent(root.transform, false);
            part = new GameObject("Contents");
            part.transform.SetParent(drawer, false);
            part.SetActive(false);
            Transform observer = new GameObject("Observer").transform;
            observer.SetParent(root.transform, false);
            var anomaly = root.AddComponent<ArchiveBodyDrawersAnomaly>();
            anomaly.Configure(observer, new[] { drawer }, new[] { part });
            anomaly.CaptureNormalState();
            return anomaly;
        }
    }
}
