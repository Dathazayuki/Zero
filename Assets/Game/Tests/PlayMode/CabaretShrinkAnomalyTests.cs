#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.PlayMode
{
    public sealed class CabaretShrinkAnomalyTests
    {
        [UnityTest]
        public IEnumerator ShrinkLowersCameraGrowthWaitsForHeadroomAndResetRestoresScale()
        {
            var rig = new GameObject("Cabaret shrink test");
            rig.transform.position = Vector3.one * 1000f;
            try
            {
                var body = Node(rig, "Player", new Vector3(0f, 0f, 5.5f)).AddComponent<CharacterController>();
                body.height = 1.8f; body.center = Vector3.up * .9f; body.radius = .28f;
                var eye = Node(body.gameObject, "Camera", Vector3.up * 1.61f).transform;
                var anomaly = Configure(rig, body);
                anomaly.Activate();
                yield return new WaitForSeconds(1f);
                Assert.That(anomaly.CurrentFactor, Is.EqualTo(.4f).Within(.001f));
                Assert.That(eye.position.y - body.transform.position.y, Is.EqualTo(1.61f * .4f).Within(.001f));
                body.transform.localPosition = new Vector3(0f, 0f, -7f);
                var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceiling.transform.SetParent(rig.transform);
                ceiling.transform.localPosition = new Vector3(0f, .95f, -7f);
                ceiling.transform.localScale = new Vector3(2f, .1f, 2f);
                Physics.SyncTransforms();
                yield return new WaitForSeconds(1f);
                Assert.That(anomaly.CurrentFactor, Is.InRange(.4f, .55f));
                Object.Destroy(ceiling);
                yield return new WaitForSeconds(1f);
                Assert.That(body.transform.localScale, Is.EqualTo(Vector3.one));
                body.transform.localPosition = new Vector3(0f, 0f, 5.5f);
                yield return new WaitForSeconds(1f);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(body.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally { Object.Destroy(rig); }
        }

        private static CabaretShrinkAnomaly Configure(GameObject rig, CharacterController body)
        {
            var room = Node(rig, "Room", Vector3.up).AddComponent<BoxCollider>();
            room.size = new Vector3(12f, 6f, 16f); room.isTrigger = true;
            var stage = Node(rig, "Stage", new Vector3(0f, 0f, 6f)).AddComponent<BoxCollider>();
            stage.size = new Vector3(10f, .24f, 3f); stage.isTrigger = true;
            var anomaly = rig.AddComponent<CabaretShrinkAnomaly>();
            var serialized = new SerializedObject(anomaly);
            serialized.FindProperty("playerBody").objectReferenceValue = body;
            serialized.FindProperty("roomArea").objectReferenceValue = room;
            serialized.FindProperty("stageArea").objectReferenceValue = stage;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
            return anomaly;
        }

        private static GameObject Node(GameObject parent, string name, Vector3 position)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent.transform, false);
            node.transform.localPosition = position;
            return node;
        }
    }
}
#endif
