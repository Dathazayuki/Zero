using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class ArchiveBoxShakeAnomalyTests
    {
        [UnityTest]
        public IEnumerator ResetAndDisableRestoreBothTheBoxAndLid()
        {
            var root = new GameObject("ShakeTest");
            Transform box = new GameObject("Box").transform;
            box.SetParent(root.transform);
            Transform lid = new GameObject("Lid").transform;
            lid.SetParent(box);
            box.localPosition = Vector3.up;
            Quaternion closed = Quaternion.Euler(4f, 0f, 0f);
            lid.localRotation = closed;
            var anomaly = root.AddComponent<ArchiveBoxShakeAnomaly>();
            anomaly.Configure(box, lid);
            anomaly.Activate();
            yield return new WaitForSeconds(0.3f);
            Assert.That(Vector3.Distance(box.localPosition, Vector3.up), Is.GreaterThan(0.000001f));
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(box.localPosition, Is.EqualTo(Vector3.up));
            Assert.That(Quaternion.Angle(lid.localRotation, closed), Is.LessThan(0.001f));
            anomaly.Activate();
            yield return null;
            anomaly.enabled = false;
            Assert.That(anomaly.IsActive, Is.False);
            Assert.That(box.localPosition, Is.EqualTo(Vector3.up));
            Object.Destroy(root);
        }
    }
}
