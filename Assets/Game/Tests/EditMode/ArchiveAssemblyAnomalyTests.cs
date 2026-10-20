using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class ArchiveAssemblyAnomalyTests
    {
        [Test]
        public void ResetDuringScatterHidesBodyAndCircleAndRestoresPhysics()
        {
            var root = new GameObject("AssemblyLifecycle");
            try
            {
                var body = root.AddComponent<ArchiveAssemblyPresentation>();
                GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.SetParent(root.transform);
                piece.transform.localPosition = Vector3.up;
                Rigidbody rigidbody = piece.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                piece.SetActive(false);
                GameObject actor = new GameObject("Actor");
                actor.transform.SetParent(root.transform);
                Animator animator = actor.AddComponent<Animator>();
                NavMeshAgent agent = actor.AddComponent<NavMeshAgent>();
                agent.enabled = false; actor.SetActive(false);
                var data = new SerializedObject(body);
                AssignArray(data, "pieces", piece.transform);
                AssignArray(data, "assemblyPoses", actor.transform);
                AssignArray(data, "bodies", rigidbody);
                Assign(data, "actor", animator); Assign(data, "agent", agent);
                Assign(data, "circle", actor); Assign(data, "focus", actor.transform);
                data.ApplyModifiedProperties();
                Exercise(root, body, piece, rigidbody);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Exercise(GameObject root, ArchiveAssemblyPresentation body, GameObject piece, Rigidbody rigidbody)
        {
            var camera = root.AddComponent<Camera>();
            var death = root.AddComponent<PlayerDeathController>();
            var presentation = root.AddComponent<AnomalyDeathPresentation>();
            var anomaly = root.AddComponent<ArchiveAssemblyAnomaly>();
            anomaly.Configure(body, camera, death, presentation);
            anomaly.Activate();
            Assert.That(piece.activeSelf, Is.True);
            Assert.That(rigidbody.isKinematic, Is.False);
            piece.transform.localPosition = Vector3.zero;
            anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
            Assert.That(piece.activeSelf, Is.False);
            Assert.That(piece.transform.localPosition, Is.EqualTo(Vector3.up));
            Assert.That(rigidbody.isKinematic, Is.True);
            Assert.That(anomaly.IsActive, Is.False);
        }

        private static void Assign(SerializedObject target, string property, Object value) => target.FindProperty(property).objectReferenceValue = value;
        private static void AssignArray(SerializedObject target, string property, Object value)
        {
            SerializedProperty array = target.FindProperty(property);
            array.arraySize = 1;
            array.GetArrayElementAtIndex(0).objectReferenceValue = value;
        }
    }
}
