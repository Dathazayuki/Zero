using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenFallingTeethAnomalyTests
    {
        [Test]
        public void ActivationOnlyReleasesGravityAndResetHidesAndRestoresTeeth()
        {
            var root = new GameObject("TeethTest");
            try
            {
                var tooth = new GameObject("Tooth"); tooth.transform.SetParent(root.transform);
                tooth.transform.localPosition = Vector3.up;
                var body = tooth.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                tooth.SetActive(false);
                var anomaly = root.AddComponent<KitchenFallingTeethAnomaly>();
                var so = new SerializedObject(anomaly); var array = so.FindProperty("teeth");
                array.arraySize = 1; array.GetArrayElementAtIndex(0).objectReferenceValue = body;
                so.ApplyModifiedPropertiesWithoutUndo();
                anomaly.Activate();
                Assert.That(tooth.activeSelf && body.useGravity && !body.isKinematic, Is.True);
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
                Assert.That(body.angularVelocity, Is.EqualTo(Vector3.zero));
                body.position = Vector3.zero;
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(tooth.activeSelf, Is.False);
                Assert.That(body.isKinematic && !body.useGravity, Is.True);
                Assert.That(tooth.transform.localPosition, Is.EqualTo(Vector3.up));
                anomaly.Activate();
                Assert.That(body.linearVelocity, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
