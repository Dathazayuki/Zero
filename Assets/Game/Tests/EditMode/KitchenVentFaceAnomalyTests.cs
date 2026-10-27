using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class KitchenVentFaceAnomalyTests
    {
        [Test]
        public void LookingTriggersUpwardRetreatOnceAndResetRestoresPeek()
        {
            var root = new GameObject("VentFaceTest");
            try
            {
                var camera = new GameObject("Camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform);
                var face = new GameObject("Face").transform; face.SetParent(root.transform); face.position = Vector3.forward * 2f;
                var target = new GameObject("Target").transform; target.SetParent(face, false);
                var anomaly = root.AddComponent<KitchenVentFaceAnomaly>(); Bind(anomaly, camera, face, target);
                anomaly.Activate(); camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                anomaly.Advance(1f); Assert.That(anomaly.IsRetreating, Is.False);
                camera.transform.rotation = Quaternion.identity;
                anomaly.Advance(0.13f); Assert.That(anomaly.IsRetreating, Is.True);
                anomaly.Advance(1f); Assert.That(face.gameObject.activeSelf, Is.False);
                Assert.That(face.position.y, Is.EqualTo(0.8f).Within(0.00001f));
                Assert.That(face.position.x, Is.Zero);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly(); anomaly.Activate();
                Assert.That(face.position, Is.EqualTo(Vector3.forward * 2f));
                Assert.That(anomaly.IsRetreating, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void Bind(KitchenVentFaceAnomaly anomaly, Camera camera, Transform face, Transform target)
        {
            var so = new SerializedObject(anomaly);
            so.FindProperty("observer").objectReferenceValue = camera;
            so.FindProperty("face").objectReferenceValue = face;
            so.FindProperty("gazePoint").objectReferenceValue = target;
            so.FindProperty("retreatOffset").vector3Value = new Vector3(0.8f, 0f, 0f);
            so.FindProperty("obstructionMask").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
