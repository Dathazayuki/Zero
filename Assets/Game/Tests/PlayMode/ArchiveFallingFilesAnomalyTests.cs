using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class ArchiveFallingFilesAnomalyTests
    {
        private GameObject root;

        [TearDown]
        public void Cleanup() => Object.DestroyImmediate(root);

        [UnityTest]
        public IEnumerator FallingCanBeResetMidairAndDisabledWithoutLeavingAMovedFile()
        {
            ArchiveFallingFilesAnomaly anomaly = Create(out Rigidbody file);
            Vector3 normal = file.position;
            anomaly.Activate();
            Assert.That(file.isKinematic, Is.False);
            Assert.That(file.useGravity, Is.True);
            yield return new WaitForSeconds(0.3f);
            Assert.That(file.position.x, Is.EqualTo(normal.x).Within(0.001f));
            Assert.That(file.angularVelocity.sqrMagnitude, Is.LessThan(0.0001f));
            Assert.That(file.position.y, Is.LessThan(normal.y));
            anomaly.ResetAnomaly();
            anomaly.ResetAnomaly();
            Assert.That(file.position, Is.EqualTo(normal));
            Assert.That(file.isKinematic, Is.True);
            Assert.That(file.useGravity, Is.False);
            yield return new WaitForFixedUpdate();
            Assert.That(file.position, Is.EqualTo(normal));
            anomaly.Activate();
            yield return new WaitForSeconds(1.8f);
            Assert.That(file.position.y, Is.InRange(0.03f, 0.4f), "The file should collide with the floor.");
            anomaly.enabled = false;
            Assert.That(file.position, Is.EqualTo(normal));
            Assert.That(file.isKinematic, Is.True);
            Assert.That(anomaly.IsActive, Is.False);
        }

        private ArchiveFallingFilesAnomaly Create(out Rigidbody file)
        {
            root = new GameObject("FallingFilesLifecycle");
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.SetParent(root.transform, false);
            item.transform.localPosition = new Vector3(-1f, 2f, 0f);
            item.transform.localScale = new Vector3(0.43f, 0.37f, 0.105f);
            file = item.AddComponent<Rigidbody>();
            file.isKinematic = true;
            file.useGravity = false;
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = Vector3.down * 0.1f;
            floor.transform.localScale = new Vector3(20f, 0.2f, 20f);
            var anomaly = root.AddComponent<ArchiveFallingFilesAnomaly>();
            anomaly.Configure(new[] { item.transform }, new[] { file });
            return anomaly;
        }
    }
}
